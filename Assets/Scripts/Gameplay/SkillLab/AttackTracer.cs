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

        readonly TargetSelector _selector = new TargetSelector();
        readonly GemModifierPipeline _pipeline = new GemModifierPipeline();
        readonly List<IAttackModifier> _scratch = new List<IAttackModifier>(8);
        readonly List<SimShot> _queue = new List<SimShot>(32);

        struct SimShot
        {
            public Vector3 Position;
            public Vector3 Direction;
            public float Damage;
            public float RemainingFlight;
            public int ChainRemaining;
            public int PierceRemaining;
            public int ForkRemaining;
            public float AoeRadius;
            public float ChainRange;
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

            var spec = _pipeline.Resolve(tower, _scratch);
            var rangeMul = spec.RangeMultiplier > 0.01f ? spec.RangeMultiplier : 1f;
            var range = tower.Def.GetFireTowerRadius(tower.Level) * rangeMul;
            if (!_selector.TrySelect(tower.Targeting, origin, range, dummies, out var primary) || primary == null)
                return trace;

            trace.HasTarget = true;
            _queue.Clear();

            var pierceRemaining = spec.Pierce ? ProjectileRuntime.DefaultPierceRemaining : 0;
            var damage = spec.Damage;
            var hydra = EvolutionEvaluator.IsHydraTower(tower);
            if (hydra)
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

        void EnqueueVolley(
            Vector3 origin,
            EnemyRuntime primary,
            AttackSpec spec,
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

            var count = spec.ProjectileCount > 0 ? spec.ProjectileCount : 1;
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
                    RemainingFlight = ProjectileRuntime.MaxFlightDistance,
                    ChainRemaining = spec.ChainCount,
                    PierceRemaining = pierceRemaining,
                    ForkRemaining = spec.ForkCount,
                    AoeRadius = spec.AoeRadius,
                    ChainRange = ProjectileRuntime.DefaultChainRange,
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

                if (shot.AoeRadius > 0f)
                {
                    trace.Discs.Add(new AttackTraceDisc
                    {
                        Center = hit.WorldPosition,
                        Radius = shot.AoeRadius,
                        Kind = AttackTraceKind.Aoe
                    });
                }

                if (shot.PierceRemaining > 0)
                {
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
                    shot.Damage *= ProjectileRuntime.ChainHopFalloff;
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
                RemainingFlight = ProjectileRuntime.MaxFlightDistance,
                ChainRemaining = parent.ChainRemaining,
                PierceRemaining = parent.PierceRemaining,
                ForkRemaining = parent.ForkRemaining - 1,
                AoeRadius = parent.AoeRadius,
                ChainRange = parent.ChainRange,
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
