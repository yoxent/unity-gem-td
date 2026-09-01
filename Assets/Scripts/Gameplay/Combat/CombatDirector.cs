using System;
using System.Collections.Generic;
using UnityEngine;
using GemTD.Core;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Map;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// Domain combat tick: cooldown ready starts a cast (NotifyFired). FireInterval is the whole action.
    /// The volley spawns at strikeNormalized of that interval; cooldown still covers the full interval.
    /// </summary>
    public sealed class CombatDirector
    {
        readonly float _cellSize;
        readonly float _projectileSpeed;
        readonly TargetSelector _selector = new TargetSelector();
        readonly List<ProjectileRuntime> _projectiles = new List<ProjectileRuntime>(32);
        readonly List<ProjectileRuntime> _spawnBuffer = new List<ProjectileRuntime>(16);
        readonly List<EffectPayloadRuntime> _effectPayloads = new List<EffectPayloadRuntime>(16);
        readonly List<EffectPayloadDefinition> _payloadDefinitionsScratch =
            new List<EffectPayloadDefinition>(8);
        readonly List<EffectPayloadPlan> _payloadPlanScratch = new List<EffectPayloadPlan>(8);
        readonly List<EnemyRuntime> _casterNovaScratch = new List<EnemyRuntime>(16);
        readonly List<PendingSequentialVolley> _pendingSequential = new List<PendingSequentialVolley>(8);
        readonly List<PendingStrike> _pendingStrikes = new List<PendingStrike>(8);
        readonly List<TowerInstance> _resolvedCasts = new List<TowerInstance>(8);
        System.Random _payloadRng;
        readonly Action<TowerDefinition, float> _recordDamage;
        readonly TileHeightMap _heights;

        public IReadOnlyList<ProjectileRuntime> Projectiles => _projectiles;
        public IReadOnlyList<EffectPayloadRuntime> EffectPayloads => _effectPayloads;
        public bool HasActiveVolley =>
            _projectiles.Count > 0
            || _effectPayloads.Count > 0
            || _pendingSequential.Count > 0
            || _pendingStrikes.Count > 0;

        public CombatDirector(
            float cellSize = 1f,
            float projectileSpeed = ProjectileRuntime.DefaultProjectileSpeed,
            Action<TowerDefinition, float> recordDamage = null,
            TileHeightMap heights = null,
            System.Random payloadRng = null)
        {
            _cellSize = cellSize > 0f ? cellSize : 1f;
            _projectileSpeed = projectileSpeed > 0f
                ? projectileSpeed
                : ProjectileRuntime.DefaultProjectileSpeed;
            _recordDamage = recordDamage;
            _heights = heights;
            _payloadRng = payloadRng ?? new System.Random();
        }

        /// <summary>Despawn all in-flight bolts (wave end / leave combat). Views pool via sync.</summary>
        public void ClearProjectiles()
        {
            ClearProjectiles(keepDelayedStationaryPulses: false);
        }

        public void ClearProjectiles(bool keepDelayedStationaryPulses)
        {
            for (var i = 0; i < _projectiles.Count; i++)
                _projectiles[i].Deactivate();
            _projectiles.Clear();
            _spawnBuffer.Clear();
            for (var i = _effectPayloads.Count - 1; i >= 0; i--)
            {
                var payload = _effectPayloads[i];
                if (keepDelayedStationaryPulses && IsLiveStationaryPulse(payload))
                    continue;
                payload.Deactivate();
                _effectPayloads.RemoveAt(i);
            }

            _payloadDefinitionsScratch.Clear();
            _payloadPlanScratch.Clear();
            _pendingSequential.Clear();
            _pendingStrikes.Clear();
            _resolvedCasts.Clear();
        }

        static bool IsLiveStationaryPulse(EffectPayloadRuntime payload)
        {
            return payload != null
                && payload.IsActive
                && payload.Plan.TravelPattern == EffectPayloadTravelPattern.StationaryPulse;
        }

        /// <summary>Reset deterministic payload scatter before replaying a preview volley.</summary>
        public void ResetPayloadRng(int seed)
        {
            _payloadRng = new System.Random(seed);
        }

        public void Tick(
            float dt,
            List<TowerInstance> towers,
            EnemyRegistry enemies,
            GemModifierPipeline pipeline,
            StatusRuntime statuses = null)
        {
            if (enemies == null || pipeline == null)
                return;

            _resolvedCasts.Clear();
            var living = ListPool<EnemyRuntime>.Get();
            enemies.CopyAlive(living);
            TickInFlight(dt, living);

            // Refresh after projectile kills so tower targeting sees current alive set.
            enemies.CopyAlive(living);
            PackAuraRuntime.Apply(living);

            if (statuses != null)
                statuses.ClearCurseHexes(living);

            if (towers != null)
            {
                for (var t = 0; t < towers.Count; t++)
                {
                    var tower = towers[t];
                    if (tower == null || tower.Def == null)
                        continue;

                    if (tower.Def.HasRole<CurseRoleDefinition>())
                    {
                        ApplyCursePresence(tower, living, pipeline, statuses);
                        continue;
                    }

                    if (!tower.Def.IsFireable)
                        continue;

                    tower.Cooldown -= dt;
                    if (HasPendingStrike(tower) || ResolvedThisTick(tower))
                        continue;
                    if (tower.Cooldown > 0f)
                        continue;

                    var modifiers = ListPool<ISkillModifier>.Get();
                    var baseline = pipeline.ResolveBaseline(tower);
                    var spec = pipeline.Resolve(tower, modifiers);
                    ListPool<ISkillModifier>.Release(modifiers);

                    var towerPos = CellToWorld(tower.Cell);
                    var muzzle = towerPos;
                    var gemMul = spec.RangeMultiplier > 0.01f ? spec.RangeMultiplier : 1f;
                    var heightMul = 1f;
                    if (_heights != null)
                    {
                        var layer = _heights.Get(tower.Cell.x, tower.Cell.y);
                        muzzle.y = TileHeightVisual.TopY(layer);
                        heightMul = TileHeightRules.RangeMultiplier(layer);
                    }
                    ApplyMuzzleLocalY(ref muzzle, tower);
                    var range = tower.Def.GetFireTowerRadius(tower.Level) * gemMul * heightMul;
                    if (!_selector.TrySelect(tower.Targeting, towerPos, range, living, out var primary))
                        continue;

                    tower.Cooldown = tower.Def.FireInterval(spec, tower.Level);
                    BeginResolvedVolley(muzzle, range, primary, spec, baseline, living, statuses, tower);
                }
            }

            ListPool<EnemyRuntime>.Release(living);
        }

        /// <summary>Advance in-flight bolts/payloads only. Does not fire towers.</summary>
        public void TickInFlight(float dt, List<EnemyRuntime> living)
        {
            if (living == null)
                return;

            TickPendingSequential(dt, living);

            for (var p = _effectPayloads.Count - 1; p >= 0; p--)
            {
                if (!_effectPayloads[p].Tick(dt, living))
                    _effectPayloads.RemoveAt(p);
            }

            for (var i = _projectiles.Count - 1; i >= 0; i--)
            {
                if (!_projectiles[i].Tick(dt, living))
                    _projectiles.RemoveAt(i);
            }

            TickPendingStrikes(dt, living);
            MergeSpawnBuffer();
        }

        /// <summary>
        /// Fire one volley from a world-space muzzle. Ignores grid cell, cooldown, and tile height.
        /// </summary>
        public bool TryFireOnce(
            TowerInstance tower,
            Vector3 muzzle,
            List<EnemyRuntime> living,
            GemModifierPipeline pipeline,
            StatusRuntime statuses = null)
        {
            if (tower == null || tower.Def == null || pipeline == null || living == null)
                return false;

            if (tower.Def.HasRole<CurseRoleDefinition>())
            {
                ApplyMuzzleLocalY(ref muzzle, tower);
                ApplyCursePresence(tower, living, pipeline, statuses, muzzle, 1f);
                return true;
            }

            if (!tower.Def.IsFireable)
                return false;

            if (HasPendingStrike(tower))
                return true;

            var modifiers = ListPool<ISkillModifier>.Get();
            var baseline = pipeline.ResolveBaseline(tower);
            var spec = pipeline.Resolve(tower, modifiers);
            ListPool<ISkillModifier>.Release(modifiers);

            var gemMul = spec.RangeMultiplier > 0.01f ? spec.RangeMultiplier : 1f;
            var range = tower.Def.GetFireTowerRadius(tower.Level) * gemMul;
            ApplyMuzzleLocalY(ref muzzle, tower);
            if (!_selector.TrySelect(tower.Targeting, muzzle, range, living, out var primary))
                return false;

            BeginResolvedVolley(muzzle, range, primary, spec, baseline, living, statuses, tower);
            return true;
        }

        void BeginResolvedVolley(
            Vector3 muzzle,
            float range,
            EnemyRuntime primary,
            SkillSpec spec,
            SkillSpec baseline,
            List<EnemyRuntime> living,
            StatusRuntime statuses,
            TowerInstance tower)
        {
            var speedMul = spec.ProjectileSpeedMultiplier > 0.01f ? spec.ProjectileSpeedMultiplier : 1f;
            var speed = _projectileSpeed * speedMul;
            var aimPoint = spec.AimMode == AimMode.Ground
                ? PathIntercept.Predict(muzzle, speed, primary)
                : primary.WorldPosition;
            var interval = 0f;
            if (tower != null && tower.Def != null)
                interval = tower.Def.FireInterval(spec, tower.Level);
            if (tower != null)
            {
                tower.CurrentFireInterval = interval;
                tower.NotifyFired(aimPoint);
            }

            var contact = tower != null
                ? TowerAttackPlayback.ContactDelay(interval, tower.StrikeNormalized)
                : interval;
            if (tower != null && contact > 0.0001f)
            {
                _pendingStrikes.Add(new PendingStrike
                {
                    Tower = tower,
                    Muzzle = muzzle,
                    Range = range,
                    Primary = primary,
                    Spec = spec,
                    Baseline = baseline,
                    Statuses = statuses,
                    Remaining = contact
                });
                return;
            }

            SpawnResolvedVolley(muzzle, range, primary, spec, baseline, living, statuses, tower);
        }

        bool HasPendingStrike(TowerInstance tower)
        {
            if (tower == null)
                return false;
            for (var i = 0; i < _pendingStrikes.Count; i++)
            {
                if (_pendingStrikes[i].Tower == tower)
                    return true;
            }

            return false;
        }

        bool ResolvedThisTick(TowerInstance tower)
        {
            if (tower == null)
                return false;
            for (var i = 0; i < _resolvedCasts.Count; i++)
            {
                if (_resolvedCasts[i] == tower)
                    return true;
            }

            return false;
        }

        void TickPendingStrikes(float dt, List<EnemyRuntime> living)
        {
            for (var i = _pendingStrikes.Count - 1; i >= 0; i--)
            {
                var pending = _pendingStrikes[i];
                pending.Remaining -= dt;
                if (pending.Remaining > 0f)
                    continue;

                _pendingStrikes.RemoveAt(i);
                if (pending.Tower != null)
                    _resolvedCasts.Add(pending.Tower);
                var primaryDead = pending.Primary == null || !pending.Primary.IsAlive;
                if (primaryDead && pending.Spec.DeliveryPattern != DeliveryPattern.GroundPulse)
                    continue;

                SpawnResolvedVolley(
                    pending.Muzzle,
                    pending.Range,
                    pending.Primary,
                    pending.Spec,
                    pending.Baseline,
                    living,
                    pending.Statuses,
                    pending.Tower);
            }
        }

        void SpawnResolvedVolley(
            Vector3 muzzle,
            float range,
            EnemyRuntime primary,
            SkillSpec spec,
            SkillSpec baseline,
            List<EnemyRuntime> living,
            StatusRuntime statuses,
            TowerInstance tower)
        {
            var speedMul = spec.ProjectileSpeedMultiplier > 0.01f ? spec.ProjectileSpeedMultiplier : 1f;
            var speed = _projectileSpeed * speedMul;
            var volleys = spec.EchoVolleyCount >= 2 ? spec.EchoVolleyCount : 1;
            var echoFactor = volleys > 1 ? spec.EchoDamageFactor : 1f;
            if (echoFactor <= 0f)
                echoFactor = 1f;
            var volleyMin = spec.DamageMin * echoFactor;
            var volleyMax = spec.DamageMax * echoFactor;
            var hydra = EvolutionEvaluator.IsHydraTower(tower);
            var aimPoint = spec.AimMode == AimMode.Ground
                ? PathIntercept.Predict(muzzle, speed, primary)
                : primary.WorldPosition;
            if (spec.DeliveryPattern == DeliveryPattern.Rain)
                CancelRainFrom(tower);

            for (var v = 0; v < volleys; v++)
            {
                if (spec.DeliveryPattern == DeliveryPattern.WarpStrike)
                {
                    SpawnWarpStrike(
                        muzzle,
                        primary,
                        spec,
                        baseline,
                        volleyMin,
                        volleyMax,
                        speed,
                        statuses,
                        tower);
                    continue;
                }

                if (spec.DeliveryPattern == DeliveryPattern.CasterNova)
                {
                    ApplyCasterNova(
                        muzzle,
                        range,
                        spec,
                        volleyMin,
                        volleyMax,
                        living,
                        statuses,
                        tower.Def);
                    continue;
                }

                if (spec.DeliveryPattern == DeliveryPattern.GroundPulse)
                {
                    ApplyGroundPulse(
                        aimPoint,
                        primary,
                        spec,
                        volleyMin,
                        volleyMax,
                        living,
                        statuses,
                        tower.Def);
                    SpawnImmediateSlamVisual(aimPoint, spec, statuses, tower);
                    SpawnDelayedStationaryPulses(
                        aimPoint,
                        spec,
                        baseline,
                        speed,
                        statuses,
                        tower);
                    continue;
                }

                if (spec.DeliveryPattern == DeliveryPattern.Rain)
                {
                    SpawnRain(
                        aimPoint,
                        spec,
                        baseline,
                        speed,
                        statuses,
                        tower);
                    continue;
                }

                if (spec.DeliveryPattern == DeliveryPattern.PayloadNova)
                {
                    SpawnPayloadNova(
                        muzzle,
                        aimPoint,
                        spec,
                        ProjectileRuntime.DefaultChainRange,
                        volleyMin,
                        volleyMax,
                        speed,
                        statuses,
                        tower.Def);
                    continue;
                }

                if (hydra)
                {
                    var laterals = EvolutionEvaluator.HydraHeadLateralOffsets;
                    var yaws = EvolutionEvaluator.HydraHeadYawOffsets;
                    for (var h = 0; h < laterals.Length; h++)
                        SpawnVolley(muzzle, aimPoint, primary, spec, ProjectileRuntime.DefaultChainRange, volleyMin, volleyMax, speed, statuses, tower.Def, yaws[h], laterals[h], shotIndex: -1, maxTravel: range);
                }
                else if (spec.SequentialIntervalSeconds > 0.001f && spec.ProjectileCount > 1)
                {
                    SpawnVolley(
                        muzzle,
                        aimPoint,
                        primary,
                        spec,
                        ProjectileRuntime.DefaultChainRange,
                        volleyMin,
                        volleyMax,
                        speed,
                        statuses,
                        tower.Def,
                        0f,
                        0f,
                        shotIndex: 0,
                        maxTravel: range);
                    _pendingSequential.Add(
                        new PendingSequentialVolley
                        {
                            Origin = muzzle,
                            AimPoint = aimPoint,
                            Primary = primary,
                            Spec = spec,
                            DamageMin = volleyMin,
                            DamageMax = volleyMax,
                            Speed = speed,
                            ChainRange = ProjectileRuntime.DefaultChainRange,
                            Statuses = statuses,
                            SourceTower = tower.Def,
                            NextIndex = 1,
                            Wait = spec.SequentialIntervalSeconds,
                            MaxTravel = range
                        });
                }
                else
                {
                    SpawnVolley(muzzle, aimPoint, primary, spec, ProjectileRuntime.DefaultChainRange, volleyMin, volleyMax, speed, statuses, tower.Def, 0f, 0f, shotIndex: -1, maxTravel: range);
                }
            }
        }

        void MergeSpawnBuffer()
        {
            for (var i = 0; i < _spawnBuffer.Count; i++)
                _projectiles.Add(_spawnBuffer[i]);
            _spawnBuffer.Clear();
        }

        void SpawnWarpStrike(
            Vector3 origin,
            EnemyRuntime primary,
            SkillSpec spec,
            SkillSpec baseline,
            float damageMin,
            float damageMax,
            float speed,
            StatusRuntime statuses,
            TowerInstance sourceTower)
        {
            var warp = new ProjectileRuntime();
            warp.InitWarpStrike(
                origin,
                primary,
                spec,
                RoleStatValue.SampleHitDamage(damageMin, damageMax),
                speed,
                ProjectileRuntime.DefaultChainRange,
                statuses,
                _spawnBuffer,
                sourceTower.Def,
                _recordDamage,
                (anchor, landedSpec) => SpawnOnImpactPayloads(
                    anchor,
                    landedSpec,
                    baseline,
                    speed,
                    statuses,
                    sourceTower));
            _projectiles.Add(warp);
        }

        void SpawnOnImpactPayloads(
            Vector3 anchor,
            SkillSpec spec,
            SkillSpec baseline,
            float speed,
            StatusRuntime statuses,
            TowerInstance sourceTower)
        {
            if (sourceTower == null || sourceTower.Def == null)
                return;

            GemModifierPipeline.CollectEffectPayloads(
                sourceTower,
                _payloadDefinitionsScratch);
            if (_payloadDefinitionsScratch.Count == 0)
                return;

            _payloadPlanScratch.Clear();
            EffectPayloadResolver.BuildOnImpact(
                _payloadDefinitionsScratch,
                spec,
                baseline,
                anchor,
                _payloadRng,
                _payloadPlanScratch);

            for (var i = 0; i < _payloadPlanScratch.Count; i++)
            {
                var plan = _payloadPlanScratch[i];
                var flight = ResolvePayloadFlightSeconds(plan, speed);
                var runtime = new EffectPayloadRuntime();
                runtime.Init(plan, flight, statuses, sourceTower.Def, _recordDamage, sourceTower);
                _effectPayloads.Add(runtime);
            }
        }

        void SpawnDelayedStationaryPulses(
            Vector3 anchor,
            SkillSpec spec,
            SkillSpec baseline,
            float speed,
            StatusRuntime statuses,
            TowerInstance sourceTower)
        {
            if (sourceTower == null || sourceTower.Def == null)
                return;

            GemModifierPipeline.CollectEffectPayloads(
                sourceTower,
                _payloadDefinitionsScratch);
            if (_payloadDefinitionsScratch.Count == 0)
                return;

            _payloadPlanScratch.Clear();
            EffectPayloadResolver.BuildDelayedStationaryPulse(
                _payloadDefinitionsScratch,
                spec,
                baseline,
                anchor,
                _payloadRng,
                _payloadPlanScratch);

            for (var i = 0; i < _payloadPlanScratch.Count; i++)
            {
                var plan = _payloadPlanScratch[i];
                var flight = ResolvePayloadFlightSeconds(plan, speed);
                var runtime = new EffectPayloadRuntime();
                runtime.Init(plan, flight, statuses, sourceTower.Def, _recordDamage, sourceTower);
                _effectPayloads.Add(runtime);
            }
        }

        void SpawnImmediateSlamVisual(
            Vector3 aimPoint,
            SkillSpec spec,
            StatusRuntime statuses,
            TowerInstance sourceTower)
        {
            if (spec.AoeRadius <= 0f || sourceTower == null || sourceTower.Def == null)
                return;

            var plan = new EffectPayloadPlan
            {
                Trigger = EffectPayloadTrigger.AfterDelay,
                TravelPattern = EffectPayloadTravelPattern.StationaryPulse,
                HitPolicy = EffectPayloadHitPolicy.PerImpact,
                Origin = aimPoint,
                LandingPoint = aimPoint,
                DamageMin = 0f,
                DamageMax = 0f,
                AoeRadius = spec.AoeRadius,
                DelaySeconds = 0f,
                HitSpec = spec,
                Visual = EffectPayloadVisual.Slam
            };
            var runtime = new EffectPayloadRuntime();
            runtime.Init(
                plan,
                EffectPayloadRuntime.MinFlightSeconds,
                statuses,
                sourceTower.Def,
                _recordDamage,
                sourceTower);
            _effectPayloads.Add(runtime);
        }

        static float ResolvePayloadFlightSeconds(in EffectPayloadPlan plan, float speed)
        {
            if (plan.TravelPattern == EffectPayloadTravelPattern.FallFromSky)
            {
                var drop = plan.ArcHeight > 0.01f ? plan.ArcHeight : 3f;
                var rainSpeed = speed > 0.01f ? speed : ProjectileRuntime.DefaultProjectileSpeed;
                return Mathf.Clamp(drop / rainSpeed, EffectPayloadRuntime.MinFlightSeconds, 0.45f);
            }

            if (plan.TravelPattern != EffectPayloadTravelPattern.Fountain)
                return EffectPayloadRuntime.MinFlightSeconds;

            var safeSpeed = speed > 0.01f ? speed : ProjectileRuntime.DefaultProjectileSpeed;
            var dist = plan.HorizontalDistance;
            return Mathf.Clamp(dist / safeSpeed, 0.12f, 0.45f);
        }

        void SpawnRain(
            Vector3 aimPoint,
            SkillSpec spec,
            SkillSpec baseline,
            float speed,
            StatusRuntime statuses,
            TowerInstance sourceTower)
        {
            if (sourceTower == null || sourceTower.Def == null)
                return;

            GemModifierPipeline.CollectEffectPayloads(sourceTower, _payloadDefinitionsScratch);
            if (_payloadDefinitionsScratch.Count == 0)
                return;

            _payloadPlanScratch.Clear();
            EffectPayloadResolver.BuildFallingRain(
                _payloadDefinitionsScratch,
                spec,
                baseline,
                aimPoint,
                _payloadRng,
                _payloadPlanScratch);

            for (var i = 0; i < _payloadPlanScratch.Count; i++)
            {
                var plan = _payloadPlanScratch[i];
                var flight = ResolvePayloadFlightSeconds(plan, speed);
                var runtime = new EffectPayloadRuntime();
                runtime.Init(plan, flight, statuses, sourceTower.Def, _recordDamage, sourceTower);
                _effectPayloads.Add(runtime);
            }
        }

        void CancelRainFrom(TowerInstance tower)
        {
            if (tower == null)
                return;

            for (var i = _effectPayloads.Count - 1; i >= 0; i--)
            {
                var payload = _effectPayloads[i];
                if (payload.Owner != tower)
                    continue;

                payload.Deactivate();
                _effectPayloads.RemoveAt(i);
            }
        }

        void ApplyCursePresence(
            TowerInstance tower,
            List<EnemyRuntime> living,
            GemModifierPipeline pipeline,
            StatusRuntime statuses)
        {
            if (tower == null || tower.Def == null)
                return;

            var towerPos = CellToWorld(tower.Cell);
            var muzzle = towerPos;
            var heightMul = 1f;
            if (_heights != null)
            {
                var layer = _heights.Get(tower.Cell.x, tower.Cell.y);
                muzzle.y = TileHeightVisual.TopY(layer);
                heightMul = TileHeightRules.RangeMultiplier(layer);
            }

            ApplyMuzzleLocalY(ref muzzle, tower);
            ApplyCursePresence(tower, living, pipeline, statuses, muzzle, heightMul);
        }

        void ApplyCursePresence(
            TowerInstance tower,
            List<EnemyRuntime> living,
            GemModifierPipeline pipeline,
            StatusRuntime statuses,
            Vector3 muzzle,
            float heightMul)
        {
            if (statuses == null || living == null || tower == null || tower.Def == null)
                return;

            var role = tower.Def.GetRole<CurseRoleDefinition>();
            if (role == null || !CurseHex.TryResolve(role, tower.Level, out var id, out var magnitude))
                return;

            var modifiers = ListPool<ISkillModifier>.Get();
            var spec = pipeline.Resolve(tower, modifiers);
            ListPool<ISkillModifier>.Release(modifiers);

            var gemMul = spec.RangeMultiplier > 0.01f ? spec.RangeMultiplier : 1f;
            if (heightMul <= 0f)
                heightMul = 1f;

            var range = tower.Def.GetFireTowerRadius(tower.Level) * gemMul * heightMul;
            _casterNovaScratch.Clear();
            AreaEffectResolver.CollectCircle(
                muzzle,
                range,
                living,
                _casterNovaScratch,
                EffectPayloadHitPolicy.PerImpact);
            for (var i = 0; i < _casterNovaScratch.Count; i++)
            {
                var enemy = _casterNovaScratch[i];
                var scaled = CurseHex.ScaleMagnitude(enemy, magnitude);
                if (scaled == 0f)
                    continue;

                statuses.Apply(
                    enemy,
                    id,
                    CurseHex.PresenceDuration,
                    scaled);
            }
        }

        void ApplyCasterNova(
            Vector3 muzzle,
            float radius,
            SkillSpec spec,
            float damageMin,
            float damageMax,
            List<EnemyRuntime> living,
            StatusRuntime statuses,
            TowerDefinition sourceTower)
        {
            if (radius <= 0f || living == null)
                return;

            var damage = RoleStatValue.SampleHitDamage(damageMin, damageMax);
            _casterNovaScratch.Clear();
            AreaEffectResolver.CollectCircle(
                muzzle,
                radius,
                living,
                _casterNovaScratch,
                EffectPayloadHitPolicy.PerImpact);
            for (var i = 0; i < _casterNovaScratch.Count; i++)
                ApplyPulseDamage(_casterNovaScratch[i], damage, spec, statuses, sourceTower);
        }

        void ApplyGroundPulse(
            Vector3 pulseOrigin,
            EnemyRuntime primary,
            SkillSpec spec,
            float damageMin,
            float damageMax,
            List<EnemyRuntime> living,
            StatusRuntime statuses,
            TowerDefinition sourceTower)
        {
            if (primary == null || !primary.IsAlive)
                return;

            var damage = RoleStatValue.SampleHitDamage(damageMin, damageMax);
            ApplyPulseDamage(primary, damage, spec, statuses, sourceTower);

            var radius = spec.AoeRadius;
            if (radius <= 0f || living == null)
                return;

            var radiusSq = radius * radius;
            for (var i = 0; i < living.Count; i++)
            {
                var enemy = living[i];
                if (enemy == null || !enemy.IsAlive || ReferenceEquals(enemy, primary))
                    continue;

                if ((enemy.WorldPosition - pulseOrigin).sqrMagnitude <= radiusSq)
                    ApplyPulseDamage(enemy, damage, spec, statuses, sourceTower);
            }
        }

        void ApplyPulseDamage(
            EnemyRuntime enemy,
            float damage,
            SkillSpec spec,
            StatusRuntime statuses,
            TowerDefinition sourceTower)
        {
            if (sourceTower != null)
                enemy.LastDamageSource = sourceTower;

            if (statuses != null)
                statuses.ApplyDamage(enemy, damage, spec);
            else
                enemy.ApplyDamage(damage, spec, null);

            _recordDamage?.Invoke(sourceTower, damage);
        }

        void SpawnPayloadNova(
            Vector3 origin,
            Vector3 aimPoint,
            SkillSpec spec,
            float chainRange,
            float damageMin,
            float damageMax,
            float speed,
            StatusRuntime statuses,
            TowerDefinition sourceTower)
        {
            if (spec.ProjectileCount <= 0)
                return;

            var payload = new ProjectileRuntime();
            payload.InitPayload(
                origin,
                aimPoint,
                spec,
                damageMin,
                damageMax,
                speed,
                chainRange,
                statuses,
                _spawnBuffer,
                sourceTower,
                _recordDamage);

            if ((aimPoint - origin).sqrMagnitude
                <= ProjectileRuntime.HitRadius * ProjectileRuntime.HitRadius)
            {
                payload.ExplodePayload();
                MergeSpawnBuffer();
                return;
            }

            _projectiles.Add(payload);
        }

        void SpawnVolley(
            Vector3 origin,
            Vector3 aimPoint,
            EnemyRuntime primary,
            SkillSpec spec,
            float chainRange,
            float damageMin,
            float damageMax,
            float speed,
            StatusRuntime statuses,
            TowerDefinition sourceTower,
            float headYawDegrees,
            float headLateral,
            int shotIndex,
            float maxTravel)
        {
            var aim = aimPoint - origin;
            if (Mathf.Abs(headLateral) > 1e-4f)
            {
                var flat = aim;
                flat.y = 0f;
                var lateralBasis = flat.sqrMagnitude > 1e-8f ? flat.normalized : Vector3.forward;
                origin += Quaternion.Euler(0f, 90f, 0f) * lateralBasis * headLateral;
                aim = aimPoint - origin;
            }

            if (aim.sqrMagnitude < 1e-8f)
                aim = Vector3.forward;
            else
                aim.Normalize();

            if (Mathf.Abs(headYawDegrees) > 1e-4f)
                aim = Quaternion.Euler(0f, headYawDegrees, 0f) * aim;

            var pierceRemaining = spec.GetPierceRemaining();
            var forkRemaining = spec.ForkCount;
            var count = spec.ProjectileCount;
            var start = shotIndex >= 0 ? shotIndex : 0;
            var end = shotIndex >= 0 ? shotIndex + 1 : count;
            if (end > count)
                end = count;

            for (var i = start; i < end; i++)
            {
                var yaw = 0f;
                if (count > 1 && spec.SpreadDegrees > 0f)
                {
                    var t = i / (float)(count - 1);
                    yaw = Mathf.Lerp(-spec.SpreadDegrees * 0.5f, spec.SpreadDegrees * 0.5f, t);
                }

                var dir = Quaternion.Euler(0f, yaw, 0f) * aim;

                var projectile = new ProjectileRuntime();
                projectile.Init(
                    origin,
                    dir,
                    primary,
                    RoleStatValue.SampleHitDamage(damageMin, damageMax),
                    spec.ChainCount,
                    speed,
                    chainRange,
                    spec.AoeRadius,
                    pierceRemaining,
                    forkRemaining,
                    spec.Ignite,
                    spec.Chill,
                    spec.Shock,
                    spec.Proliferate,
                    statuses,
                    _spawnBuffer,
                    softSeek: false,
                    seekOffset: default,
                    sourceTower,
                    _recordDamage,
                    spec.KnockbackChance,
                    spec.KnockbackDistance,
                    spec.ChainHopFalloff,
                    spec.BleedChance,
                    spec.BleedDamageMultiplier,
                    AilmentTune.FromSkillSpec(spec),
                    spec,
                    maxTravel);
                _projectiles.Add(projectile);
            }
        }

        void TickPendingSequential(float dt, List<EnemyRuntime> living)
        {
            for (var i = _pendingSequential.Count - 1; i >= 0; i--)
            {
                var pending = _pendingSequential[i];
                pending.Wait -= dt;
                while (pending.Wait <= 0f && pending.NextIndex < pending.Spec.ProjectileCount)
                {
                    SpawnVolley(
                        pending.Origin,
                        pending.AimPoint,
                        pending.Primary,
                        pending.Spec,
                        pending.ChainRange,
                        pending.DamageMin,
                        pending.DamageMax,
                        pending.Speed,
                        pending.Statuses,
                        pending.SourceTower,
                        0f,
                        0f,
                        pending.NextIndex,
                        pending.MaxTravel);
                    pending.NextIndex++;
                    pending.Wait += pending.Spec.SequentialIntervalSeconds;
                }

                if (pending.NextIndex >= pending.Spec.ProjectileCount)
                    _pendingSequential.RemoveAt(i);
            }

            MergeSpawnBuffer();
        }

        sealed class PendingSequentialVolley
        {
            public Vector3 Origin;
            public Vector3 AimPoint;
            public EnemyRuntime Primary;
            public SkillSpec Spec;
            public float DamageMin;
            public float DamageMax;
            public float Speed;
            public float ChainRange;
            public StatusRuntime Statuses;
            public TowerDefinition SourceTower;
            public int NextIndex;
            public float Wait;
            public float MaxTravel;
        }

        sealed class PendingStrike
        {
            public TowerInstance Tower;
            public Vector3 Muzzle;
            public float Range;
            public EnemyRuntime Primary;
            public SkillSpec Spec;
            public SkillSpec Baseline;
            public StatusRuntime Statuses;
            public float Remaining;
        }

        static void ApplyMuzzleLocalY(ref Vector3 muzzle, TowerInstance tower)
        {
            if (tower == null || tower.MuzzleLocalY == 0f)
                return;
            muzzle.y += tower.MuzzleLocalY;
        }

        Vector3 CellToWorld(Vector2Int cell)
        {
            var half = _cellSize * 0.5f;
            return new Vector3(cell.x * _cellSize + half, 0f, cell.y * _cellSize + half);
        }
    }
}
