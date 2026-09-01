using System;
using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// Domain projectile. Straight Direct index-0 bolts forward-cone seek the locked
    /// primary (XZ hemisphere). Fan extras, Ground, pierce continue, chain hops, and
    /// fork children fly straight. Hit tests stay XZ.
    /// Chain: snap-aim once toward nearest living enemy within <see cref="DefaultChainRange"/>, then fly straight.
    /// Fork: parent dies on the forking hit, then two children at ±ForkHalfAngleDegrees.
    /// Fork children never fork.
    /// </summary>
    public sealed class ProjectileRuntime
    {
        /// <summary>Normal Chain rarity hop damage factor. Lesser / Greater use the tier constants below.</summary>
        public const float DefaultChainHopFalloff = 0.6f;
        public const float LesserChainHopFalloff = 0.5f;
        public const float GreaterChainHopFalloff = 0.7f;
        public const float DefaultChainRange = 3f;
        public const int DefaultPierceRemaining = 1;
        public const int InfinitePierceRemaining = -1;
        public const float DefaultProjectileSpeed = 20f;
        public const float MaxLifetimeSeconds = 2f;
        public const float ForkHalfAngleDegrees = 30f;
        public const float ForkSpawnForwardPad = 0.08f;
        /// <summary>
        /// Shotgun fan when <see cref="SkillSpec.ProjectileCount"/> is above 1 and no spread was authored.
        /// </summary>
        public const float DefaultVolleySpreadDegrees = 24f;

        public static float ForkChildYawDegrees(int index, int count)
        {
            if (count <= 1)
                return 0f;
            var t = index / (float)(count - 1);
            return Mathf.Lerp(-ForkHalfAngleDegrees, ForkHalfAngleDegrees, t);
        }

        public static float VolleyYawDegrees(int index, int count, float spreadDegrees)
        {
            if (count <= 1)
                return 0f;
            var spread = spreadDegrees > 0f ? spreadDegrees : DefaultVolleySpreadDegrees;
            if (index <= 0)
                return 0f;

            var extra = count - 1;
            if (extra == 1)
                return spread * 0.5f;

            var t = (index - 1) / (float)(extra - 1);
            return Mathf.Lerp(-spread * 0.5f, spread * 0.5f, t);
        }
        public const float BleedDuration = 2f;
        public const float BleedHitFraction = 0.2f;
        public const float IgniteDuration = 2f;
        public const float IgniteHitFraction = 0.2f;
        public const float ChillDuration = 2f;
        public const float ChillMagnitude = 0.6f;
        public const float ShockDuration = 3f;
        public const float ShockMagnitude = 1.25f;
        public const float FreezeDuration = 2f;
        public const float PoisonDuration = 2f;
        public const float PoisonHitFraction = 0.2f;
        public const float StunDuration = 2f;
        public const float HallowingFlameDuration = 6f;

        public const float HitRadius = 0.15f;
        public const float WarpRiseHeight = 1.5f;
        public const float WarpDropHeight = 1.5f;
        public const float MoltenMagmaDamageFactor = 0.4f;
        public const float PierceLookAheadPad = 0.05f;
        const float ProlifRadius = 1.5f;

        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }
        public EnemyRuntime Target { get; private set; }
        public float Damage { get; private set; }
        public int ChainRemaining { get; private set; }
        public float ChainHopFalloff { get; private set; }
        public int PierceRemaining { get; private set; }
        public int ForkRemaining { get; private set; }
        public float Speed { get; private set; }
        public float ChainRange { get; private set; }
        public float AoeRadius { get; private set; }
        public bool Seeking { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsPayload { get; private set; }
        public bool IsWarpStrike { get; private set; }

        /// <summary>Unused — kept for call-site / test compat.</summary>
        public bool SoftSeek { get; private set; }

        bool _prolif;
        float _knockbackChance;
        float _knockbackDistance;
        AilmentTune _ailments;
        StatusRuntime _statuses;
        List<ProjectileRuntime> _spawnBuffer;
        EnemyRuntime _lastHit;
        TowerDefinition _sourceTower;
        Action<TowerDefinition, float> _recordDamage;
        float _age;
        float _maxTravel;
        float _travelled;
        Vector3 _landPoint;
        SkillSpec _payloadSpec;
        SkillSpec _hitSpec;
        float _payloadDamageMin;
        float _payloadDamageMax;
        float _payloadChainRange;
        bool _warpDropping;
        Vector3 _warpOrigin;
        SkillSpec _warpSpec;
        Action<Vector3, SkillSpec> _onImpactPayloads;

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
            float knockbackDistance = 0f,
            float chainHopFalloff = 0f,
            float bleedChance = 0f,
            float bleedDamageMultiplier = 0f,
            AilmentTune ailments = default,
            SkillSpec hitSpec = default,
            float maxTravel = 0f,
            bool seeking = false)
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
            ChainHopFalloff = chainHopFalloff == 0f ? 1f : chainHopFalloff;
            AoeRadius = aoeRadius > 0f ? aoeRadius : 0f;
            // Index-0 Straight Direct may seek. Chain hops snap-aim once in OnHit.
            Seeking = seeking;
            SoftSeek = false;
            _ = softSeek;
            _ = seekOffset;
            _prolif = prolif;
            _ailments = ailments;
            _ailments.Ignite = ignite || ailments.Ignite;
            _ailments.Chill = chill || ailments.Chill;
            _ailments.Shock = shock || ailments.Shock;
            if (bleedChance > 0f)
                _ailments.BleedChance = bleedChance;
            if (bleedDamageMultiplier != 0f)
                _ailments.BleedDamageMultiplier = bleedDamageMultiplier;
            else if (_ailments.BleedDamageMultiplier == 0f)
                _ailments.BleedDamageMultiplier = 1f;
            if (_ailments.AilmentDamageMultiplier == 0f)
                _ailments.AilmentDamageMultiplier = 1f;
            if (_ailments.AilmentDurationMultiplier == 0f)
                _ailments.AilmentDurationMultiplier = 1f;
            _statuses = statuses;
            _spawnBuffer = spawnBuffer;
            _lastHit = null;
            _sourceTower = sourceTower;
            _recordDamage = recordDamage;
            _knockbackChance = knockbackChance > 0f ? knockbackChance : 0f;
            _knockbackDistance = knockbackDistance > 0f ? knockbackDistance : 0f;
            _age = 0f;
            _travelled = 0f;
            _maxTravel = maxTravel > 0f ? maxTravel : 0f;
            IsActive = true;
            IsPayload = false;
            IsWarpStrike = false;
            _hitSpec = hitSpec;
        }

        public void InitPayload(
            Vector3 origin,
            Vector3 landPoint,
            SkillSpec spec,
            float damageMin,
            float damageMax,
            float speed,
            float chainRange,
            StatusRuntime statuses,
            List<ProjectileRuntime> spawnBuffer,
            TowerDefinition sourceTower,
            Action<TowerDefinition, float> recordDamage)
        {
            Init(
                origin,
                landPoint - origin,
                null,
                0f,
                0,
                speed,
                chainRange,
                0f,
                0,
                0,
                false,
                false,
                false,
                false,
                statuses,
                spawnBuffer,
                false,
                default,
                sourceTower,
                recordDamage,
                0f,
                0f,
                0f,
                0f,
                0f,
                default);
            IsPayload = true;
            _landPoint = landPoint;
            _payloadSpec = spec;
            _hitSpec = spec;
            _payloadDamageMin = damageMin;
            _payloadDamageMax = damageMax;
            _payloadChainRange = chainRange;
        }

        public void InitWarpStrike(
            Vector3 origin,
            EnemyRuntime target,
            SkillSpec spec,
            float meleeDamage,
            float speed,
            float chainRange,
            StatusRuntime statuses,
            List<ProjectileRuntime> spawnBuffer,
            TowerDefinition sourceTower,
            Action<TowerDefinition, float> recordDamage,
            Action<Vector3, SkillSpec> onImpactPayloads = null)
        {
            Init(
                origin,
                Vector3.up,
                target,
                meleeDamage,
                0,
                speed,
                chainRange,
                spec.AoeRadius,
                0,
                0,
                spec.Ignite,
                spec.Chill,
                spec.Shock,
                spec.Proliferate,
                statuses,
                spawnBuffer,
                false,
                default,
                sourceTower,
                recordDamage,
                spec.KnockbackChance,
                spec.KnockbackDistance,
                spec.ChainHopFalloff,
                spec.BleedChance,
                spec.BleedDamageMultiplier,
                AilmentTune.FromSkillSpec(spec));
            IsWarpStrike = true;
            _warpDropping = false;
            _warpOrigin = origin;
            _warpSpec = spec;
            _hitSpec = spec;
            _onImpactPayloads = onImpactPayloads;
            PierceRemaining = 0;
            ForkRemaining = 0;
            ChainRemaining = 0;
        }

        public void ExplodePayload()
        {
            if (_spawnBuffer == null)
            {
                IsActive = false;
                return;
            }

            var count = _payloadSpec.ProjectileCount;
            if (count <= 0)
            {
                IsActive = false;
                return;
            }

            var pierceRemaining = _payloadSpec.GetPierceRemaining();
            var forkRemaining = _payloadSpec.ForkCount;
            var ailments = AilmentTune.FromSkillSpec(_payloadSpec);
            var origin = Position;
            var step = 360f / count;
            for (var i = 0; i < count; i++)
            {
                var yaw = i * step;
                var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                var child = new ProjectileRuntime();
                child.Init(
                    origin,
                    dir,
                    null,
                    RoleStatValue.SampleHitDamage(_payloadDamageMin, _payloadDamageMax),
                    _payloadSpec.ChainCount,
                    Speed,
                    _payloadChainRange,
                    _payloadSpec.AoeRadius,
                    pierceRemaining,
                    forkRemaining,
                    _payloadSpec.Ignite,
                    _payloadSpec.Chill,
                    _payloadSpec.Shock,
                    _payloadSpec.Proliferate,
                    _statuses,
                    _spawnBuffer,
                    false,
                    default,
                    _sourceTower,
                    _recordDamage,
                    _payloadSpec.KnockbackChance,
                    _payloadSpec.KnockbackDistance,
                    _payloadSpec.ChainHopFalloff,
                    _payloadSpec.BleedChance,
                    _payloadSpec.BleedDamageMultiplier,
                    ailments,
                    _payloadSpec);
                _spawnBuffer.Add(child);
            }

            IsActive = false;
        }

        public void Deactivate() => IsActive = false;

        void TickForwardConeSeek()
        {
            if (!Seeking)
                return;

            if (Target == null || !Target.IsAlive)
            {
                Seeking = false;
                return;
            }

            var to = Target.WorldPosition - Position;
            var flatTo = to;
            flatTo.y = 0f;
            var flatDir = Direction;
            flatDir.y = 0f;
            if (flatDir.sqrMagnitude < 1e-8f)
                flatDir = Vector3.forward;
            else
                flatDir.Normalize();

            if (flatTo.sqrMagnitude < 1e-8f)
                return;

            if (Vector3.Dot(flatDir, flatTo.normalized) <= 0f)
            {
                Seeking = false;
                return;
            }

            if (to.sqrMagnitude > 1e-8f)
                Direction = to.normalized;
        }

        /// <summary>
        /// Advances flight. Returns false when the projectile should be removed.
        /// </summary>
        public bool Tick(float dt, List<EnemyRuntime> livingCandidates)
        {
            if (!IsActive)
                return false;

            if (IsPayload)
                return TickPayload(dt);

            if (IsWarpStrike)
                return TickWarpStrike(dt, livingCandidates);

            if (dt > 0f)
            {
                _age += dt;
                if (_age >= MaxLifetimeSeconds)
                {
                    IsActive = false;
                    return false;
                }
            }

            if (dt <= 0f)
                return true;

            TickForwardConeSeek();

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
            _travelled += Flat(to - from).magnitude;
            if (_maxTravel > 0.01f && _travelled >= _maxTravel)
            {
                IsActive = false;
                return false;
            }

            return true;
        }

        bool TickPayload(float dt)
        {
            if (dt > 0f)
            {
                _age += dt;
                if (_age >= MaxLifetimeSeconds)
                {
                    ExplodePayload();
                    return false;
                }
            }

            if (dt <= 0f)
                return true;

            var toLand = _landPoint - Position;
            var remaining = toLand.magnitude;
            var stepDist = Speed * dt;
            if (remaining <= HitRadius || stepDist >= remaining)
            {
                Position = _landPoint;
                ExplodePayload();
                return false;
            }

            Position += toLand / remaining * stepDist;
            return true;
        }

        bool TickWarpStrike(float dt, List<EnemyRuntime> livingCandidates)
        {
            if (dt > 0f)
            {
                _age += dt;
                if (_age >= MaxLifetimeSeconds)
                {
                    IsActive = false;
                    return false;
                }
            }

            if (dt <= 0f)
                return true;

            if (!_warpDropping)
            {
                Position += Vector3.up * (Speed * dt);
                if (Position.y < _warpOrigin.y + WarpRiseHeight)
                    return true;

                var target = Target;
                if (target == null || !target.IsAlive)
                {
                    IsActive = false;
                    return false;
                }

                _landPoint = target.WorldPosition;
                Position = _landPoint + Vector3.up * WarpDropHeight;
                Direction = (_landPoint - Position).normalized;
                _warpDropping = true;
                _age = 0f;
            }

            var toLand = _landPoint - Position;
            var remaining = toLand.magnitude;
            var stepDist = Speed * dt;
            if (remaining <= HitRadius || stepDist >= remaining)
            {
                Position = _landPoint;
                Direction = Vector3.down;
                LandWarpStrike(livingCandidates);
                return false;
            }

            Position += toLand / remaining * stepDist;
            Direction = (_landPoint - Position).normalized;
            return true;
        }

        void LandWarpStrike(List<EnemyRuntime> livingCandidates)
        {
            var hit = Target;
            if (hit != null && hit.IsAlive)
            {
                Target = hit;
                OnHit(livingCandidates);
            }

            _onImpactPayloads?.Invoke(Position, _warpSpec);
            IsActive = false;
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
            if (PierceRemaining == InfinitePierceRemaining || PierceRemaining > 0)
            {
                if (PierceRemaining > 0)
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

            var dealt = Damage + ExtraHitDamage(enemy);
            if (_statuses != null)
                _statuses.ApplyDamage(enemy, dealt, _hitSpec);
            else
                enemy.ApplyDamage(dealt, _hitSpec, null);

            _recordDamage?.Invoke(_sourceTower, dealt);
        }

        float ExtraHitDamage(EnemyRuntime enemy)
        {
            var extra = 0f;
            if (!_ailments.HallowingFlame)
            {
                extra += Damage * _ailments.PhysAsExtraFire;
                extra += Damage * _ailments.PhysAsExtraCold;
                extra += Damage * _ailments.PhysAsExtraLightning;
                extra += Damage * _ailments.PhysAsExtraChaos;
            }

            if (_statuses != null
                && _statuses.TryConsumeHallowingFlame(enemy, _sourceTower, out var hallowExtra))
            {
                extra += Damage * hallowExtra;
            }

            return extra;
        }

        void ApplyStatusesOnHit(EnemyRuntime hit, List<EnemyRuntime> livingCandidates)
        {
            if (_statuses == null || hit == null)
                return;

            if (RollAilment(_ailments.Ignite, _ailments.IgniteChance))
            {
                _statuses.Apply(
                    hit,
                    StatusId.Ignite,
                    ScaledDuration(_ailments.IgniteDuration, IgniteDuration),
                    Damage
                    * IgniteHitFraction
                    * BurningOrDefault(_ailments.BurningDamageMultiplier)
                    * AilmentDamageOrDefault());
            }

            if (RollAilment(false, _ailments.BleedChance))
            {
                _statuses.Apply(
                    hit,
                    StatusId.Bleed,
                    ScaledDuration(_ailments.BleedDuration, BleedDuration),
                    Damage
                    * BleedHitFraction
                    * _ailments.BleedDamageMultiplier
                    * AilmentDamageOrDefault());
            }

            if (RollAilment(_ailments.Chill, 0f))
            {
                var effect = _ailments.ChillEffect == 0f ? 1f : _ailments.ChillEffect;
                var magnitude = 1f - (1f - ChillMagnitude) * effect;
                if (magnitude < 0f)
                    magnitude = 0f;
                _statuses.Apply(
                    hit,
                    StatusId.Chill,
                    ScaledDuration(_ailments.ChillDuration, ChillDuration),
                    magnitude);
            }

            if (RollAilment(_ailments.Shock, _ailments.ShockChance))
            {
                var effect = _ailments.ShockEffect == 0f ? 1f : _ailments.ShockEffect;
                var magnitude = 1f + (ShockMagnitude - 1f) * effect;
                _statuses.Apply(
                    hit,
                    StatusId.Shock,
                    ScaledDuration(_ailments.ShockDuration, ShockDuration),
                    magnitude);
            }

            if (RollAilment(false, _ailments.FreezeChance))
            {
                _statuses.Apply(
                    hit,
                    StatusId.Freeze,
                    ScaledDuration(_ailments.FreezeDuration, FreezeDuration),
                    0f);
            }

            if (RollAilment(false, _ailments.PoisonChance))
            {
                _statuses.Apply(
                    hit,
                    StatusId.Poison,
                    ScaledDuration(_ailments.PoisonDuration, PoisonDuration),
                    Damage * PoisonHitFraction * AilmentDamageOrDefault());
            }

            if (RollAilment(false, _ailments.StunChance))
            {
                _statuses.Apply(
                    hit,
                    StatusId.Stun,
                    ScaledDuration(_ailments.StunDuration, StunDuration),
                    0f);
            }

            if (_ailments.HallowingFlame)
            {
                _statuses.Apply(
                    hit,
                    StatusId.HallowingFlame,
                    HallowingFlameDuration,
                    _ailments.PhysAsExtraFire,
                    _sourceTower);
            }

            if (_prolif)
                _statuses.ProliferateIgniteChillShock(hit, ProlifRadius, livingCandidates);
        }

        static bool RollAilment(bool flag, float chance)
        {
            var p = flag ? 1f : chance;
            if (p <= 0f)
                return false;
            if (p >= 1f)
                return true;
            return UnityEngine.Random.value < p;
        }

        static float DurationOrDefault(float authored, float fallback)
        {
            return authored > 0f ? authored : fallback;
        }

        float ScaledDuration(float authored, float fallback)
        {
            return DurationOrDefault(authored, fallback) * DurationMulOrDefault();
        }

        float DurationMulOrDefault()
        {
            return _ailments.AilmentDurationMultiplier == 0f
                ? 1f
                : _ailments.AilmentDurationMultiplier;
        }

        float AilmentDamageOrDefault()
        {
            return _ailments.AilmentDamageMultiplier == 0f
                ? 1f
                : _ailments.AilmentDamageMultiplier;
        }

        static float BurningOrDefault(float authored)
        {
            return authored == 0f ? 1f : authored;
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

            // Parent dies on this hit. Child count is ForkRemaining, fanned across ±ForkHalfAngleDegrees.
            var inbound = Direction.sqrMagnitude > 1e-8f ? Direction.normalized : Vector3.forward;
            var spawnAt = Position + inbound * ForkSpawnForwardPad;
            var count = ForkRemaining;
            for (var i = 0; i < count; i++)
            {
                var yaw = ForkChildYawDegrees(i, count);
                SpawnForkChild(spawnAt, Quaternion.Euler(0f, yaw, 0f) * inbound);
            }
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
                forkRemaining: 0,
                _ailments.Ignite,
                _ailments.Chill,
                _ailments.Shock,
                _prolif,
                _statuses,
                _spawnBuffer,
                sourceTower: _sourceTower,
                recordDamage: _recordDamage,
                knockbackChance: _knockbackChance,
                knockbackDistance: _knockbackDistance,
                chainHopFalloff: ChainHopFalloff,
                ailments: _ailments,
                hitSpec: _hitSpec);
            child._lastHit = _lastHit;
            child._maxTravel = _maxTravel;
            child._travelled = 0f;
            _spawnBuffer.Add(child);
        }

        EnemyRuntime FindPierceCandidate(
            Vector3 from,
            Vector3 to,
            List<EnemyRuntime> candidates)
        {
            if (candidates == null || Direction.sqrMagnitude < 1e-8f)
                return null;

            var move = Flat(to) - Flat(from);
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
                if (enemy == null || !enemy.IsAlive)
                    continue;
                if (ReferenceEquals(enemy, _lastHit))
                    continue;

                var toEnemy = Flat(enemy.WorldPosition) - Flat(from);
                var t = Vector3.Dot(toEnemy, moveDir);
                if (t < -HitRadius || t > moveLen + HitRadius)
                    continue;

                var closest = Flat(from) + moveDir * Mathf.Clamp(t, 0f, moveLen);
                if ((Flat(enemy.WorldPosition) - closest).sqrMagnitude > hitRadiusSq)
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

                var distSq = FlatSqr(enemy.WorldPosition, from);
                if (distSq > rangeSq || distSq >= bestDistSq)
                    continue;

                bestDistSq = distSq;
                best = enemy;
            }

            return best;
        }

        static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        static float FlatSqr(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
