using System;
using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// Domain projectile. Primary / fork / pierce / chain all fly straight (ballistic).
    /// Chain: snap-aim once toward nearest living enemy within <see cref="DefaultChainRange"/>, then fly straight.
    /// Fork continues past the hit: inbound -o&lt; two forward-angled children.
    /// </summary>
    public sealed class ProjectileRuntime
    {
        public const float ChainHopFalloff = 0.6f;
        public const float DefaultChainRange = 3f;
        public const int DefaultPierceRemaining = 1;
        public const float MaxLifetimeSeconds = 6f;
        public const float MaxFlightDistance = 80f;
        public const float ForkHalfAngleDegrees = 45f;
        public const float ForkSpawnForwardPad = 0.08f;

        const float HitRadius = 0.15f;
        const float PierceLookAheadPad = 0.05f;
        const float IgniteDuration = 2f;
        const float IgniteHitFraction = 0.2f;
        const float ChillDuration = 2f;
        const float ChillMagnitude = 0.6f;
        const float ShockDuration = 3f;
        const float ShockMagnitude = 1.25f;
        const float ProlifRadius = 1.5f;

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

        /// <summary>Unused — kept for call-site / test compat.</summary>
        public bool SoftSeek { get; private set; }

        bool _ignite;
        bool _chill;
        bool _shock;
        bool _prolif;
        float _knockbackChance;
        float _knockbackDistance;
        StatusRuntime _statuses;
        List<ProjectileRuntime> _spawnBuffer;
        EnemyRuntime _lastHit;
        TowerDefinition _sourceTower;
        Action<TowerDefinition, float> _recordDamage;
        float _age;
        float _traveled;

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
            Vector3 seekOffset = default,
            TowerDefinition sourceTower = null,
            Action<TowerDefinition, float> recordDamage = null,
            float knockbackChance = 0f,
            float knockbackDistance = 0f)
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
            // Ballistic only. Chain hop snap-aims Direction once in OnHit (no continuous homing).
            Seeking = false;
            SoftSeek = false;
            _ = softSeek;
            _ = seekOffset;
            _ignite = ignite;
            _chill = chill;
            _shock = shock;
            _prolif = prolif;
            _statuses = statuses;
            _spawnBuffer = spawnBuffer;
            _lastHit = null;
            _sourceTower = sourceTower;
            _recordDamage = recordDamage;
            _knockbackChance = knockbackChance > 0f ? knockbackChance : 0f;
            _knockbackDistance = knockbackDistance > 0f ? knockbackDistance : 0f;
            _age = 0f;
            _traveled = 0f;
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

            if (dt > 0f)
            {
                _age += dt;
                _traveled += Speed * dt;
                if (_age >= MaxLifetimeSeconds || _traveled >= MaxFlightDistance)
                {
                    IsActive = false;
                    return false;
                }
            }

            if (dt <= 0f)
                return true;

            var from = Position;
            var stepDist = Speed * dt;
            var to = from + Direction * stepDist;

            var hit = FindPierceCandidate(from, to, livingCandidates);
            if (hit != null)
            {
                Position = hit.WorldPosition;
                Target = hit;
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
            ApplyKnockbackOnHit(hit);

            // PoE-style: one behavior per collision — Pierce > Fork > Chain.
            // PierceRemaining is extra through-hits; 1 = continue past this target once.
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
                    Seeking = false;
                    SoftSeek = false;
                    // Snap-aim once toward the nearest in-range enemy, then resume ballistic flight.
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
            if (_sourceTower != null)
                enemy.LastDamageSource = _sourceTower;

            if (_statuses != null)
                _statuses.ApplyDamage(enemy, Damage);
            else
                enemy.ApplyDamage(Damage);

            _recordDamage?.Invoke(_sourceTower, Damage);
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

        void ApplyKnockbackOnHit(EnemyRuntime hit)
        {
            if (hit == null || !hit.IsAlive)
                return;
            if (_knockbackChance <= 0f || _knockbackDistance <= 0f)
                return;
            if (_knockbackChance < 1f && UnityEngine.Random.value >= _knockbackChance)
                return;

            hit.KnockbackAlongPath(_knockbackDistance);
        }

        void SpawnForkChildren()
        {
            if (_spawnBuffer == null)
                return;

            // -o<  continue past the hit along inbound, then open ±ForkHalfAngleDegrees.
            var inbound = Direction.sqrMagnitude > 1e-8f ? Direction.normalized : Vector3.forward;
            var spawnAt = Position + inbound * ForkSpawnForwardPad;
            SpawnForkChild(spawnAt, Quaternion.Euler(0f, ForkHalfAngleDegrees, 0f) * inbound);
            SpawnForkChild(spawnAt, Quaternion.Euler(0f, -ForkHalfAngleDegrees, 0f) * inbound);
        }

        void SpawnForkChild(Vector3 origin, Vector3 childDirection)
        {
            var child = new ProjectileRuntime();
            child.Init(
                origin,
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
                _spawnBuffer,
                sourceTower: _sourceTower,
                recordDamage: _recordDamage,
                knockbackChance: _knockbackChance,
                knockbackDistance: _knockbackDistance);
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
