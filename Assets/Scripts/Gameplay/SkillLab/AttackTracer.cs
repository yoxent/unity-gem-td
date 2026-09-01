using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.SkillLab
{
    /// <summary>
    /// Mode B snapshot of a volley tree. Duplicates ProjectileRuntime hop order (Pierce → Fork → Chain)
    /// without applying damage. Chain tie-break must match ProjectileRuntime (first equal-distance wins).
    /// </summary>
    public sealed class AttackTracer
    {
        public const int DefaultPayloadSeed = 12345;

        readonly float _baseProjectileSpeed;
        readonly TargetSelector _selector = new TargetSelector();
        readonly GemModifierPipeline _pipeline = new GemModifierPipeline();
        readonly List<ISkillModifier> _scratch = new List<ISkillModifier>(8);
        readonly List<SimShot> _queue = new List<SimShot>(32);
        readonly List<EffectPayloadDefinition> _payloadDefinitionsScratch =
            new List<EffectPayloadDefinition>(8);
        readonly List<EffectPayloadPlan> _payloadPlans = new List<EffectPayloadPlan>(8);
        readonly List<Vector3> _polylineScratch = new List<Vector3>(16);
        readonly List<EnemyRuntime> _impactScratch = new List<EnemyRuntime>(8);

        public AttackTracer(float baseProjectileSpeed = ProjectileRuntime.DefaultProjectileSpeed)
        {
            _baseProjectileSpeed = baseProjectileSpeed > 0f
                ? baseProjectileSpeed
                : ProjectileRuntime.DefaultProjectileSpeed;
        }

        struct SimShot
        {
            public Vector3 Position;
            public Vector3 Direction;
            public float Damage;
            public float ProjectileSpeed;
            public float RemainingFlight;
            public int ChainRemaining;
            public int PierceRemaining;
            public int ForkRemaining;
            public float AoeRadius;
            public float ChainRange;
            public float ChainHopFalloff;
            public AttackTraceKind Kind;
            public EnemyRuntime LastHit;
        }

        public AttackTrace Trace(TowerInstance tower, Vector3 origin, List<EnemyRuntime> dummies)
        {
            return Trace(tower, origin, dummies, new System.Random(DefaultPayloadSeed));
        }

        public AttackTrace Trace(
            TowerInstance tower,
            Vector3 origin,
            List<EnemyRuntime> dummies,
            System.Random payloadRng)
        {
            return Trace(tower, origin, dummies, payloadRng, includeRandomPayloads: true);
        }

        public AttackTrace Trace(
            TowerInstance tower,
            Vector3 origin,
            List<EnemyRuntime> dummies,
            System.Random payloadRng,
            bool includeRandomPayloads)
        {
            var trace = new AttackTrace();
            if (tower == null
                || tower.Def == null
                || !tower.Def.IsFireable)
                return trace;
            if (dummies == null)
                return trace;

            var isCurse = tower.Def.HasRole<CurseRoleDefinition>();
            if (!isCurse && dummies.Count == 0)
                return trace;
            if (includeRandomPayloads && payloadRng == null)
                payloadRng = new System.Random(DefaultPayloadSeed);

            var baseline = _pipeline.ResolveBaseline(tower);
            var spec = _pipeline.Resolve(tower, _scratch);
            var rangeMul = spec.RangeMultiplier > 0.01f ? spec.RangeMultiplier : 1f;
            var range = tower.Def.GetFireTowerRadius(tower.Level) * rangeMul;
            if (isCurse)
            {
                if (range <= 0f)
                    return trace;
                trace.HasTarget = true;
                TraceCasterNova(trace, origin, range, dummies);
                return trace;
            }

            if (dummies.Count == 0)
                return trace;
            if (!_selector.TrySelect(tower.Targeting, origin, range, dummies, out var primary) || primary == null)
                return trace;

            trace.HasTarget = true;
            _queue.Clear();

            var pierceRemaining = spec.GetPierceRemaining();
            var damage = spec.Damage;
            var speedMul = spec.ProjectileSpeedMultiplier > 0.01f ? spec.ProjectileSpeedMultiplier : 1f;
            var speed = _baseProjectileSpeed * speedMul;
            var aimPoint = spec.AimMode == AimMode.Ground
                ? PathIntercept.Predict(origin, speed, primary)
                : primary.WorldPosition;

            var volleys = spec.EchoVolleyCount >= 2 ? spec.EchoVolleyCount : 1;
            var echoFactor = volleys > 1 ? spec.EchoDamageFactor : 1f;
            if (echoFactor <= 0f)
                echoFactor = 1f;

            if (spec.DeliveryPattern == DeliveryPattern.CasterNova)
            {
                TraceCasterNova(trace, origin, range, dummies);
            }
            else if (spec.DeliveryPattern == DeliveryPattern.GroundPulse)
            {
                TraceGroundPulse(trace, aimPoint, primary, spec, dummies, tower);
            }
            else if (spec.DeliveryPattern == DeliveryPattern.WarpStrike)
            {
                for (var v = 0; v < volleys; v++)
                {
                    TraceWarpStrike(
                        trace,
                        origin,
                        primary,
                        spec,
                        baseline,
                        damage * echoFactor,
                        dummies,
                        tower,
                        payloadRng,
                        includeRandomPayloads);
                }
            }
            else if (spec.DeliveryPattern == DeliveryPattern.PayloadNova)
            {
                for (var v = 0; v < volleys; v++)
                    TracePayloadNova(trace, origin, aimPoint, spec, damage * echoFactor);
            }
            else if (spec.DeliveryPattern == DeliveryPattern.Rain)
            {
                for (var v = 0; v < volleys; v++)
                {
                    TraceRain(
                        trace,
                        aimPoint,
                        spec,
                        baseline,
                        dummies,
                        tower,
                        payloadRng,
                        includeRandomPayloads);
                }
            }
            else if (EvolutionEvaluator.IsHydraTower(tower))
            {
                var laterals = EvolutionEvaluator.HydraHeadLateralOffsets;
                var yaws = EvolutionEvaluator.HydraHeadYawOffsets;
                for (var v = 0; v < volleys; v++)
                {
                    var volleyDamage = damage * echoFactor;
                    for (var h = 0; h < laterals.Length; h++)
                    {
                        var isCenter = Mathf.Abs(yaws[h]) < 1e-4f && Mathf.Abs(laterals[h]) < 1e-4f;
                        EnqueueVolley(
                            origin,
                            aimPoint,
                            spec,
                            volleyDamage,
                            pierceRemaining,
                            yaws[h],
                            laterals[h],
                            isCenter ? AttackTraceKind.Primary : AttackTraceKind.HydraHead);
                    }
                }
            }
            else
            {
                for (var v = 0; v < volleys; v++)
                {
                    EnqueueVolley(
                        origin,
                        aimPoint,
                        spec,
                        damage * echoFactor,
                        pierceRemaining,
                        0f,
                        0f,
                        AttackTraceKind.Primary);
                }
            }

            Simulate(trace, dummies);
            return trace;
        }

        void TracePayloadNova(
            AttackTrace trace,
            Vector3 origin,
            Vector3 aimPoint,
            SkillSpec spec,
            float damage)
        {
            var count = spec.ProjectileCount;
            if (count <= 0)
                return;

            AddSegment(trace, origin, aimPoint, AttackTraceKind.PayloadNova, damage);

            var speed = _baseProjectileSpeed
                * (spec.ProjectileSpeedMultiplier > 0.01f
                    ? spec.ProjectileSpeedMultiplier
                    : 1f);
            var step = 360f / count;
            var pierceRemaining = spec.GetPierceRemaining();
            for (var i = 0; i < count; i++)
            {
                var direction = Quaternion.Euler(0f, i * step, 0f) * Vector3.forward;
                _queue.Add(new SimShot
                {
                    Position = aimPoint,
                    Direction = direction.sqrMagnitude > 1e-8f
                        ? direction.normalized
                        : Vector3.forward,
                    Damage = damage,
                    ProjectileSpeed = speed,
                    RemainingFlight = speed * ProjectileRuntime.MaxLifetimeSeconds,
                    ChainRemaining = spec.ChainCount,
                    PierceRemaining = pierceRemaining,
                    ForkRemaining = spec.ForkCount,
                    AoeRadius = spec.AoeRadius,
                    ChainRange = ProjectileRuntime.DefaultChainRange,
                    ChainHopFalloff = spec.ChainHopFalloff == 0f ? 1f : spec.ChainHopFalloff,
                    Kind = AttackTraceKind.PayloadNova,
                    LastHit = null
                });
            }
        }

        void TraceWarpStrike(
            AttackTrace trace,
            Vector3 origin,
            EnemyRuntime primary,
            SkillSpec spec,
            SkillSpec baseline,
            float damage,
            List<EnemyRuntime> dummies,
            TowerInstance tower,
            System.Random payloadRng,
            bool includeRandomPayloads)
        {
            var riseTop = origin + Vector3.up * ProjectileRuntime.WarpRiseHeight;
            AddSegment(trace, origin, riseTop, AttackTraceKind.WarpRise, 0f);

            var landPoint = primary.WorldPosition;
            var dropStart = landPoint + Vector3.up * ProjectileRuntime.WarpDropHeight;
            AddSegment(trace, dropStart, landPoint, AttackTraceKind.WarpDrop, damage);
            trace.Discs.Add(new AttackTraceDisc
            {
                Center = landPoint,
                Radius = 0.25f,
                Kind = AttackTraceKind.WarpDrop
            });
            AddHitTarget(trace, primary);
            RecordAoeTargets(trace, landPoint, primary, spec.AoeRadius, dummies);

            if (!includeRandomPayloads || tower == null || tower.Def == null)
                return;

            GemModifierPipeline.CollectEffectPayloads(
                tower,
                _payloadDefinitionsScratch);
            if (_payloadDefinitionsScratch.Count == 0)
                return;

            _payloadPlans.Clear();
            EffectPayloadResolver.BuildOnImpact(
                _payloadDefinitionsScratch,
                spec,
                baseline,
                landPoint,
                payloadRng,
                _payloadPlans);

            for (var i = 0; i < _payloadPlans.Count; i++)
                AppendPayloadPlan(trace, _payloadPlans[i], dummies, AttackTraceKind.Magma);
        }

        void TraceCasterNova(
            AttackTrace trace,
            Vector3 origin,
            float radius,
            List<EnemyRuntime> dummies)
        {
            if (radius <= 0f)
                return;

            trace.Discs.Add(new AttackTraceDisc
            {
                Center = origin,
                Radius = radius,
                Kind = AttackTraceKind.Aoe
            });

            _impactScratch.Clear();
            AreaEffectResolver.CollectCircle(
                origin,
                radius,
                dummies,
                _impactScratch,
                EffectPayloadHitPolicy.PerImpact);
            for (var i = 0; i < _impactScratch.Count; i++)
                AddHitTarget(trace, _impactScratch[i]);
        }

        void TraceRain(
            AttackTrace trace,
            Vector3 aimPoint,
            SkillSpec spec,
            SkillSpec baseline,
            List<EnemyRuntime> dummies,
            TowerInstance tower,
            System.Random payloadRng,
            bool includeRandomPayloads)
        {
            if (tower == null || tower.Def == null)
                return;

            GemModifierPipeline.CollectEffectPayloads(tower, _payloadDefinitionsScratch);
            if (_payloadDefinitionsScratch.Count == 0)
                return;

            var stormRadius = 0f;
            for (var d = 0; d < _payloadDefinitionsScratch.Count; d++)
            {
                var def = _payloadDefinitionsScratch[d];
                if (def == null
                    || def.TravelPattern != EffectPayloadTravelPattern.FallFromSky
                    || !def.IsValid)
                    continue;
                if (def.MaxDistance > stormRadius)
                    stormRadius = def.MaxDistance;
            }

            if (stormRadius > 0f)
            {
                trace.Discs.Add(new AttackTraceDisc
                {
                    Center = aimPoint,
                    Radius = stormRadius,
                    Kind = AttackTraceKind.Aoe
                });
            }

            if (!includeRandomPayloads)
                return;

            _payloadPlans.Clear();
            EffectPayloadResolver.BuildFallingRain(
                _payloadDefinitionsScratch,
                spec,
                baseline,
                aimPoint,
                payloadRng,
                _payloadPlans);

            for (var i = 0; i < _payloadPlans.Count; i++)
                AppendPayloadPlan(trace, _payloadPlans[i], dummies, AttackTraceKind.Rain);
        }

        public void AppendPayload(
            AttackTrace trace,
            in EffectPayloadPlan plan,
            List<EnemyRuntime> dummies)
        {
            if (trace == null)
                return;

            var kind = AttackTraceKind.Magma;
            if (plan.TravelPattern == EffectPayloadTravelPattern.FallFromSky)
                kind = AttackTraceKind.Rain;
            else if (plan.TravelPattern == EffectPayloadTravelPattern.StationaryPulse)
                kind = AttackTraceKind.Aftershock;
            AppendPayloadPlan(trace, plan, dummies, kind);
        }

        void AppendPayloadPlan(
            AttackTrace trace,
            in EffectPayloadPlan plan,
            List<EnemyRuntime> dummies,
            AttackTraceKind kind)
        {
            var payloadDamage = (plan.DamageMin + plan.DamageMax) * 0.5f;
            if (plan.TravelPattern == EffectPayloadTravelPattern.Fountain)
            {
                _polylineScratch.Clear();
                FountainTrajectory.SamplePolyline(
                    plan.Origin,
                    plan.LandingPoint,
                    plan.ArcHeight,
                    FountainTrajectory.DefaultSampleCount,
                    _polylineScratch);

                for (var s = 1; s < _polylineScratch.Count; s++)
                {
                    AddSegment(
                        trace,
                        _polylineScratch[s - 1],
                        _polylineScratch[s],
                        kind,
                        payloadDamage);
                }
            }
            else
            {
                AddSegment(trace, plan.Origin, plan.LandingPoint, kind, payloadDamage);
            }

            trace.Discs.Add(new AttackTraceDisc
            {
                Center = plan.LandingPoint,
                Radius = plan.AoeRadius,
                Kind = kind
            });

            _impactScratch.Clear();
            AreaEffectResolver.CollectCircle(
                plan.LandingPoint,
                plan.AoeRadius,
                dummies,
                _impactScratch,
                plan.HitPolicy);
            for (var t = 0; t < _impactScratch.Count; t++)
            {
                var victim = _impactScratch[t];
                AddHitTarget(trace, victim);
                trace.PayloadHitRecords.Add(victim);
            }
        }

        void TraceGroundPulse(
            AttackTrace trace,
            Vector3 pulseOrigin,
            EnemyRuntime primary,
            SkillSpec spec,
            List<EnemyRuntime> dummies,
            TowerInstance tower)
        {
            AddHitTarget(trace, primary);
            RecordAoeTargets(trace, pulseOrigin, primary, spec.AoeRadius, dummies);

            if (tower == null || tower.Def == null)
                return;

            GemModifierPipeline.CollectEffectPayloads(tower, _payloadDefinitionsScratch);
            if (_payloadDefinitionsScratch.Count == 0)
                return;

            var baseline = _pipeline.ResolveBaseline(tower);
            _payloadPlans.Clear();
            EffectPayloadResolver.BuildDelayedStationaryPulse(
                _payloadDefinitionsScratch,
                spec,
                baseline,
                pulseOrigin,
                new System.Random(DefaultPayloadSeed),
                _payloadPlans);
            for (var i = 0; i < _payloadPlans.Count; i++)
                AppendPayloadPlan(trace, _payloadPlans[i], dummies, AttackTraceKind.Aftershock);
        }

        void EnqueueVolley(
            Vector3 origin,
            Vector3 aimPoint,
            SkillSpec spec,
            float damage,
            int pierceRemaining,
            float headYawDegrees,
            float headLateral,
            AttackTraceKind firstKind)
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

            var count = spec.ProjectileCount;
            for (var i = 0; i < count; i++)
            {
                var yaw = 0f;
                if (count > 1 && spec.SpreadDegrees > 0f)
                {
                    var t = i / (float)(count - 1);
                    yaw = Mathf.Lerp(-spec.SpreadDegrees * 0.5f, spec.SpreadDegrees * 0.5f, t);
                }

                var dir = Quaternion.Euler(0f, yaw, 0f) * aim;
                _queue.Add(new SimShot
                {
                    Position = origin,
                    Direction = dir.sqrMagnitude > 1e-8f ? dir.normalized : Vector3.forward,
                    Damage = damage,
                    ProjectileSpeed = _baseProjectileSpeed
                        * (spec.ProjectileSpeedMultiplier > 0.01f
                            ? spec.ProjectileSpeedMultiplier
                            : 1f),
                    RemainingFlight = _baseProjectileSpeed
                        * (spec.ProjectileSpeedMultiplier > 0.01f
                            ? spec.ProjectileSpeedMultiplier
                            : 1f)
                        * ProjectileRuntime.MaxLifetimeSeconds,
                    ChainRemaining = spec.ChainCount,
                    PierceRemaining = pierceRemaining,
                    ForkRemaining = spec.ForkCount,
                    AoeRadius = spec.AoeRadius,
                    ChainRange = ProjectileRuntime.DefaultChainRange,
                    ChainHopFalloff = spec.ChainHopFalloff == 0f ? 1f : spec.ChainHopFalloff,
                    Kind = firstKind,
                    LastHit = null
                });
            }
        }

        void Simulate(AttackTrace trace, List<EnemyRuntime> dummies)
        {
            var q = 0;
            while (q < _queue.Count)
            {
                if (trace.Segments.Count >= AttackTrace.MaxSegments)
                {
                    trace.Truncated = true;
                    return;
                }

                var shot = _queue[q];
                q++;
                SimulateShot(ref shot, trace, dummies);
            }
        }

        void SimulateShot(ref SimShot shot, AttackTrace trace, List<EnemyRuntime> dummies)
        {
            var guard = 0;
            while (guard++ < 64 && shot.RemainingFlight > 1e-4f)
            {
                if (trace.Segments.Count >= AttackTrace.MaxSegments)
                {
                    trace.Truncated = true;
                    return;
                }

                var hit = FindCandidate(shot.Position, shot.Direction, shot.RemainingFlight, shot.LastHit, dummies);
                if (hit == null)
                {
                    var end = shot.Position + shot.Direction * shot.RemainingFlight;
                    AddSegment(trace, shot.Position, end, shot.Kind, shot.Damage);
                    return;
                }

                AddSegment(trace, shot.Position, hit.WorldPosition, shot.Kind, shot.Damage);
                var traveled = Vector3.Distance(shot.Position, hit.WorldPosition);
                shot.RemainingFlight -= traveled;
                shot.Position = hit.WorldPosition;
                shot.LastHit = hit;
                AddHitTarget(trace, hit);

                if (shot.AoeRadius > 0f)
                {
                    trace.Discs.Add(new AttackTraceDisc
                    {
                        Center = hit.WorldPosition,
                        Radius = shot.AoeRadius,
                        Kind = AttackTraceKind.Aoe
                    });

                    var radiusSq = shot.AoeRadius * shot.AoeRadius;
                    for (var i = 0; i < dummies.Count; i++)
                    {
                        var splashTarget = dummies[i];
                        if (splashTarget == null
                            || !splashTarget.IsAlive
                            || ReferenceEquals(splashTarget, hit))
                            continue;
                        if ((splashTarget.WorldPosition - hit.WorldPosition).sqrMagnitude <= radiusSq)
                            AddHitTarget(trace, splashTarget);
                    }
                }

                if (shot.PierceRemaining == ProjectileRuntime.InfinitePierceRemaining
                    || shot.PierceRemaining > 0)
                {
                    if (shot.PierceRemaining > 0)
                        shot.PierceRemaining--;
                    shot.Kind = AttackTraceKind.Pierce;
                    continue;
                }

                if (shot.ForkRemaining > 0)
                {
                    SpawnForkChildren(shot, hit);
                    return;
                }

                if (shot.ChainRemaining > 0)
                {
                    var next = FindNearestOther(shot.Position, hit, shot.ChainRange, dummies);
                    if (next == null)
                        return;
                    shot.Damage *= shot.ChainHopFalloff;
                    shot.ChainRemaining--;
                    var toNext = next.WorldPosition - shot.Position;
                    if (toNext.sqrMagnitude < 1e-8f)
                        return;
                    shot.Direction = toNext.normalized;
                    shot.Kind = AttackTraceKind.Chain;
                    continue;
                }

                return;
            }
        }

        void SpawnForkChildren(SimShot parent, EnemyRuntime hit)
        {
            var inbound = parent.Direction.sqrMagnitude > 1e-8f ? parent.Direction.normalized : Vector3.forward;
            var spawnAt = hit.WorldPosition + inbound * ProjectileRuntime.ForkSpawnForwardPad;
            var count = parent.ForkRemaining;
            for (var i = 0; i < count; i++)
            {
                var yaw = ProjectileRuntime.ForkChildYawDegrees(i, count);
                EnqueueFork(parent, spawnAt, Quaternion.Euler(0f, yaw, 0f) * inbound);
            }
        }

        void EnqueueFork(SimShot parent, Vector3 origin, Vector3 dir)
        {
            _queue.Add(new SimShot
            {
                Position = origin,
                Direction = dir.sqrMagnitude > 1e-8f ? dir.normalized : Vector3.forward,
                Damage = parent.Damage,
                ProjectileSpeed = parent.ProjectileSpeed,
                RemainingFlight = parent.ProjectileSpeed * ProjectileRuntime.MaxLifetimeSeconds,
                ChainRemaining = parent.ChainRemaining,
                PierceRemaining = parent.PierceRemaining,
                ForkRemaining = 0,
                AoeRadius = parent.AoeRadius,
                ChainRange = parent.ChainRange,
                ChainHopFalloff = parent.ChainHopFalloff,
                Kind = AttackTraceKind.Fork,
                LastHit = parent.LastHit
            });
        }

        static void AddSegment(AttackTrace trace, Vector3 from, Vector3 to, AttackTraceKind kind, float damage)
        {
            if (trace.Segments.Count >= AttackTrace.MaxSegments)
            {
                trace.Truncated = true;
                return;
            }

            trace.Segments.Add(new AttackTraceSegment
            {
                From = from,
                To = to,
                Kind = kind,
                Damage = damage
            });
        }

        static void AddHitTarget(AttackTrace trace, EnemyRuntime target)
        {
            if (trace == null || target == null)
                return;

            for (var i = 0; i < trace.HitTargets.Count; i++)
            {
                if (ReferenceEquals(trace.HitTargets[i], target))
                    return;
            }

            trace.HitTargets.Add(target);
        }

        static void RecordAoeTargets(
            AttackTrace trace,
            Vector3 center,
            EnemyRuntime primary,
            float radius,
            List<EnemyRuntime> dummies)
        {
            if (trace == null || radius <= 0f)
                return;

            trace.Discs.Add(new AttackTraceDisc
            {
                Center = center,
                Radius = radius,
                Kind = AttackTraceKind.Aoe
            });

            if (dummies == null)
                return;

            var radiusSq = radius * radius;
            for (var i = 0; i < dummies.Count; i++)
            {
                var target = dummies[i];
                if (target == null || !target.IsAlive || ReferenceEquals(target, primary))
                    continue;
                if ((target.WorldPosition - center).sqrMagnitude <= radiusSq)
                    AddHitTarget(trace, target);
            }
        }

        static EnemyRuntime FindCandidate(
            Vector3 from,
            Vector3 direction,
            float maxDist,
            EnemyRuntime lastHit,
            List<EnemyRuntime> candidates)
        {
            if (direction.sqrMagnitude < 1e-8f || maxDist <= 1e-8f)
                return null;

            var moveDir = direction.normalized;
            var collisionRadius = ProjectileRuntime.HitRadius + ProjectileRuntime.PierceLookAheadPad;
            var hitRadiusSq = collisionRadius * collisionRadius;
            EnemyRuntime best = null;
            var bestT = float.PositiveInfinity;

            for (var i = 0; i < candidates.Count; i++)
            {
                var enemy = candidates[i];
                if (enemy == null || !enemy.IsAlive || ReferenceEquals(enemy, lastHit))
                    continue;

                var rel = enemy.WorldPosition - from;
                var t = Vector3.Dot(rel, moveDir);
                if (t < -ProjectileRuntime.HitRadius
                    || t > maxDist + ProjectileRuntime.HitRadius)
                    continue;

                var closest = from + moveDir * Mathf.Clamp(t, 0f, maxDist);
                if ((enemy.WorldPosition - closest).sqrMagnitude > hitRadiusSq)
                    continue;

                if (t < bestT)
                {
                    bestT = t;
                    best = enemy;
                }
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
