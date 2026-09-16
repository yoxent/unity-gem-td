using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class EffectPayloadResolverTests
    {
        static EffectPayloadDefinition MoltenDefinition => new EffectPayloadDefinition
        {
            Trigger = EffectPayloadTrigger.OnImpact,
            Anchor = EffectPayloadAnchor.PrimaryTarget,
            TravelPattern = EffectPayloadTravelPattern.Fountain,
            ScatterPattern = EffectPayloadScatterPattern.RandomRing,
            HitPolicy = EffectPayloadHitPolicy.PerImpact,
            Tags = GemTag.Aoe | GemTag.Projectile,
            Count = 4,
            DamageMultiplier = 0.4f,
            AoeRadius = 1f,
            MinDistance = 1f,
            MaxDistance = 4f,
            ArcHeight = 1.5f
        };

        static EffectPayloadDefinition StraightProjectileDefinition => new EffectPayloadDefinition
        {
            Trigger = EffectPayloadTrigger.OnImpact,
            Anchor = EffectPayloadAnchor.ImpactPoint,
            TravelPattern = EffectPayloadTravelPattern.Straight,
            ScatterPattern = EffectPayloadScatterPattern.FixedRadial,
            HitPolicy = EffectPayloadHitPolicy.OncePerPayload,
            Tags = GemTag.Projectile,
            Count = 3,
            DamageMultiplier = 0.5f,
            AoeRadius = 0.5f,
            MinDistance = 2f,
            MaxDistance = 2f
        };

        static SkillSpec BaseSpec =>
            SkillSpec.FromBase(10f, 10f, projectiles: 1, aoe: 0f);

        static EffectPayloadDefinition FirestormDefinition => new EffectPayloadDefinition
        {
            Trigger = EffectPayloadTrigger.AfterDelay,
            Anchor = EffectPayloadAnchor.GroundTarget,
            TravelPattern = EffectPayloadTravelPattern.FallFromSky,
            ScatterPattern = EffectPayloadScatterPattern.None,
            HitPolicy = EffectPayloadHitPolicy.PerImpact,
            Tags = GemTag.Aoe,
            Count = 10,
            DamageMultiplier = 1f,
            AoeRadius = 1.3f,
            MinDistance = 0f,
            MaxDistance = 2.5f,
            ArcHeight = 3f,
            IntervalSeconds = 0.15f
        };

        [Test]
        public void BuildOnImpact_SeededPlan_IsReproducible()
        {
            var anchor = new Vector3(4f, 0f, 0f);
            var a = Build(MoltenDefinition, BaseSpec, BaseSpec, anchor, seed: 12345);
            var b = Build(MoltenDefinition, BaseSpec, BaseSpec, anchor, seed: 12345);

            Assert.AreEqual(4, a.Count);
            Assert.AreEqual(4, b.Count);
            for (var i = 0; i < 4; i++)
            {
                Assert.AreEqual(a[i].LandingPoint.x, b[i].LandingPoint.x, 1e-4f);
                Assert.AreEqual(a[i].LandingPoint.z, b[i].LandingPoint.z, 1e-4f);
            }
        }

        [Test]
        public void BuildOnImpact_RandomRing_EndpointsWithinDistanceBounds()
        {
            var plans = Build(MoltenDefinition, BaseSpec, BaseSpec, new Vector3(0f, 0f, 0f), seed: 99);
            Assert.AreEqual(4, plans.Count);

            for (var i = 0; i < plans.Count; i++)
            {
                var dist = plans[i].HorizontalDistance;
                Assert.GreaterOrEqual(dist, 1f - 1e-3f);
                Assert.LessOrEqual(dist, 4f + 1e-3f);
            }
        }

        [Test]
        public void BuildOnImpact_RandomRing_UsesFullRingDirections()
        {
            var def = MoltenDefinition;
            def.Count = 24;
            var plans = Build(def, BaseSpec, BaseSpec, Vector3.zero, seed: 777);
            var quadrants = new bool[4];
            for (var i = 0; i < plans.Count; i++)
            {
                var delta = plans[i].LandingPoint;
                if (delta.x >= 0f && delta.z >= 0f) quadrants[0] = true;
                if (delta.x < 0f && delta.z >= 0f) quadrants[1] = true;
                if (delta.x < 0f && delta.z < 0f) quadrants[2] = true;
                if (delta.x >= 0f && delta.z < 0f) quadrants[3] = true;
            }

            for (var q = 0; q < 4; q++)
                Assert.IsTrue(quadrants[q], $"Expected scatter in quadrant {q}");
        }

        [Test]
        public void BuildOnImpact_CopiesDamageAndAoeFromSpec()
        {
            var spec = SkillSpec.FromBase(8f, 12f, projectiles: 1, aoe: 0f);
            var baseline = SkillSpec.FromBase(8f, 12f, projectiles: 1, aoe: 0f);
            var plans = new List<EffectPayloadPlan>(4);
            EffectPayloadResolver.BuildOnImpact(
                new[] { MoltenDefinition },
                spec,
                baseline,
                Vector3.zero,
                new System.Random(1),
                plans);

            Assert.AreEqual(3.2f, plans[0].DamageMin, 1e-4f);
            Assert.AreEqual(4.8f, plans[0].DamageMax, 1e-4f);
            Assert.AreEqual(1f, plans[0].AoeRadius, 1e-4f);
            Assert.AreEqual(EffectPayloadTravelPattern.Fountain, plans[0].TravelPattern);
            Assert.AreEqual(EffectPayloadHitPolicy.PerImpact, plans[0].HitPolicy);
        }

        [Test]
        public void BuildOnImpact_AoeTaggedPayload_ScalesRadiusWithIncreasedArea()
        {
            var baseline = SkillSpec.FromBase(10f, 10f, projectiles: 1, aoe: 0f);
            var spec = baseline;
            spec = GemStatResolver.Apply(spec, CatalogGemModifiers.For(GemId.IncreasedArea));

            var plans = Build(MoltenDefinition, spec, baseline, Vector3.zero, seed: 1);
            Assert.AreEqual(4, plans.Count);
            Assert.AreEqual(1.35f, plans[0].AoeRadius, 1e-3f);
        }

        [Test]
        public void BuildOnImpact_FountainPayloadWithoutProjectileTag_IgnoresMultipleProjectiles()
        {
            var def = MoltenDefinition;
            def.Tags = GemTag.Aoe;

            var baseline = SkillSpec.FromBase(10f, 10f, projectiles: 1, aoe: 0f);
            var spec = baseline;
            spec = GemStatResolver.Apply(spec, CatalogGemModifiers.For(GemId.MultipleProjectiles));

            var plans = Build(def, spec, baseline, Vector3.zero, seed: 1);
            Assert.AreEqual(4, plans.Count);
        }

        [Test]
        public void BuildOnImpact_FountainPayloadWithProjectileTag_AddsMultipleProjectiles()
        {
            var def = MoltenDefinition;
            def.Tags = GemTag.Aoe | GemTag.Projectile;

            var baseline = SkillSpec.FromBase(10f, 10f, projectiles: 1, aoe: 0f);
            var spec = baseline;
            spec = GemStatResolver.Apply(spec, CatalogGemModifiers.For(GemId.MultipleProjectiles));

            var plans = Build(def, spec, baseline, Vector3.zero, seed: 1);
            Assert.AreEqual(6, plans.Count);
        }

        [Test]
        public void BuildOnImpact_StraightProjectilePayload_AddsMultipleProjectiles()
        {
            var baseline = SkillSpec.FromBase(10f, 10f, projectiles: 1, aoe: 0f);
            var spec = baseline;
            spec = GemStatResolver.Apply(spec, CatalogGemModifiers.For(GemId.MultipleProjectiles));

            var plans = Build(StraightProjectileDefinition, spec, baseline, Vector3.zero, seed: 1);
            Assert.AreEqual(5, plans.Count);
        }

        [Test]
        public void BuildOnImpact_StraightPayloadWithoutAoeTag_IgnoresIncreasedArea()
        {
            var def = StraightProjectileDefinition;
            def.Tags = GemTag.Projectile;

            var baseline = SkillSpec.FromBase(10f, 10f, projectiles: 1, aoe: 0f);
            var spec = baseline;
            spec = GemStatResolver.Apply(spec, CatalogGemModifiers.For(GemId.IncreasedArea));

            var plans = Build(def, spec, baseline, Vector3.zero, seed: 1);
            Assert.AreEqual(3, plans.Count);
            Assert.AreEqual(0.5f, plans[0].AoeRadius, 1e-3f);
        }

        [Test]
        public void BuildOnImpact_InvalidOrEmptyDefinitions_ProducesNoPlans()
        {
            var plans = new List<EffectPayloadPlan>(4);
            EffectPayloadResolver.BuildOnImpact(
                Array.Empty<EffectPayloadDefinition>(),
                BaseSpec,
                BaseSpec,
                Vector3.zero,
                new System.Random(1),
                plans);
            Assert.AreEqual(0, plans.Count);

            var invalid = new EffectPayloadDefinition
            {
                Count = 0,
                DamageMultiplier = 0.4f,
                AoeRadius = 1f
            };
            EffectPayloadResolver.BuildOnImpact(
                new[] { invalid },
                BaseSpec,
                BaseSpec,
                Vector3.zero,
                new System.Random(1),
                plans);
            Assert.AreEqual(0, plans.Count);
        }

        [Test]
        public void BuildFallingRain_FirstBoltOnAim_OthersInDisk_StaggeredDelays()
        {
            var aim = new Vector3(4f, 0f, 1f);
            var into = new List<EffectPayloadPlan>(10);
            EffectPayloadResolver.BuildFallingRain(
                new[] { FirestormDefinition },
                BaseSpec,
                BaseSpec,
                aim,
                new System.Random(12345),
                into);
            Assert.AreEqual(10, into.Count);
            Assert.AreEqual(aim.x, into[0].LandingPoint.x, 1e-4f);
            Assert.AreEqual(aim.z, into[0].LandingPoint.z, 1e-4f);
            Assert.AreEqual(0f, into[0].DelaySeconds, 1e-4f);
            for (var i = 0; i < into.Count; i++)
            {
                Assert.AreEqual(i * 0.15f, into[i].DelaySeconds, 1e-4f);
                Assert.AreEqual(EffectPayloadTravelPattern.FallFromSky, into[i].TravelPattern);
                Assert.AreEqual(into[i].LandingPoint.x, into[i].Origin.x, 1e-4f);
                Assert.AreEqual(into[i].LandingPoint.z, into[i].Origin.z, 1e-4f);
                Assert.Greater(into[i].Origin.y, into[i].LandingPoint.y);
                var dx = into[i].LandingPoint.x - aim.x;
                var dz = into[i].LandingPoint.z - aim.z;
                Assert.LessOrEqual(dx * dx + dz * dz, 2.5f * 2.5f + 1e-3f);
            }
        }

        static List<EffectPayloadPlan> Build(
            EffectPayloadDefinition def,
            SkillSpec spec,
            SkillSpec baseline,
            Vector3 anchor,
            int seed)
        {
            var plans = new List<EffectPayloadPlan>(8);
            EffectPayloadResolver.BuildOnImpact(
                new[] { def },
                spec,
                baseline,
                anchor,
                new System.Random(seed),
                plans);
            return plans;
        }
    }
}
