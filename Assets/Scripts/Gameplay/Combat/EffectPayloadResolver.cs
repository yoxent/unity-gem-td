using System;
using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// Builds deterministic payload plans from authored definitions and a resolved skill spec.
    /// Uses an injected random source — never UnityEngine.Random.
    /// </summary>
    public static class EffectPayloadResolver
    {
        public static void BuildOnImpact(
            IReadOnlyList<EffectPayloadDefinition> definitions,
            in SkillSpec spec,
            in SkillSpec baseline,
            Vector3 anchorPosition,
            System.Random rng,
            List<EffectPayloadPlan> into)
        {
            if (definitions == null || definitions.Count == 0 || into == null)
                return;

            for (var d = 0; d < definitions.Count; d++)
            {
                var def = definitions[d];
                if (def == null || def.Trigger != EffectPayloadTrigger.OnImpact || !def.IsValid)
                    continue;

                AppendPlans(def, spec, baseline, anchorPosition, rng, into);
            }
        }

        static void AppendPlans(
            EffectPayloadDefinition def,
            in SkillSpec spec,
            in SkillSpec baseline,
            Vector3 anchorPosition,
            System.Random rng,
            List<EffectPayloadPlan> into)
        {
            ResolvePayloadStats(def, spec, baseline, out var count, out var aoeRadius);
            if (count <= 0 || aoeRadius <= 0f)
                return;

            var origin = ResolveAnchor(def.Anchor, anchorPosition);
            var damageMin = spec.DamageMin * def.DamageMultiplier;
            var damageMax = spec.DamageMax * def.DamageMultiplier;
            var ailments = AilmentTune.FromSkillSpec(spec);

            switch (def.ScatterPattern)
            {
                case EffectPayloadScatterPattern.FixedRadial:
                    AppendFixedRadial(
                        def,
                        spec,
                        origin,
                        count,
                        aoeRadius,
                        damageMin,
                        damageMax,
                        ailments,
                        into);
                    break;

                case EffectPayloadScatterPattern.RandomRing:
                    AppendRandomRing(
                        def,
                        spec,
                        origin,
                        count,
                        aoeRadius,
                        damageMin,
                        damageMax,
                        ailments,
                        rng,
                        into);
                    break;

                default:
                    AppendSingle(def, spec, origin, origin, aoeRadius, damageMin, damageMax, ailments, into);
                    break;
            }
        }

        internal static void ResolvePayloadStats(
            EffectPayloadDefinition def,
            in SkillSpec spec,
            in SkillSpec baseline,
            out int count,
            out float aoeRadius)
        {
            count = def.Count;
            aoeRadius = def.AoeRadius;

            if ((def.Tags & GemTag.Aoe) != 0 && def.AoeRadius > 0f)
                aoeRadius = def.AoeRadius * spec.AoeRadiusMultiplier;

            if ((def.Tags & GemTag.Projectile) != 0)
            {
                var extra = spec.ProjectileCount - baseline.ProjectileCount;
                if (extra > 0)
                    count += extra;
            }
        }

        static void AppendFixedRadial(
            EffectPayloadDefinition def,
            in SkillSpec spec,
            Vector3 origin,
            int count,
            float aoeRadius,
            float damageMin,
            float damageMax,
            AilmentTune ailments,
            List<EffectPayloadPlan> into)
        {
            if (count <= 0)
                return;

            var step = 360f / count;
            for (var i = 0; i < count; i++)
            {
                var yaw = i * step;
                var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                var dist = (def.MinDistance + def.MaxDistance) * 0.5f;
                var landing = origin + dir * dist;
                landing.y = origin.y;
                AppendSingle(def, spec, origin, landing, aoeRadius, damageMin, damageMax, ailments, into);
            }
        }

        static void AppendRandomRing(
            EffectPayloadDefinition def,
            in SkillSpec spec,
            Vector3 origin,
            int count,
            float aoeRadius,
            float damageMin,
            float damageMax,
            AilmentTune ailments,
            System.Random rng,
            List<EffectPayloadPlan> into)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            var minDist = def.MinDistance;
            var maxDist = def.MaxDistance;
            for (var i = 0; i < count; i++)
            {
                var angle = (float)(rng.NextDouble() * 360.0);
                var t = (float)rng.NextDouble();
                var dist = minDist + (maxDist - minDist) * t;
                var dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var landing = origin + dir * dist;
                landing.y = origin.y;
                AppendSingle(def, spec, origin, landing, aoeRadius, damageMin, damageMax, ailments, into);
            }
        }

        static void AppendSingle(
            EffectPayloadDefinition def,
            in SkillSpec spec,
            Vector3 origin,
            Vector3 landing,
            float aoeRadius,
            float damageMin,
            float damageMax,
            AilmentTune ailments,
            List<EffectPayloadPlan> into)
        {
            into.Add(new EffectPayloadPlan
            {
                Trigger = def.Trigger,
                TravelPattern = def.TravelPattern,
                HitPolicy = def.HitPolicy,
                Origin = origin,
                LandingPoint = landing,
                DamageMin = damageMin,
                DamageMax = damageMax,
                AoeRadius = aoeRadius,
                ArcHeight = def.ArcHeight > 0f ? def.ArcHeight : 1.5f,
                DelaySeconds = def.DelaySeconds,
                IntervalSeconds = def.IntervalSeconds,
                RepeatCount = def.RepeatCount,
                Ailments = ailments,
                Proliferate = spec.Proliferate,
                KnockbackChance = spec.KnockbackChance,
                KnockbackDistance = spec.KnockbackDistance
            });
        }

        static Vector3 ResolveAnchor(EffectPayloadAnchor anchor, Vector3 anchorPosition)
        {
            switch (anchor)
            {
                case EffectPayloadAnchor.Caster:
                case EffectPayloadAnchor.GroundTarget:
                case EffectPayloadAnchor.ImpactPoint:
                case EffectPayloadAnchor.PrimaryTarget:
                default:
                    return anchorPosition;
            }
        }
    }
}
