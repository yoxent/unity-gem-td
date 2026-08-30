using NUnit.Framework;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class SupportGemMapTests
    {
        const string ChanceToBleedJson =
            "{\"name\":\"Chance to Bleed Support\",\"tags\":[\"Attack\",\"Physical\",\"Support\"],\"upside\":\"Increases the tower's offensive output in a specialized way.\",\"downside\":\"Reduces another aspect of offensive performance, consistency, or flexibility.\",\"explicitMods\":[{\"text\":\"Supported Attacks deal #% more Damage with Bleeding\",\"values\":{\"lesser\":10,\"normal\":19,\"greater\":29}},{\"text\":\"Supported Attacks have #% chance to cause Bleeding\",\"values\":{\"lesser\":25,\"normal\":25,\"greater\":25}}]}";

        const string AncestralCallJson =
            "{\"name\":\"Ancestral Call Support\",\"tags\":[\"Support\",\"Melee\",\"Attack\",\"Strike\"],\"upside\":\"Changes projectile behavior or improves target coverage.\",\"downside\":\"Reduces damage, consistency, or targeting flexibility.\",\"explicitMods\":[{\"text\":\"Supported Skills deal #% less Damage\",\"values\":{\"lesser\":19,\"normal\":10,\"greater\":0}},{\"text\":\"Supported Strike Skills target # additional nearby Enemies\",\"values\":null}]}";

        [Test]
        public void FullValuesObject_FillsLesserNormalAndGreater()
        {
            var result = SupportGemMap.FromGemJson(
                "{\"name\":\"Test Support\",\"tags\":[\"Support\"],\"upside\":\"a\",\"downside\":\"b\",\"explicitMods\":[{\"text\":\"Supported Skills deal #% more Damage\",\"values\":{\"lesser\":10,\"normal\":20,\"greater\":30}}]}");

            Assert.IsTrue(result.CanIngest);
            Assert.AreEqual(1, result.Modifiers.Length);
            Assert.AreEqual(1.1f, result.Modifiers[0].Lesser, 1e-4f);
            Assert.AreEqual(1.2f, result.Modifiers[0].Normal, 1e-4f);
            Assert.AreEqual(1.3f, result.Modifiers[0].Greater, 1e-4f);
        }

        [Test]
        public void ChainSupport_AddsWikiChainCountWhenJsonCountIsNull()
        {
            var result = SupportGemMap.FromGemJson(
                "{\"name\":\"Chain Support\",\"tags\":[\"Support\",\"Chaining\",\"Projectile\"],\"upside\":\"a\",\"downside\":\"b\",\"explicitMods\":[{\"text\":\"Supported Skills deal #% less Damage with Hits\",\"values\":{\"lesser\":30,\"normal\":21,\"greater\":11}},{\"text\":\"Supported Skills Chain # times\",\"values\":null}]}");

            Assert.IsTrue(result.CanIngest);
            Assert.AreEqual(2, result.Modifiers.Length);
            Assert.AreEqual(GemStat.Damage, result.Modifiers[0].Stat);
            Assert.AreEqual(RoleModifierOperation.Multiply, result.Modifiers[0].Operation);
            Assert.AreEqual(0.79f, result.Modifiers[0].Normal, 1e-4f);
            Assert.AreEqual(GemStat.ChainCount, result.Modifiers[1].Stat);
            Assert.AreEqual(RoleModifierOperation.Add, result.Modifiers[1].Operation);
            Assert.AreEqual(1f, result.Modifiers[1].Value, 1e-4f);
            Assert.AreEqual(ProjectileRuntime.LesserChainHopFalloff, result.Modifiers[1].LesserFalloff, 1e-4f);
            Assert.AreEqual(ProjectileRuntime.DefaultChainHopFalloff, result.Modifiers[1].NormalFalloff, 1e-4f);
            Assert.AreEqual(ProjectileRuntime.GreaterChainHopFalloff, result.Modifiers[1].GreaterFalloff, 1e-4f);
            Assert.AreEqual(ProjectileRuntime.DefaultChainHopFalloff, result.Modifiers[1].Falloff, 1e-4f);
            Assert.AreEqual(0, result.FlavorTexts.Length);
        }

        [Test]
        public void NullValuesRow_DoesNotCreateModifier()
        {
            var result = SupportGemMap.FromGemJson(
                "{\"name\":\"Test Support\",\"tags\":[\"Support\"],\"upside\":\"a\",\"downside\":\"b\",\"explicitMods\":[{\"text\":\"Reminder only\",\"values\":null}]}");

            Assert.IsTrue(result.CanIngest);
            Assert.AreEqual(0, result.Modifiers.Length);
        }

        [Test]
        public void ChanceToBleed_MapsBleedDamageAndChance()
        {
            var result = SupportGemMap.FromGemJson(ChanceToBleedJson);
            Assert.AreEqual("Chance to Bleed", result.DisplayName);
            Assert.AreEqual("chance_to_bleed", result.Slug);
            Assert.AreEqual(GemTag.Attack | GemTag.Physical | GemTag.Support, result.Tags);
            Assert.IsTrue(result.CanIngest);
            Assert.AreEqual(0, result.Unmapped.Length);
            Assert.AreEqual(0, result.FlavorTexts.Length);
            Assert.AreEqual(2, result.Modifiers.Length);
            Assert.AreEqual(GemStat.AilmentDamage, result.Modifiers[0].Stat);
            Assert.AreEqual(RoleModifierOperation.Multiply, result.Modifiers[0].Operation);
            Assert.AreEqual(1.19f, result.Modifiers[0].Value, 0.001f);
            Assert.AreEqual(1.1f, result.Modifiers[0].Lesser, 0.001f);
            Assert.AreEqual(1.19f, result.Modifiers[0].Normal, 0.001f);
            Assert.AreEqual(1.29f, result.Modifiers[0].Greater, 0.001f);
            Assert.AreEqual(GemStat.BleedChance, result.Modifiers[1].Stat);
            Assert.AreEqual(RoleModifierOperation.Set, result.Modifiers[1].Operation);
            Assert.AreEqual(0.25f, result.Modifiers[1].Value, 0.001f);
            Assert.AreEqual(0.25f, result.Modifiers[1].Lesser, 0.001f);
            Assert.AreEqual(0.25f, result.Modifiers[1].Normal, 0.001f);
            Assert.AreEqual(0.25f, result.Modifiers[1].Greater, 0.001f);
        }

        [Test]
        public void AncestralCall_Skipped_ExtraStrikeTargets()
        {
            var result = SupportGemMap.FromGemJson(AncestralCallJson);
            Assert.IsFalse(result.CanIngest);
            Assert.IsNotEmpty(result.SkipReason);
            Assert.AreEqual(0, result.Modifiers.Length);
        }

        [Test]
        public void IgniteChance_MapsFraction()
        {
            var result = SupportGemMap.FromGemJson(
                "{\"name\":\"Ignite Support\",\"tags\":[\"Fire\",\"Support\"],\"upside\":\"a\",\"downside\":\"b\",\"explicitMods\":[{\"text\":\"Supported Skills have #% chance to Ignite\",\"values\":{\"normal\":40}}]}");
            Assert.IsTrue(result.CanIngest);
            Assert.AreEqual(1, result.Modifiers.Length);
            Assert.AreEqual(GemStat.IgniteChance, result.Modifiers[0].Stat);
            Assert.AreEqual(RoleModifierOperation.Set, result.Modifiers[0].Operation);
            Assert.AreEqual(0.4f, result.Modifiers[0].Value, 0.001f);
        }

        [Test]
        public void ChillEffect_MapsIncreasedAsMultiply()
        {
            var result = SupportGemMap.FromGemJson(
                "{\"name\":\"Bonechill Support\",\"tags\":[\"Cold\",\"Support\"],\"upside\":\"a\",\"downside\":\"b\",\"explicitMods\":[{\"text\":\"#% increased Effect of Chill inflicted with Supported Skills\",\"values\":{\"normal\":20}}]}");
            Assert.IsTrue(result.CanIngest);
            Assert.AreEqual(GemStat.ChillEffect, result.Modifiers[0].Stat);
            Assert.AreEqual(RoleModifierOperation.Multiply, result.Modifiers[0].Operation);
            Assert.AreEqual(1.2f, result.Modifiers[0].Value, 0.001f);
        }

        [Test]
        public void Empower_OverridesPlusLevel_WithNormalRarityDamage()
        {
            var result = SupportGemMap.FromGemJson(
                "{\"name\":\"Empower Support\",\"tags\":[\"Exceptional\",\"Support\"],\"upside\":\"a\",\"downside\":\"b\",\"explicitMods\":[{\"text\":\"# to Level of Supported Skill Gems\",\"values\":{\"lesser\":1,\"normal\":2,\"greater\":3}}]}");
            Assert.IsTrue(result.CanIngest);
            Assert.AreEqual(0, result.Unmapped.Length);
            Assert.AreEqual(1, result.Modifiers.Length);
            Assert.AreEqual(GemStat.Damage, result.Modifiers[0].Stat);
            Assert.AreEqual(RoleModifierOperation.Multiply, result.Modifiers[0].Operation);
            Assert.AreEqual(1.4f, result.Modifiers[0].Value, 0.001f);
            Assert.AreEqual(1.4f, result.Modifiers[0].Resolve(GemRarity.Lesser).Value, 0.001f);
            Assert.AreEqual(1.4f, result.Modifiers[0].Resolve(GemRarity.Normal).Value, 0.001f);
            Assert.AreEqual(1.4f, result.Modifiers[0].Resolve(GemRarity.Greater).Value, 0.001f);
        }

        [Test]
        public void Enhance_OverridesQuality_WithAttackAndCastSpeed()
        {
            var result = SupportGemMap.FromGemJson(
                "{\"name\":\"Enhance Support\",\"tags\":[\"Exceptional\",\"Support\"],\"upside\":\"a\",\"downside\":\"b\",\"explicitMods\":[{\"text\":\"#% to Quality of Supported Skill Gems\",\"values\":{\"normal\":16}}]}");
            Assert.IsTrue(result.CanIngest);
            Assert.AreEqual(2, result.Modifiers.Length);
            Assert.AreEqual(GemStat.AttackSpeedMultiplier, result.Modifiers[0].Stat);
            Assert.AreEqual(1.4f, result.Modifiers[0].Value, 0.001f);
            Assert.AreEqual(1.4f, result.Modifiers[0].Resolve(GemRarity.Lesser).Value, 0.001f);
            Assert.AreEqual(1.4f, result.Modifiers[0].Resolve(GemRarity.Greater).Value, 0.001f);
            Assert.AreEqual(GemStat.CastSpeedMultiplier, result.Modifiers[1].Stat);
            Assert.AreEqual(1.4f, result.Modifiers[1].Value, 0.001f);
            Assert.AreEqual(1.4f, result.Modifiers[1].Resolve(GemRarity.Lesser).Value, 0.001f);
            Assert.AreEqual(1.4f, result.Modifiers[1].Resolve(GemRarity.Greater).Value, 0.001f);
        }

        [Test]
        public void Enlighten_OverridesCost_WithRange()
        {
            var result = SupportGemMap.FromGemJson(
                "{\"name\":\"Enlighten Support\",\"tags\":[\"Exceptional\",\"Support\"],\"upside\":\"a\",\"downside\":\"b\",\"explicitMods\":[{\"text\":\"Cost & Reservation Multiplier %\",\"values\":{\"normal\":92}}]}");
            Assert.IsTrue(result.CanIngest);
            Assert.AreEqual(GemStat.RangeMultiplier, result.Modifiers[0].Stat);
            Assert.AreEqual(1.4f, result.Modifiers[0].Value, 0.001f);
            Assert.AreEqual(1.4f, result.Modifiers[0].Resolve(GemRarity.Lesser).Value, 0.001f);
            Assert.AreEqual(1.4f, result.Modifiers[0].Resolve(GemRarity.Normal).Value, 0.001f);
            Assert.AreEqual(1.4f, result.Modifiers[0].Resolve(GemRarity.Greater).Value, 0.001f);
        }

        [Test]
        public void BurningDamage_MapsMoreDamageWithIgnite()
        {
            var result = SupportGemMap.FromGemJson(
                "{\"name\":\"Ignite Proliferation Support\",\"tags\":[\"Fire\",\"Support\"],\"upside\":\"a\",\"downside\":\"b\",\"explicitMods\":[{\"text\":\"Supported Skills deal #% more Damage with Ignite\",\"values\":{\"normal\":20}}]}");
            Assert.IsTrue(result.CanIngest);
            Assert.AreEqual(GemStat.AilmentDamage, result.Modifiers[0].Stat);
            Assert.AreEqual(1.2f, result.Modifiers[0].Value, 0.001f);
        }

        [Test]
        public void Hallow_MapsFlameFlagExtraFireAndMagnitude()
        {
            var result = SupportGemMap.FromGemJson(
                "{\"name\":\"Hallow Support\",\"tags\":[\"Physical\",\"Fire\",\"Support\",\"Melee\",\"Attack\"],\"upside\":\"a\",\"downside\":\"b\",\"explicitMods\":[{\"text\":\"#% increased magnitude of Hallowing Flame inflicted by Supported Skills\",\"values\":{\"normal\":16}},{\"text\":\"Supported Skills inflict Hallowing Flame on Melee Hit\",\"values\":null}]}");
            Assert.IsTrue(result.CanIngest);
            Assert.AreEqual(GemTag.Physical | GemTag.Fire | GemTag.Support | GemTag.Melee | GemTag.Attack, result.Tags);
            Assert.AreEqual(3, result.Modifiers.Length);
            Assert.AreEqual(GemStat.HallowingFlame, result.Modifiers[0].Stat);
            Assert.AreEqual(1f, result.Modifiers[0].Value, 0.001f);
            Assert.AreEqual(GemStat.PhysAsExtraFire, result.Modifiers[1].Stat);
            Assert.AreEqual(RoleModifierOperation.Set, result.Modifiers[1].Operation);
            Assert.AreEqual(0.25f, result.Modifiers[1].Value, 0.001f);
            Assert.AreEqual(GemStat.PhysAsExtraFire, result.Modifiers[2].Stat);
            Assert.AreEqual(RoleModifierOperation.Multiply, result.Modifiers[2].Operation);
            Assert.AreEqual(1.16f, result.Modifiers[2].Value, 0.001f);
        }

        [Test]
        public void Fortify_MapsMeleeDamageAndAilmentDamage()
        {
            var result = SupportGemMap.FromGemJson(
                "{\"name\":\"Fortify Support\",\"tags\":[\"Attack\",\"Support\",\"Melee\"],\"category\":\"utility_control\",\"upside\":\"a\",\"downside\":\"b\",\"explicitMods\":[{\"text\":\"Supported Skills deal #% more Melee Damage\",\"values\":{\"normal\":14}},{\"text\":\"Supported Skills deal #% more Damage with Ailments caused by Melee Hits\",\"values\":{\"normal\":14}}]}");
            Assert.IsTrue(result.CanIngest);
            Assert.AreEqual(2, result.Modifiers.Length);
            Assert.AreEqual(GemStat.Damage, result.Modifiers[0].Stat);
            Assert.AreEqual(1.14f, result.Modifiers[0].Value, 0.001f);
            Assert.AreEqual(GemStat.AilmentDamage, result.Modifiers[1].Stat);
            Assert.AreEqual(1.14f, result.Modifiers[1].Value, 0.001f);
        }

        [Test]
        public void DeadlyAilments_MapsAilmentMoreAndWikiLessHits()
        {
            var result = SupportGemMap.FromGemJson(
                "{\"name\":\"Deadly Ailments Support\",\"tags\":[\"Support\"],\"category\":\"offensive\",\"upside\":\"a\",\"downside\":\"b\",\"explicitMods\":[{\"text\":\"Supported Skills deal #% more Damage with Ailments\",\"values\":{\"normal\":36}},{\"text\":\"Supported Skills deal #% less Damage with Hits\",\"values\":null}]}");
            Assert.IsTrue(result.CanIngest);
            Assert.AreEqual(2, result.Modifiers.Length);
            Assert.AreEqual(GemStat.AilmentDamage, result.Modifiers[0].Stat);
            Assert.AreEqual(1.36f, result.Modifiers[0].Value, 0.001f);
            Assert.AreEqual(GemStat.Damage, result.Modifiers[1].Stat);
            Assert.AreEqual(0.2f, result.Modifiers[1].Value, 0.001f);
        }

        [Test]
        public void Ruthless_Skipped()
        {
            var result = SupportGemMap.FromGemJson(
                "{\"name\":\"Ruthless Support\",\"tags\":[\"Slam\",\"Support\",\"Melee\",\"Attack\"],\"category\":\"offensive\",\"upside\":\"a\",\"downside\":\"b\",\"explicitMods\":[{\"text\":\"Ruthless Blows with Supported Skills deal #% more Melee Damage\",\"values\":{\"normal\":83}}]}");
            Assert.IsFalse(result.CanIngest);
            Assert.AreEqual(0, result.Modifiers.Length);
        }

        [Test]
        public void ArrowNova_MapsDamageAndAimDelivery()
        {
            var result = SupportGemMap.FromGemJson(
                "{\"name\":\"Arrow Nova Support\",\"tags\":[\"Bow\",\"Support\",\"Projectile\"],\"category\":\"projectile_targeting\",\"upside\":\"a\",\"downside\":\"b\",\"explicitMods\":[{\"text\":\"Supported Skills deal #% less Projectile Damage\",\"values\":{\"normal\":23}},{\"text\":\"Supported Skills fire a Payload Arrow into the airProjectiles from the Supported Skill Fire from where the Payload Arrow lands\",\"values\":null},{\"text\":\"Supported Skills Fire Projectiles in a circle\",\"values\":null}]}");
            Assert.IsTrue(result.CanIngest);
            Assert.AreEqual(3, result.Modifiers.Length);
            Assert.AreEqual(GemStat.Damage, result.Modifiers[0].Stat);
            Assert.AreEqual(RoleModifierOperation.Multiply, result.Modifiers[0].Operation);
            Assert.AreEqual(0.77f, result.Modifiers[0].Value, 0.001f);
            Assert.AreEqual(GemStat.AimMode, result.Modifiers[1].Stat);
            Assert.AreEqual(RoleModifierOperation.Set, result.Modifiers[1].Operation);
            Assert.AreEqual(1f, result.Modifiers[1].Value, 0.001f);
            Assert.AreEqual(GemStat.DeliveryPattern, result.Modifiers[2].Stat);
            Assert.AreEqual(RoleModifierOperation.Set, result.Modifiers[2].Operation);
            Assert.AreEqual(1f, result.Modifiers[2].Value, 0.001f);
        }

        [Test]
        public void MeleeSplash_SetsBaseRadiusThenMoreAoe()
        {
            var result = SupportGemMap.FromGemJson(
                "{\"name\":\"Melee Splash Support\",\"tags\":[\"Support\",\"Melee\",\"Attack\",\"Strike\",\"AoE\"],\"category\":\"projectile_targeting\",\"upside\":\"a\",\"downside\":\"b\",\"explicitMods\":[{\"text\":\"Supported Skills have #% more Melee Splash Area of Effect\",\"values\":{\"normal\":27}},{\"text\":\"Supported Skills deal Splash Damage to surrounding targets\",\"values\":null},{\"text\":\"Supported Skills deal #% less Splash Damage to surrounding targets\",\"values\":null}]}");
            Assert.IsTrue(result.CanIngest);
            Assert.AreEqual(2, result.Modifiers.Length);
            Assert.AreEqual(GemStat.AoeRadius, result.Modifiers[0].Stat);
            Assert.AreEqual(RoleModifierOperation.Set, result.Modifiers[0].Operation);
            Assert.AreEqual(1.4f, result.Modifiers[0].Value, 0.001f);
            Assert.AreEqual(GemStat.AoeRadius, result.Modifiers[1].Stat);
            Assert.AreEqual(RoleModifierOperation.Multiply, result.Modifiers[1].Operation);
            Assert.AreEqual(1.27f, result.Modifiers[1].Value, 0.001f);
        }

        [Test]
        public void BallistaTotem_SkippedAsTransformation()
        {
            var result = SupportGemMap.FromGemJson(
                "{\"name\":\"Ballista Totem Support\",\"tags\":[\"Bow\",\"Projectile\",\"Support\",\"Totem\"],\"category\":\"transformation\",\"upside\":\"a\",\"downside\":\"b\",\"explicitMods\":[{\"text\":\"Supported Skills deal #% less Damage\",\"values\":{\"normal\":33}}]}");
            Assert.IsFalse(result.CanIngest);
            Assert.AreEqual(0, result.Modifiers.Length);
        }
    }
}
