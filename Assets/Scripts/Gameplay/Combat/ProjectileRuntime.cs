using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Enemies;

namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// Domain projectile: seeks target (or flies straight when piercing/forked),
    /// applies damage on hit, optional Chain bounce with hop falloff, Fork splits, Pierce continue.
    /// </summary>
    public sealed class ProjectileRuntime
    {
        public const float ChainHopFalloff = 0.6f;
        public const int DefaultPierceRemaining = 8;

        const float HitRadius = 0.15f;
        const float PierceLookAheadPad = 0.05f;
        const float IgniteDuration = 2f;
        const float IgniteHitFraction = 0.2f;
        const float ChillDuration = 2f;
        const float ChillMagnitude = 0.6f;
        const float ShockDuration = 3f;
        const float ShockMagnitude = 1.25f;
        const float ProlifRadius = 1.5f;
        const float SoftSeekTurnDegPerSec = 140f;

        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }
        public EnemyRuntime Target { get; private set; }
        public float Damage { get; private set; }
        public int ChainRemaining { get; private set; }
        public int PierceRemaining { get; private set; }
        public int ForkRemaining { get; private set; }
        public float Speed { get; private set; }
        public float ChainRange { get; private set; }
        public float AoeRadius { get; private set; }
        public bool Seeking { get; private set; }
        public bool IsActive { get; private set; }

        /// <summary>
        /// When true, seeking rotates toward the aim point gradually so shotgun / Hydra
        /// fan directions stay visible instead of collapsing onto one line every tick.
        /// </summary>
        public bool SoftSeek { get; private set; }

        bool _ignite;
        bool _chill;
        bool _shock;
        bool _prolif;
        StatusRuntime _statuses;
        List<ProjectileRuntime> _spawnBuffer;
        EnemyRuntime _lastHit;
        Vector3 _seekOffset;

        public void Init(
            Vector3 origin,
            Vector3 direction,
            EnemyRuntime target,
            float damage,
            int chainCount,
            float speed,
            float chainRange,
            float aoeRadius = 0f,
            int pierceRemaining = 0,
            int forkRemaining = 0,
            bool ignite = false,
            bool chill = false,
            bool shock = false,
            bool prolif = false,
            StatusRuntime statuses = null,
            List<ProjectileRuntime> spawnBuffer = null,
            bool softSeek = false,
            Vector3 seekOffset = default)
        {
            Position = origin;
            Direction = direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector3.forward;
            Target = target;
            Damage = damage;
            ChainRemaining = chainCount;
            PierceRemaining = pierceRemaining;
            ForkRemaining = forkRemaining;
            Speed = speed;
            ChainRange = chainRange;
            AoeRadius = aoeRadius > 0f ? aoeRadius : 0f;
            Seeking = target != null;
            SoftSeek = softSeek && Seeking;
            _seekOffset = seekOffset;
            _ignite = ignite;
            _chill = chill;
            _shock = shock;
            _prolif = prolif;
            _statuses = statuses;
            _spawnBuffer = spawnBuffer;
            _lastHit = null;
            IsActive = true;
        }

        public void Deactivate() => IsActive = false;

        /// <summary>
        /// Advances flight. Returns false when the projectile should be removed.
        /// </summary>
        public bool Tick(float dt, List<EnemyRuntime> livingCandidates)
        {
            if (!IsActive)
                return false;

            if (Seeking)
            {
                if (Target == null || !Target.IsAlive)
                {
                    IsActive = false;
                    return false;
                }

                if (dt <= 0f)
                    return true;

                var enemyPos = Target.WorldPosition;
                var toEnemy = enemyPos - Position;
                var dist = toEnemy.magnitude;
                var step = Speed * dt;

                // Hit uses the real enemy body; aim may include a lateral seek offset for fans.
                if (dist <= HitRadius || step >= dist)
                {
                    Position = enemyPos;
                    OnHit(livingCandidates);
                    return IsActive;
                }

                var aimPoint = enemyPos + _seekOffset;
                var toAim = aimPoint - Position;
                var desired = toAim.sqrMagnitude > 1e-8f ? toAim.normalized : toEnemy / dist;

                if (SoftSeek)
                    Direction = Vector3.RotateTowards(
                        Direction,
                        desired,
                        SoftSeekTurnDegPerSec * Mathf.Deg2Rad * dt,
                        0f);
                else
                    Direction = desired;

                Position += Direction * step;
                return true;
            }

            if (dt <= 0f)
                return true;

            var from = Position;
            var stepDist = Speed * dt;
            var movement = Direction * stepDist;
            var to = from + movement;

            var pierceHit = FindPierceCandidate(from, to, livingCandidates);
            if (pierceHit != null)
            {
                Position = pierceHit.WorldPosition;
                Target = pierceHit;
                OnHit(livingCandidates);
                return IsActive;
            }

            Position = to;
            return true;
        }

        void OnHit(List<EnemyRuntime> livingCandidates)
        {
            var hit = Target;
            if (hit == null || !hit.IsAlive)
            {
                IsActive = false;
                return;
            }

            _lastHit = hit;
            ApplyHitDamage(hit);

            if (AoeRadius > 0f && livingCandidates != null)
            {
                var radiusSq = AoeRadius * AoeRadius;
                var hitPos = hit.WorldPosition;
                for (var i = 0; i < livingCandidates.Count; i++)
                {
                    var enemy = livingCandidates[i];
                    if (enemy == null || !enemy.IsAlive || ReferenceEquals(enemy, hit))
                        continue;

                    if ((enemy.WorldPosition - hitPos).sqrMagnitude <= radiusSq)
                        ApplyHitDamage(enemy);
                }
            }

            ApplyStatusesOnHit(hit, livingCandidates);

            // PoE-style: one behavior per collision — Pierce > Fork > Chain.
            if (PierceRemaining > 0)
            {
                PierceRemaining--;
                Target = null;
                Seeking = false;
                return;
            }

            if (ForkRemaining > 0)
            {
                SpawnForkChildren();
                ForkRemaining = 0;
                IsActive = false;
                return;
            }

            if (ChainRemaining > 0)
            {
                var next = FindNearestOther(Position, hit, ChainRange, livingCandidates);
                if (next != null)
                {
                    Damage *= ChainHopFalloff;
                    ChainRemaining--;
                    Target = next;
                    Seeking = true;
                    SoftSeek = false;
                    _seekOffset = Vector3.zero;
                    var aim = next.WorldPosition - Position;
                    if (aim.sqrMagnitude > 1e-8f)
                        Direction = aim.normalized;
                    return;
                }
            }

            IsActive = false;
        }

        void ApplyHitDamage(EnemyRuntime enemy)
        {
            if (_statuses != null)
                _statuses.ApplyDamage(enemy, Damage);
            else
                enemy.ApplyDamage(Damage);
        }

        void ApplyStatusesOnHit(EnemyRuntime hit, List<EnemyRuntime> livingCandidates)
        {
            if (_statuses == null || hit == null)
                return;

            if (_ignite)
                _statuses.Apply(hit, StatusId.Ignite, IgniteDuration, Damage * IgniteHitFraction);

            if (_chill)
                _statuses.Apply(hit, StatusId.Chill, ChillDuration, ChillMagnitude);

            if (_shock)
                _statuses.Apply(hit, StatusId.Shock, ShockDuration, ShockMagnitude);

            if (_prolif)
                _statuses.ProliferateIgniteChillShock(hit, ProlifRadius, livingCandidates);
        }

        void SpawnForkChildren()
        {
            if (_spawnBuffer == null)
                return;

            var inbound = Direction.sqrMagnitude > 1e-8f ? Direction.normalized : Vector3.forward;
            SpawnForkChild(Quaternion.Euler(0f, 45f, 0f) * inbound);
            SpawnForkChild(Quaternion.Euler(0f, -45f, 0f) * inbound);
        }

        void SpawnForkChild(Vector3 childDirection)
        {
            var child = new ProjectileRuntime();
            child.Init(
                Position,
                childDirection,
                target: null,
                Damage,
                ChainRemaining,
                Speed,
                ChainRange,
                AoeRadius,
                PierceRemaining,
                ForkRemaining - 1,
                _ignite,
                _chill,
                _shock,
                _prolif,
                _statuses,
                _spawnBuffer);
            child._lastHit = _lastHit;
            _spawnBuffer.Add(child);
        }

        EnemyRuntime FindPierceCandidate(
            Vector3 from,
            Vector3 to,
            List<EnemyRuntime> candidates)
        {
            if (candidates == null || Direction.sqrMagnitude < 1e-8f)
                return null;

            var move = to - from;
            var moveLen = move.magnitude;
            if (moveLen < 1e-8f)
                return null;

            var moveDir = move / moveLen;
            var hitRadiusSq = (HitRadius + PierceLookAheadPad) * (HitRadius + PierceLookAheadPad);
            EnemyRuntime best = null;
            var bestT = float.PositiveInfinity;

            for (var i = 0; i < candidates.Count; i++)
            {
                var enemy = candidates[i];
                if (enemy == null || !enemy.IsAlive || ReferenceEquals(enemy, _lastHit))
                    continue;

                var toEnemy = enemy.WorldPosition - from;
                var t = Vector3.Dot(toEnemy, moveDir);
                if (t < -HitRadius || t > moveLen + HitRadius)
                    continue;

                var closest = from + moveDir * Mathf.Clamp(t, 0f, moveLen);
                if ((enemy.WorldPosition - closest).sqrMagnitude > hitRadiusSq)
                    continue;

                if (t >= bestT)
                    continue;

                bestT = t;
                best = enemy;
            }

            return best;
        }

        static EnemyRuntime FindNearestOther(
            Vector3 from,
            EnemyRuntime current,
            float range,
            List<EnemyRuntime> candidates)
        {
            if (candidates == null || range <= 0f)
                return null;

            var rangeSq = range * range;
            EnemyRuntime best = null;
            var bestDistSq = float.PositiveInfinity;

            for (var i = 0; i < candidates.Count; i++)
            {
                var enemy = candidates[i];
                if (enemy == null || !enemy.IsAlive || ReferenceEquals(enemy, current))
                    continue;

                var distSq = (enemy.WorldPosition - from).sqrMagnitude;
                if (distSq > rangeSq || distSq >= bestDistSq)
                    continue;

                bestDistSq = distSq;
                best = enemy;
            }

            return best;
        }
    }
}
