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
    /// without applying damage. Mode C must converge on a shared path.
    /// </summary>
    public sealed class AttackTracer
    {
        const float HitRadius = 0.15f;
        const float PierceLookAheadPad = 0.05f;

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
        readonly System.Random _previewRng = new System.Random(12345);

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
            var trace = new AttackTrace();
            if (tower == null || tower.Def == null || !tower.Def.IsFireable)
                return trace;
            if (dummies == null || dummies.Count == 0)
                return trace;

            var baseline = _pipeline.ResolveBaseline(tower);
            var spec = _pipeline.Resolve(tower, _scratch);
            var rangeMul = spec.RangeMultiplier > 0.01f ? spec.RangeMultiplier : 1f;
            var range = tower.Def.GetFireTowerRadius(tower.Level) * rangeMul;
            if (!_selector.TrySelect(tower.Targeting, origin, range, dummies, out var primary) || primary == null)
                return trace;

            trace.HasTarget = true;
            _queue.Clear();

            var pierceRemaining = spec.GetPierceRemaining();
            var damage = spec.Damage;
            if (spec.DeliveryPattern == DeliveryPattern.WarpStrike)
            {
                TraceWarpStrike(trace, origin, primary, spec, baseline, damage, dummies, tower);
            }
            else if (spec.DeliveryPattern == DeliveryPattern.GroundPulse)
            {
                TraceGroundPulse(trace, origin, primary, spec, dummies);
            }
            else if (EvolutionEvaluator.IsHydraTower(tower))
            {
                var laterals = EvolutionEvaluator.HydraHeadLateralOffsets;
                var yaws = EvolutionEvaluator.HydraHeadYawOffsets;
                for (var h = 0; h < laterals.Length; h++)
                {
                    var isCenter = Mathf.Abs(yaws[h]) < 1e-4f && Mathf.Abs(laterals[h]) < 1e-4f;
                    EnqueueVolley(
                        origin,
                        primary,
                        spec,
                        damage,
                        pierceRemaining,
                        yaws[h],
                        laterals[h],
                        isCenter ? AttackTraceKind.Primary : AttackTraceKind.HydraHead);
                }
            }
            else
            {
                EnqueueVolley(origin, primary, spec, damage, pierceRemaining, 0f, 0f, AttackTraceKind.Primary);
            }

            Simulate(trace, dummies);
            return trace;
        }

        void TraceWarpStrike(
            AttackTrace trace,
            Vector3 origin,
            EnemyRuntime primary,
            SkillSpec spec,
            SkillSpec baseline,
            float damage,
            List<EnemyRuntime> dummies,
            TowerInstance tower)
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

            if (tower == null || tower.Def == null)
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
                _previewRng,
                _payloadPlans);

            for (var i = 0; i < _payloadPlans.Count; i++)
            {
                var plan = _payloadPlans[i];
                var payloadDamage = (plan.DamageMin + plan.DamageMax) * 0.5f;
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
                        AttackTraceKind.Magma,
                        payloadDamage);
                }

                trace.Discs.Add(new AttackTraceDisc
                {
                    Center = plan.LandingPoint,
                    Radius = plan.AoeRadius,
                    Kind = AttackTraceKind.Magma
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
        }

        static void TraceGroundPulse(
            AttackTrace trace,
            Vector3 origin,
            EnemyRuntime primary,
            SkillSpec spec,
            List<EnemyRuntime> dummies)
        {
            AddHitTarget(trace, primary);
            RecordAoeTargets(trace, origin, primary, spec.AoeRadius, dummies);
        }

        void EnqueueVolley(
            Vector3 origin,
            EnemyRuntime primary,
            SkillSpec spec,
            float damage,
            int pierceRemaining,
            float headYawDegrees,
            float headLateral,
            AttackTraceKind firstKind)
        {
            var aim = primary.WorldPosition - origin;
            if (aim.sqrMagnitude < 1e-8f)
                aim = Vector3.forward;
            else
                aim.Normalize();

            if (Mathf.Abs(headLateral) > 1e-4f)
            {
                var headSide = Quaternion.Euler(0f, 90f, 0f) * aim;
                origin += headSide * headLateral;
                aim = primary.WorldPosition - origin;
                if (aim.sqrMagnitude > 1e-8f)
                    aim.Normalize();
            }

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
            EnqueueFork(parent, spawnAt, Quaternion.Euler(0f, ProjectileRuntime.ForkHalfAngleDegrees, 0f) * inbound);
            EnqueueFork(parent, spawnAt, Quaternion.Euler(0f, -ProjectileRuntime.ForkHalfAngleDegrees, 0f) * inbound);
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
                ForkRemaining = parent.ForkRemaining - 1,
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
            var hitRadiusSq = (HitRadius + PierceLookAheadPad) * (HitRadius + PierceLookAheadPad);
            EnemyRuntime best = null;
            var bestT = float.PositiveInfinity;

            for (var i = 0; i < candidates.Count; i++)
            {
                var enemy = candidates[i];
                if (enemy == null || !enemy.IsAlive || ReferenceEquals(enemy, lastHit))
                    continue;

                var rel = enemy.WorldPosition - from;
                var t = Vector3.Dot(rel, moveDir);
                if (t < 0f || t > maxDist)
                    continue;

                var closest = from + moveDir * t;
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
            EnemyRuntime exclude,
            float range,
            List<EnemyRuntime> candidates)
        {
            EnemyRuntime best = null;
            var bestSq = range * range;
            for (var i = 0; i < candidates.Count; i++)
            {
                var enemy = candidates[i];
                if (enemy == null || !enemy.IsAlive || ReferenceEquals(enemy, exclude))
                    continue;
                var sq = (enemy.WorldPosition - from).sqrMagnitude;
                if (sq <= bestSq)
                {
                    bestSq = sq;
                    best = enemy;
                }
            }

            return best;
        }
    }
}
