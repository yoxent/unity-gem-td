using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class GemStatResolverTests
    {
        [Test]
        public void ChainCount_AddTen_SetsTenJumps()
        {
            var spec = GemStatResolver.Apply(
                SkillSpec.FromBase(10f),
                new[]
                {
                    GemStatModifier.Single(GemStat.ChainCount, RoleModifierOperation.Add, 10f)
                });

            Assert.AreEqual(10, spec.ChainCount);
            Assert.AreEqual(1f, spec.ChainHopFalloff, 0.001f);
            Assert.AreEqual(10f, spec.Damage, 0.001f);
        }

        [Test]
        public void ChainCount_FalloffZero_MeansNoHopReduction()
        {
            var spec = GemStatResolver.Apply(
                SkillSpec.FromBase(10f),
                new[]
                {
                    GemStatModifier.Single(GemStat.ChainCount, RoleModifierOperation.Add, 2f, 0f)
                });

            Assert.AreEqual(2, spec.ChainCount);
            Assert.AreEqual(1f, spec.ChainHopFalloff, 0.001f);
        }

        [Test]
        public void ChainCount_FalloffTravelsWithCount()
        {
            var spec = GemStatResolver.Apply(
                SkillSpec.FromBase(10f),
                new[]
                {
                    GemStatModifier.Single(
                        GemStat.ChainCount,
                        RoleModifierOperation.Add,
                        1f,
                        ProjectileRuntime.DefaultChainHopFalloff)
                });

            Assert.AreEqual(1, spec.ChainCount);
            Assert.AreEqual(0.6f, spec.ChainHopFalloff, 0.001f);
        }

        [Test]
        public void ChainCount_TieredFalloff_UsesSelectedRarity()
        {
            var baseline = SkillSpec.FromBase(10f);
            var modifier = GemStatModifier.TieredSingle(
                GemStat.ChainCount,
                RoleModifierOperation.Add,
                lesser: 1f,
                normal: 1f,
                greater: 1f,
                lesserFalloff: ProjectileRuntime.LesserChainHopFalloff,
                normalFalloff: ProjectileRuntime.DefaultChainHopFalloff,
                greaterFalloff: ProjectileRuntime.GreaterChainHopFalloff);

            var lesser = GemStatResolver.Apply(baseline, new[] { modifier }, GemRarity.Lesser);
            var normal = GemStatResolver.Apply(baseline, new[] { modifier }, GemRarity.Normal);
            var greater = GemStatResolver.Apply(baseline, new[] { modifier }, GemRarity.Greater);

            Assert.AreEqual(ProjectileRuntime.LesserChainHopFalloff, lesser.ChainHopFalloff, 1e-4f);
            Assert.AreEqual(ProjectileRuntime.DefaultChainHopFalloff, normal.ChainHopFalloff, 1e-4f);
            Assert.AreEqual(ProjectileRuntime.GreaterChainHopFalloff, greater.ChainHopFalloff, 1e-4f);
        }

        [Test]
        public void ChainCount_LegacySingleFalloff_AppliesToEveryRarity()
        {
            var baseline = SkillSpec.FromBase(10f);
            var legacy = new GemStatModifier
            {
                Stat = GemStat.ChainCount,
                Operation = RoleModifierOperation.Add,
                ValueKind = RoleStatValueKind.Single,
                Value = 1f,
                Lesser = 1f,
                Normal = 1f,
                Greater = 1f,
                Falloff = 0.55f
            };

            var lesser = GemStatResolver.Apply(baseline, new[] { legacy }, GemRarity.Lesser);
            var greater = GemStatResolver.Apply(baseline, new[] { legacy }, GemRarity.Greater);

            Assert.AreEqual(0.55f, lesser.ChainHopFalloff, 1e-4f);
            Assert.AreEqual(0.55f, greater.ChainHopFalloff, 1e-4f);
        }

        [Test]
        public void EmptyModifiers_LeaveBaseline()
        {
            var baseline = SkillSpec.FromBase(10f);
            var spec = GemStatResolver.Apply(baseline, null);
            Assert.AreEqual(10f, spec.Damage, 0.001f);
            Assert.AreEqual(0, spec.ChainCount);

            spec = GemStatResolver.Apply(baseline, System.Array.Empty<GemStatModifier>());
            Assert.AreEqual(10f, spec.Damage, 0.001f);
        }

        [Test]
        public void AppliesSetThenAddThenMultiply()
        {
            var spec = GemStatResolver.Apply(
                SkillSpec.FromBase(10f),
                new[]
                {
                    GemStatModifier.Single(GemStat.ChainCount, RoleModifierOperation.Multiply, 2f),
                    GemStatModifier.Single(GemStat.ChainCount, RoleModifierOperation.Add, 3f),
                    GemStatModifier.Single(GemStat.ChainCount, RoleModifierOperation.Set, 4f)
                });

            Assert.AreEqual(14, spec.ChainCount);
        }

        [Test]
        public void DamageMultiply_ScalesHit()
        {
            var spec = GemStatResolver.Apply(
                SkillSpec.FromBase(10f),
                new[]
                {
                    GemStatModifier.Single(GemStat.Damage, RoleModifierOperation.Multiply, 0.7f)
                });

            Assert.AreEqual(7f, spec.Damage, 0.001f);
        }

        [Test]
        public void PierceSetZero_StaysFiniteWithNoPierce()
        {
            var spec = GemStatResolver.Apply(
                SkillSpec.FromBase(10f),
                new[]
                {
                    GemStatModifier.Single(GemStat.PierceCount, RoleModifierOperation.Set, 0f)
                });

            Assert.AreEqual(PierceMode.Finite, spec.PierceBehavior);
            Assert.AreEqual(0, spec.PierceCount);
            Assert.IsFalse(spec.Pierce);
        }

        [Test]
        public void PierceAdd_EnablesFinitePierce()
        {
            var spec = GemStatResolver.Apply(
                SkillSpec.FromBase(10f),
                new[]
                {
                    GemStatModifier.Single(GemStat.PierceCount, RoleModifierOperation.Add, 1f)
                });

            Assert.IsTrue(spec.Pierce);
            Assert.AreEqual(PierceMode.Finite, spec.PierceBehavior);
            Assert.AreEqual(1, spec.PierceCount);
        }

        [Test]
        public void IgniteSet_TogglesFlag()
        {
            var spec = GemStatResolver.Apply(
                SkillSpec.FromBase(10f),
                new[]
                {
                    GemStatModifier.Single(GemStat.Ignite, RoleModifierOperation.Set, 1f)
                });

            Assert.IsTrue(spec.Ignite);
        }

        [Test]
        public void BleedChanceSet_StoresFraction()
        {
            var spec = GemStatResolver.Apply(
                SkillSpec.FromBase(10f),
                new[]
                {
                    GemStatModifier.Single(GemStat.BleedChance, RoleModifierOperation.Set, 0.25f)
                });

            Assert.AreEqual(0.25f, spec.BleedChance, 0.001f);
            Assert.AreEqual(1f, spec.BleedDamageMultiplier, 0.001f);
        }

        [Test]
        public void BleedDamageMultiply_ScalesBleedOnly()
        {
            var spec = GemStatResolver.Apply(
                SkillSpec.FromBase(10f),
                new[]
                {
                    GemStatModifier.Single(GemStat.BleedDamage, RoleModifierOperation.Multiply, 1.19f)
                });

            Assert.AreEqual(10f, spec.Damage, 0.001f);
            Assert.AreEqual(1.19f, spec.BleedDamageMultiplier, 0.001f);
        }

        [Test]
        public void BurningDamageMultiply_ScalesIgniteDotOnly()
        {
            var spec = GemStatResolver.Apply(
                SkillSpec.FromBase(10f),
                new[]
                {
                    GemStatModifier.Single(GemStat.BurningDamage, RoleModifierOperation.Multiply, 1.2f)
                });

            Assert.AreEqual(10f, spec.Damage, 0.001f);
            Assert.AreEqual(1.2f, spec.BurningDamageMultiplier, 0.001f);
        }

        [Test]
        public void PhysAsExtraFire_SetThenMagnitudeMultiply()
        {
            var spec = GemStatResolver.Apply(
                SkillSpec.FromBase(10f),
                new[]
                {
                    GemStatModifier.Single(GemStat.PhysAsExtraFire, RoleModifierOperation.Set, 0.25f),
                    GemStatModifier.Single(GemStat.PhysAsExtraFire, RoleModifierOperation.Multiply, 1.16f)
                });

            Assert.AreEqual(10f, spec.Damage, 0.001f);
            Assert.AreEqual(0.29f, spec.PhysAsExtraFire, 0.001f);
        }

        [Test]
        public void Conversion_StoresFraction()
        {
            var spec = GemStatResolver.Apply(
                SkillSpec.FromBase(10f),
                new[]
                {
                    GemStatModifier.Single(GemStat.ConvertFireToCold, RoleModifierOperation.Set, 0.5f)
                });

            Assert.AreEqual(0.5f, spec.ConvertFireToCold, 0.001f);
            Assert.AreEqual(10f, spec.Damage, 0.001f);
        }

        [Test]
        public void AilmentAuthoredStats_RoundTrip()
        {
            var spec = GemStatResolver.Apply(
                SkillSpec.FromBase(10f),
                new[]
                {
                    GemStatModifier.Single(GemStat.IgniteChance, RoleModifierOperation.Set, 0.4f),
                    GemStatModifier.Single(GemStat.IgniteDuration, RoleModifierOperation.Set, 4f),
                    GemStatModifier.Single(GemStat.ChillEffect, RoleModifierOperation.Multiply, 1.2f),
                    GemStatModifier.Single(GemStat.ChillDuration, RoleModifierOperation.Set, 3f),
                    GemStatModifier.Single(GemStat.ShockChance, RoleModifierOperation.Set, 0.5f),
                    GemStatModifier.Single(GemStat.ShockEffect, RoleModifierOperation.Multiply, 1.1f),
                    GemStatModifier.Single(GemStat.ShockDuration, RoleModifierOperation.Set, 5f),
                    GemStatModifier.Single(GemStat.BleedDuration, RoleModifierOperation.Set, 6f),
                    GemStatModifier.Single(GemStat.FreezeChance, RoleModifierOperation.Set, 0.2f),
                    GemStatModifier.Single(GemStat.FreezeDuration, RoleModifierOperation.Set, 1.5f),
                    GemStatModifier.Single(GemStat.PoisonChance, RoleModifierOperation.Set, 0.3f),
                    GemStatModifier.Single(GemStat.PoisonDuration, RoleModifierOperation.Set, 4f),
                    GemStatModifier.Single(GemStat.StunChance, RoleModifierOperation.Set, 0.15f),
                    GemStatModifier.Single(GemStat.StunDuration, RoleModifierOperation.Set, 1f)
                });

            Assert.AreEqual(0.4f, spec.IgniteChance, 0.001f);
            Assert.AreEqual(4f, spec.IgniteDuration, 0.001f);
            Assert.AreEqual(1.2f, spec.ChillEffect, 0.001f);
            Assert.AreEqual(3f, spec.ChillDuration, 0.001f);
            Assert.AreEqual(0.5f, spec.ShockChance, 0.001f);
            Assert.AreEqual(1.1f, spec.ShockEffect, 0.001f);
            Assert.AreEqual(5f, spec.ShockDuration, 0.001f);
            Assert.AreEqual(6f, spec.BleedDuration, 0.001f);
            Assert.AreEqual(0.2f, spec.FreezeChance, 0.001f);
            Assert.AreEqual(1.5f, spec.FreezeDuration, 0.001f);
            Assert.AreEqual(0.3f, spec.PoisonChance, 0.001f);
            Assert.AreEqual(4f, spec.PoisonDuration, 0.001f);
            Assert.AreEqual(0.15f, spec.StunChance, 0.001f);
            Assert.AreEqual(1f, spec.StunDuration, 0.001f);
            Assert.IsFalse(spec.Shock);
        }

        [Test]
        public void AilmentDamageMultiply_StoresMultiplier()
        {
            var spec = GemStatResolver.Apply(
                SkillSpec.FromBase(10f),
                new[]
                {
                    GemStatModifier.Single(GemStat.AilmentDamage, RoleModifierOperation.Multiply, 1.36f)
                });

            Assert.AreEqual(10f, spec.Damage, 0.001f);
            Assert.AreEqual(1.36f, spec.AilmentDamageMultiplier, 0.001f);
        }

        [Test]
        public void TieredDamageModifier_UsesSelectedRarity()
        {
            var baseline = SkillSpec.FromBase(10f, 10f, projectiles: 1, aoe: 0f);
            var modifier = GemStatModifier.TieredSingle(
                GemStat.Damage,
                RoleModifierOperation.Multiply,
                lesser: 0.8f,
                normal: 1f,
                greater: 1.3f);

            var lesser = GemStatResolver.Apply(baseline, new[] { modifier }, GemRarity.Lesser);
            var greater = GemStatResolver.Apply(baseline, new[] { modifier }, GemRarity.Greater);

            Assert.AreEqual(8f, lesser.Damage, 1e-4f);
            Assert.AreEqual(13f, greater.Damage, 1e-4f);
        }

        [Test]
        public void TieredModifiers_PreserveSetAddMultiplyOrderForEveryRarity()
        {
            var baseline = SkillSpec.FromBase(1f);
            var modifiers = new[]
            {
                GemStatModifier.TieredSingle(
                    GemStat.Damage,
                    RoleModifierOperation.Multiply,
                    lesser: 2f,
                    normal: 3f,
                    greater: 4f),
                GemStatModifier.TieredSingle(
                    GemStat.Damage,
                    RoleModifierOperation.Add,
                    lesser: 1f,
                    normal: 2f,
                    greater: 3f),
                GemStatModifier.TieredSingle(
                    GemStat.Damage,
                    RoleModifierOperation.Set,
                    lesser: 10f,
                    normal: 20f,
                    greater: 30f)
            };

            var lesser = GemStatResolver.Apply(baseline, modifiers, GemRarity.Lesser);
            var normal = GemStatResolver.Apply(baseline, modifiers, GemRarity.Normal);
            var greater = GemStatResolver.Apply(baseline, modifiers, GemRarity.Greater);

            Assert.AreEqual(22f, lesser.Damage, 1e-4f);
            Assert.AreEqual(66f, normal.Damage, 1e-4f);
            Assert.AreEqual(132f, greater.Damage, 1e-4f);
        }

        [Test]
        public void LegacyModifierWithoutTierValues_UsesValueForEveryRarity()
        {
            var baseline = SkillSpec.FromBase(10f, 10f, projectiles: 1, aoe: 0f);
            var legacy = GemStatModifier.Single(
                GemStat.Damage,
                RoleModifierOperation.Multiply,
                0.7f);
            legacy.Lesser = 0f;
            legacy.Normal = 0f;
            legacy.Greater = 0f;

            var lesser = GemStatResolver.Apply(baseline, new[] { legacy }, GemRarity.Lesser);
            var normal = GemStatResolver.Apply(baseline, new[] { legacy }, GemRarity.Normal);
            var greater = GemStatResolver.Apply(baseline, new[] { legacy }, GemRarity.Greater);

            Assert.AreEqual(7f, lesser.Damage, 1e-4f);
            Assert.AreEqual(7f, normal.Damage, 1e-4f);
            Assert.AreEqual(7f, greater.Damage, 1e-4f);
        }
    }
}
