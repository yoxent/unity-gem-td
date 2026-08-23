using NUnit.Framework;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class SkillGemTowerMapTests
    {
        const string CleaveJson =
            "{\"name\":\"Cleave\",\"slug\":\"Cleave\",\"tags\":[\"Attack\",\"AoE\",\"Melee\"],\"category\":\"attack\",\"header\":{\"attack_speed\":{\"kind\":\"percent\",\"value\":80}},\"radius\":{\"kind\":\"metres\",\"value\":1}}";

        const string SmiteJson =
            "{\"name\":\"Smite\",\"slug\":\"Smite\",\"tags\":[\"Lightning\",\"Melee\",\"Attack\",\"AoE\",\"Duration\",\"Strike\",\"Aura\"],\"category\":\"attack\",\"header\":{\"attack_speed\":{\"kind\":\"percent\",\"value\":85}},\"radius\":{\"kind\":\"metres\",\"value\":2.1}}";

        const string FireballJson =
            "{\"name\":\"Fireball\",\"slug\":\"Fireball\",\"tags\":[\"Projectile\",\"Spell\",\"AoE\",\"Fire\"],\"category\":\"spell\",\"header\":{\"cast_time\":{\"kind\":\"seconds\",\"value\":0.75}},\"radius\":{\"kind\":\"metres\",\"value\":1.8}}";

        const string VitalityJson =
            "{\"name\":\"Vitality\",\"slug\":\"Vitality\",\"tags\":[\"Aura\",\"Spell\",\"AoE\"],\"category\":\"aura\",\"header\":{},\"levels\":{\"4\":{\"base_damage_effectiveness\":{\"kind\":\"percent\",\"value\":100},\"You and nearby Allies Regenerate # Life per second\":{\"kind\":\"seconds\",\"value\":19.7}}},\"radius\":{\"kind\":\"metres\",\"value\":1.5}}";

        const string WarlordsMarkJson =
            "{\"name\":\"Warlord's Mark\",\"slug\":\"Warlords_Mark\",\"tags\":[\"Spell\",\"Curse\",\"Mark\"],\"category\":\"curse\",\"header\":{\"cast_time\":{\"kind\":\"seconds\",\"value\":0.5}},\"radius\":{\"kind\":\"metres\",\"value\":4.5}}";

        const string ExplosiveTrapJson =
            "{\"name\":\"Explosive Trap\",\"slug\":\"Explosive_Trap\",\"tags\":[\"Trap\",\"Spell\",\"AoE\",\"Fire\",\"Physical\"],\"category\":\"trap\",\"header\":{\"cast_time\":{\"kind\":\"seconds\",\"value\":1.0}},\"radius\":{\"kind\":\"metres\",\"value\":3.5}}";

        const string MissingAttackSpeedJson =
            "{\"name\":\"Plain Strike\",\"slug\":\"Plain_Strike\",\"tags\":[\"Attack\",\"Melee\"],\"category\":\"attack\",\"header\":{}}";

        const string LightningDurationJson =
            "{\"name\":\"Spark\",\"slug\":\"Spark\",\"tags\":[\"Spell\",\"Projectile\",\"Lightning\",\"Duration\"],\"category\":\"spell\",\"header\":{\"cast_time\":{\"kind\":\"seconds\",\"value\":0.65}}}";

        const string SplashAttackJson =
            "{\"name\":\"Shockwave\",\"slug\":\"Shockwave\",\"tags\":[\"Attack\",\"AoE\"],\"category\":\"attack\",\"header\":{\"attack_speed\":{\"kind\":\"percent\",\"value\":75}}}";

        const string MinMaxHeaderJson =
            "{\"name\":\"Ranged Hit\",\"slug\":\"Ranged_Hit\",\"tags\":[\"Attack\",\"Projectile\"],\"category\":\"attack\",\"header\":{\"attack_speed\":{\"kind\":\"percent\",\"value\":{\"min\":70,\"max\":90}}}}";

        [Test]
        public void Cleave_MapsAttackSpeedEightyAndMeleeAoeTags()
        {
            var r = SkillGemTowerMap.FromJson(CleaveJson);
            Assert.AreEqual(1f, r.AttackTime, 0.001f);
            Assert.AreEqual(80f, r.AttackSpeed, 0.001f);
            Assert.IsTrue((r.Tags & GemTag.Attack) != 0);
            Assert.IsTrue((r.Tags & GemTag.Aoe) != 0);
            Assert.IsTrue((r.Tags & GemTag.Melee) != 0);
            Assert.AreEqual(10f, r.Damage, 0.001f);
            Assert.AreEqual(20, r.Cost);
            Assert.GreaterOrEqual(r.TowerRadius, 3.5f);
            Assert.AreEqual(1, r.RoleKinds.Length);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Attack, r.RoleKinds[0]);
        }

        [Test]
        public void Smite_AttachesAuraRoleAndKeepsAttackSpeed()
        {
            var r = SkillGemTowerMap.FromJson(SmiteJson);
            Assert.AreEqual(85f, r.AttackSpeed, 0.001f);
            Assert.IsTrue((r.Tags & GemTag.Aura) != 0);
            Assert.AreEqual(2, r.RoleKinds.Length);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Attack, r.RoleKinds[0]);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Aura, r.RoleKinds[1]);
            Assert.AreEqual(10f, r.Damage, 0.001f);
            Assert.AreEqual(3.5f, r.TowerRadius, 0.001f);
            Assert.AreEqual(2.1f, r.AuraTowerRadius, 0.001f);
            Assert.IsTrue(r.IsActiveCatalogCompatible);
        }

        [Test]
        public void Fireball_MapsCastTimeAndPlaceholderDamage()
        {
            var r = SkillGemTowerMap.FromJson(FireballJson);
            Assert.AreEqual(0.75f, r.CastTime, 0.001f);
            Assert.AreEqual(8f, r.Damage, 0.001f);
            Assert.IsTrue((r.Tags & GemTag.Spell) != 0);
            Assert.IsTrue((r.Tags & GemTag.Projectile) != 0);
            Assert.IsTrue((r.Tags & GemTag.Aoe) != 0);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Spell, r.RoleKinds[0]);
            Assert.AreEqual(25, r.Cost);
            Assert.AreEqual(3, r.SocketCount);
            Assert.AreEqual(3.5f, r.TowerRadius, 0.001f);
            Assert.IsTrue(r.IsActiveCatalogCompatible);
        }

        [Test]
        public void Vitality_MapsAuraSocketAndZeroDamage()
        {
            var r = SkillGemTowerMap.FromJson(VitalityJson);
            Assert.AreEqual(1.5f, r.TowerRadius, 0.001f);
            Assert.AreEqual(0f, r.Damage, 0.001f);
            Assert.AreEqual(1, r.SocketCount);
            Assert.AreEqual(30, r.Cost);
            Assert.AreEqual(50f, r.ReservationPercent, 0.001f);
            Assert.IsFalse(r.AllowsHydraEvolution);
            CollectionAssert.AreEqual(new[] { 4 }, r.SourceLevels);
            Assert.IsFalse(r.IsActiveCatalogCompatible);
            CollectionAssert.Contains(
                r.UnsupportedEffectKeys,
                "You and nearby Allies Regenerate # Life per second");
        }

        [Test]
        public void WarlordsMark_MapsCurseRadiusAndCastTime()
        {
            var r = SkillGemTowerMap.FromJson(WarlordsMarkJson);
            Assert.AreEqual(4.5f, r.TowerRadius, 0.001f);
            Assert.AreEqual(0.5f, r.CastTime, 0.001f);
            Assert.AreEqual(0f, r.Damage, 0.001f);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Curse, r.RoleKinds[0]);
        }

        [Test]
        public void ExplosiveTrap_MapsTrapRadiusAndCastTime()
        {
            var r = SkillGemTowerMap.FromJson(ExplosiveTrapJson);
            Assert.AreEqual(3.5f, r.TowerRadius, 0.001f);
            Assert.AreEqual(1f, r.CastTime, 0.001f);
            Assert.AreEqual(8f, r.Damage, 0.001f);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Trap, r.RoleKinds[0]);
        }

        [Test]
        public void MissingAttackSpeed_DefaultsToOneHundred()
        {
            var r = SkillGemTowerMap.FromJson(MissingAttackSpeedJson);
            Assert.AreEqual(100f, r.AttackSpeed, 0.001f);
            Assert.AreEqual(1f, r.AttackTime, 0.001f);
        }

        [Test]
        public void TagMap_IgnoresLightningAndDuration()
        {
            var r = SkillGemTowerMap.FromJson(LightningDurationJson);
            Assert.IsTrue((r.Tags & GemTag.Spell) != 0);
            Assert.IsTrue((r.Tags & GemTag.Projectile) != 0);
            Assert.AreEqual(GemTag.Spell | GemTag.Projectile, r.Tags);
        }

        [Test]
        public void AttackAoeWithoutMeleeOrProjectile_PreservesAoeTag()
        {
            var r = SkillGemTowerMap.FromJson(SplashAttackJson);
            Assert.IsTrue((r.Tags & GemTag.Aoe) != 0);
        }

        [Test]
        public void HeaderMinMax_UsesMin()
        {
            var r = SkillGemTowerMap.FromJson(MinMaxHeaderJson);
            Assert.AreEqual(70f, r.AttackSpeed, 0.001f);
        }

    }
}
