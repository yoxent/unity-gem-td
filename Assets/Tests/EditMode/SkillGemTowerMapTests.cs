using NUnit.Framework;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class SkillGemTowerMapTests
    {
        const string CleaveJson =
            "{\"name\":\"Cleave\",\"slug\":\"Cleave\",\"tags\":[\"Attack\",\"AoE\",\"Melee\"],\"category\":\"attack\",\"header\":{\"attack_speed\":{\"kind\":\"percent\",\"value\":80}},\"levels\":{\"1\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":210},\"# metres to radius\":{\"kind\":\"metres\",\"value\":0.2}},\"5\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":511.2},\"# metres to radius\":{\"kind\":\"metres\",\"value\":1}}},\"radius\":{\"kind\":\"metres\",\"value\":1,\"by_level\":{\"1\":0.2,\"5\":1}}}";

        const string SmiteJson =
            "{\"name\":\"Smite\",\"slug\":\"Smite\",\"tags\":[\"Lightning\",\"Melee\",\"Attack\",\"AoE\",\"Duration\",\"Strike\",\"Aura\"],\"category\":\"attack\",\"header\":{\"attack_speed\":{\"kind\":\"percent\",\"value\":85}},\"radius\":{\"kind\":\"metres\",\"value\":2.1}}";

        const string FireballJson =
            "{\"name\":\"Fireball\",\"slug\":\"Fireball\",\"tags\":[\"Projectile\",\"Spell\",\"AoE\",\"Fire\"],\"category\":\"spell\",\"header\":{\"cast_time\":{\"kind\":\"seconds\",\"value\":0.75}},\"levels\":{\"1\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[19,28]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":1.1}},\"2\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[86,130]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":1.3}},\"3\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[276,414]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":1.5}},\"4\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[790,1184]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":1.6}},\"5\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[1883,2825]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":1.8}},\"6\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[3050,4575]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":2.1}},\"7\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[4898,7347]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":2.2}},\"8\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[6955,10433]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":2.3}},\"9\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[8770,13154]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":2.3}},\"10\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[11041,16562]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":2.4}}},\"radius\":{\"kind\":\"metres\",\"value\":1.8,\"by_level\":{\"1\":1.1,\"2\":1.3,\"3\":1.5,\"4\":1.6,\"5\":1.8,\"6\":2.1,\"7\":2.2,\"8\":2.3,\"9\":2.3,\"10\":2.4}}}";

        const string VitalityJson =
            "{\"name\":\"Vitality\",\"slug\":\"Vitality\",\"tags\":[\"Aura\",\"Spell\",\"AoE\"],\"category\":\"aura\",\"header\":{},\"levels\":{\"1\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"You and nearby Allies Regenerate # Life per second\":{\"kind\":\"seconds\",\"value\":19.7}}},\"radius\":{\"kind\":\"metres\",\"value\":1.5}}";

        const string AngerJson =
            "{\"name\":\"Anger\",\"slug\":\"Anger\",\"tags\":[\"Aura\",\"Spell\",\"AoE\",\"Fire\"],\"category\":\"aura\",\"header\":{\"reservation\":{\"kind\":\"percent\",\"value\":{\"amount\":50,\"resource\":\"mana\"}}},\"levels\":{\"1\":{\"You and nearby allies deal # to # additional Fire Damage with Attacks\":{\"kind\":\"flat\",\"value\":[25,36]},\"You and nearby allies deal # to # additional Fire Damage with Spells\":{\"kind\":\"flat\",\"value\":[25,36]},\"# metres to radius\":{\"kind\":\"metres\",\"value\":0.3},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}},\"5\":{\"You and nearby allies deal # to # additional Fire Damage with Attacks\":{\"kind\":\"flat\",\"value\":[109,155]},\"You and nearby allies deal # to # additional Fire Damage with Spells\":{\"kind\":\"flat\",\"value\":[109,155]},\"# metres to radius\":{\"kind\":\"metres\",\"value\":1.9},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}}},\"radius\":{\"kind\":\"metres\",\"value\":1.5}}";

        const string WarlordsMarkJson =
            "{\"name\":\"Warlord's Mark\",\"slug\":\"Warlords_Mark\",\"tags\":[\"Spell\",\"Curse\",\"Mark\"],\"category\":\"curse\",\"header\":{\"cast_time\":{\"kind\":\"seconds\",\"value\":0.5}},\"radius\":{\"kind\":\"metres\",\"value\":4.5}}";

        const string ExplosiveTrapJson =
            "{\"name\":\"Explosive Trap\",\"slug\":\"Explosive_Trap\",\"tags\":[\"Trap\",\"Spell\",\"AoE\",\"Fire\",\"Physical\"],\"category\":\"trap\",\"header\":{\"cast_time\":{\"kind\":\"seconds\",\"value\":1.0}},\"levels\":{\"1\":{\"Base explosion radius is # metres\":{\"kind\":\"metres\",\"value\":[1,1.3]},\"Deals # to # Physical Damage\":{\"kind\":\"flat\",\"value\":[10,15]},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}}},\"radius\":{\"kind\":\"metres\",\"value\":3.5}}";

        const string PyroclastMineJson =
            "{\"name\":\"Pyroclast Mine\",\"slug\":\"Pyroclast_Mine\",\"tags\":[\"Mine\",\"Spell\",\"Projectile\",\"Fire\",\"AoE\",\"Aura\",\"Nova\"],\"category\":\"mine\",\"header\":{\"cast_time\":{\"kind\":\"seconds\",\"value\":0.18},\"projectile_speed\":{\"kind\":\"metres_per_second\",\"value\":7.5}},\"levels\":{\"1\":{\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[59,89]},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}},\"5\":{\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[325,487]},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}}},\"radius\":{\"kind\":\"metres\",\"value\":3.5}}";

        const string FrostbiteJson =
            "{\"name\":\"Frostbite\",\"slug\":\"Frostbite\",\"tags\":[\"Spell\",\"AoE\",\"Duration\",\"Cold\",\"Curse\",\"Hex\"],\"category\":\"curse\",\"header\":{\"cast_time\":{\"kind\":\"seconds\",\"value\":0.5}},\"levels\":{\"1\":{\"# metres to radius\":{\"kind\":\"metres\",\"value\":0.2},\"Base duration is # seconds\":{\"kind\":\"seconds\",\"value\":8.6},\"Cursed enemies have #% to Cold Resistance\":{\"kind\":\"percent\",\"value\":-20},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}},\"5\":{\"# metres to radius\":{\"kind\":\"metres\",\"value\":1},\"Base duration is # seconds\":{\"kind\":\"seconds\",\"value\":11.8},\"Cursed enemies have #% to Cold Resistance\":{\"kind\":\"percent\",\"value\":-36},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}}},\"radius\":{\"kind\":\"metres\",\"value\":4.5}}";

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
            var attack = r.GetRolePayload(SkillGemTowerMap.RoleKind.Attack);
            Assert.AreEqual(1f, FindModifier(attack.Modifiers, RoleStat.AttackTime).Value, 0.001f);
            Assert.AreEqual(80f, FindModifier(attack.Modifiers, RoleStat.AttackSpeed).Value, 0.001f);
            Assert.AreEqual(3.5f, FindModifier(attack.Modifiers, RoleStat.TowerRadius).Value, 0.001f);
            Assert.IsTrue((r.Tags & GemTag.Attack) != 0);
            Assert.IsTrue((r.Tags & GemTag.Aoe) != 0);
            Assert.IsTrue((r.Tags & GemTag.Melee) != 0);
            Assert.AreEqual(10f, r.Damage, 0.001f);
            Assert.AreEqual(20, r.Cost);
            Assert.AreEqual(
                1f,
                FindModifier(attack.Levels[0].Modifiers, RoleStat.SplashRadius).Value,
                0.001f);
            Assert.AreEqual(
                RoleModifierOperation.Multiply,
                FindModifier(attack.Levels[0].Modifiers, RoleStat.Damage).Operation);
            Assert.AreEqual(1, r.RoleKinds.Length);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Attack, r.RoleKinds[0]);
        }

        [Test]
        public void Smite_AttachesAuraRoleAndKeepsAttackSpeed()
        {
            var r = SkillGemTowerMap.FromJson(SmiteJson);
            var attack = r.GetRolePayload(SkillGemTowerMap.RoleKind.Attack);
            var aura = r.GetRolePayload(SkillGemTowerMap.RoleKind.Aura);
            Assert.AreEqual(85f, FindModifier(attack.Modifiers, RoleStat.AttackSpeed).Value, 0.001f);
            Assert.AreEqual(3.5f, FindModifier(attack.Modifiers, RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(2.1f, FindModifier(aura.Modifiers, RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(
                50f,
                FindModifier(aura.Modifiers, RoleStat.ReservationPercent).Value,
                0.001f);
            Assert.IsTrue((r.Tags & GemTag.Aura) != 0);
            Assert.AreEqual(2, r.RoleKinds.Length);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Attack, r.RoleKinds[0]);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Aura, r.RoleKinds[1]);
            Assert.AreEqual(10f, r.Damage, 0.001f);
            Assert.IsTrue(r.IsActiveCatalogCompatible);
        }

        [Test]
        public void Fireball_MapsTenLevelsAndProjectileStats()
        {
            var r = SkillGemTowerMap.FromJson(FireballJson);
            var spell = r.GetRolePayload(SkillGemTowerMap.RoleKind.Spell);
            Assert.AreEqual(0.75f, FindModifier(spell.Modifiers, RoleStat.CastTime).Value, 0.001f);
            Assert.AreEqual(100f, FindModifier(spell.Modifiers, RoleStat.CastSpeed).Value, 0.001f);
            Assert.AreEqual(5f, FindModifier(spell.Modifiers, RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(8f, r.Damage, 0.001f);
            Assert.AreEqual(1f, FindModifier(spell.Modifiers, RoleStat.ProjectileSpeed).Value, 0.001f);
            Assert.IsTrue((r.Tags & GemTag.Spell) != 0);
            Assert.IsTrue((r.Tags & GemTag.Projectile) != 0);
            Assert.IsTrue((r.Tags & GemTag.Aoe) != 0);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Spell, r.RoleKinds[0]);
            Assert.AreEqual(25, r.Cost);
            Assert.AreEqual(3, r.SocketCount);
            Assert.AreEqual(10, spell.Levels.Length);
            Assert.AreEqual(1, spell.Levels[0].SourceLevel);
            Assert.AreEqual(10, spell.Levels[9].SourceLevel);
            Assert.AreEqual(19f, FindModifier(spell.Levels[0], RoleStat.Damage).Min, 0.001f);
            Assert.AreEqual(28f, FindModifier(spell.Levels[0], RoleStat.Damage).Max, 0.001f);
            Assert.AreEqual(11041f, FindModifier(spell.Levels[9], RoleStat.Damage).Min, 0.001f);
            Assert.AreEqual(16562f, FindModifier(spell.Levels[9], RoleStat.Damage).Max, 0.001f);
            Assert.AreEqual(1.1f, FindModifier(spell.Levels[0], RoleStat.SplashRadius).Value, 0.001f);
            Assert.AreEqual(2.4f, FindModifier(spell.Levels[9], RoleStat.SplashRadius).Value, 0.001f);
            Assert.IsFalse(HasModifier(spell.Levels[0], RoleStat.CastTime));
            Assert.IsFalse(HasModifier(spell.Levels[0], RoleStat.CastSpeed));
            Assert.IsFalse(HasModifier(spell.Levels[0], RoleStat.ProjectileSpeed));
            Assert.IsFalse(HasModifier(spell.Levels[0], RoleStat.TowerRadius));
            Assert.IsFalse(HasModifier(spell.Levels[0], RoleStat.Damage, RoleModifierOperation.Multiply));
            Assert.IsTrue(r.IsActiveCatalogCompatible);
            Assert.NotNull(spell.Effects);
            Assert.AreEqual(0, spell.Effects.Length);
            Assert.NotNull(spell.Levels[0].Effects);
            Assert.AreEqual(0, spell.Levels[0].Effects.Length);
        }

        [Test]
        public void Vitality_MapsAuraSocketAndZeroDamage()
        {
            var r = SkillGemTowerMap.FromJson(VitalityJson);
            var aura = r.GetRolePayload(SkillGemTowerMap.RoleKind.Aura);
            Assert.AreEqual(1.5f, FindModifier(aura.Modifiers, RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(
                50f,
                FindModifier(aura.Modifiers, RoleStat.ReservationPercent).Value,
                0.001f);
            Assert.AreEqual(0f, r.Damage, 0.001f);
            Assert.AreEqual(1, r.SocketCount);
            Assert.AreEqual(30, r.Cost);
            Assert.IsFalse(r.AllowsHydraEvolution);
            CollectionAssert.AreEqual(new[] { 1 }, r.SourceLevels);
            Assert.IsFalse(r.IsActiveCatalogCompatible);
            CollectionAssert.Contains(
                r.UnsupportedEffectKeys,
                "You and nearby Allies Regenerate # Life per second");
        }

        [Test]
        public void Anger_MapsAddedFireEffectsAndRadiusBonus()
        {
            var r = SkillGemTowerMap.FromJson(AngerJson);
            var aura = r.GetRolePayload(SkillGemTowerMap.RoleKind.Aura);
            Assert.AreEqual(1.5f, FindModifier(aura.Modifiers, RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(
                50f,
                FindModifier(aura.Modifiers, RoleStat.ReservationPercent).Value,
                0.001f);
            Assert.AreEqual(0, aura.Effects.Length);
            Assert.AreEqual(
                0.3f,
                FindModifier(aura.Levels[0], RoleStat.TowerRadius).Value,
                0.001f);
            Assert.AreEqual(
                RoleModifierOperation.Add,
                FindModifier(aura.Levels[0], RoleStat.TowerRadius).Operation);
            Assert.AreEqual(25f, FindEffect(aura.Levels[0], RoleEffectKind.AllyAddedAttackFireDamage).Min, 0.001f);
            Assert.AreEqual(36f, FindEffect(aura.Levels[0], RoleEffectKind.AllyAddedAttackFireDamage).Max, 0.001f);
            Assert.AreEqual(25f, FindEffect(aura.Levels[0], RoleEffectKind.AllyAddedSpellFireDamage).Min, 0.001f);
            Assert.AreEqual(36f, FindEffect(aura.Levels[0], RoleEffectKind.AllyAddedSpellFireDamage).Max, 0.001f);
            Assert.AreEqual(109f, FindEffect(aura.Levels[1], RoleEffectKind.AllyAddedAttackFireDamage).Min, 0.001f);
            Assert.AreEqual(155f, FindEffect(aura.Levels[1], RoleEffectKind.AllyAddedAttackFireDamage).Max, 0.001f);
            Assert.AreEqual(1.9f, FindModifier(aura.Levels[1], RoleStat.TowerRadius).Value, 0.001f);
            Assert.IsFalse(r.IsActiveCatalogCompatible);
            Assert.AreEqual(0, r.UnsupportedEffectKeys.Length);
        }

        [Test]
        public void WarlordsMark_MapsCurseRadiusAndCastTime()
        {
            var r = SkillGemTowerMap.FromJson(WarlordsMarkJson);
            var curse = r.GetRolePayload(SkillGemTowerMap.RoleKind.Curse);
            Assert.AreEqual(4.5f, FindModifier(curse.Modifiers, RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(0.5f, FindModifier(curse.Modifiers, RoleStat.CastTime).Value, 0.001f);
            Assert.AreEqual(0f, r.Damage, 0.001f);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Curse, r.RoleKinds[0]);
        }

        [Test]
        public void ExplosiveTrap_MapsTrapRadiusAndCastTime()
        {
            var r = SkillGemTowerMap.FromJson(ExplosiveTrapJson);
            var trap = r.GetRolePayload(SkillGemTowerMap.RoleKind.Trap);
            Assert.AreEqual(3.5f, FindModifier(trap.Modifiers, RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(1f, FindModifier(trap.Modifiers, RoleStat.CastTime).Value, 0.001f);
            Assert.AreEqual(8f, r.Damage, 0.001f);
            Assert.AreEqual(1.3f, FindModifier(trap.Levels[0], RoleStat.SplashRadius).Value, 0.001f);
            Assert.AreEqual(10f, FindModifier(trap.Levels[0], RoleStat.Damage).Min, 0.001f);
            Assert.AreEqual(15f, FindModifier(trap.Levels[0], RoleStat.Damage).Max, 0.001f);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Trap, r.RoleKinds[0]);
        }

        [Test]
        public void PyroclastMine_MapsConstantDeliveryStatsAndFlatDamageLevels()
        {
            var r = SkillGemTowerMap.FromJson(PyroclastMineJson);
            var mine = r.GetRolePayload(SkillGemTowerMap.RoleKind.Mine);
            Assert.AreEqual(0.18f, FindModifier(mine.Modifiers, RoleStat.CastTime).Value, 0.001f);
            Assert.AreEqual(100f, FindModifier(mine.Modifiers, RoleStat.CastSpeed).Value, 0.001f);
            Assert.AreEqual(3.5f, FindModifier(mine.Modifiers, RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(1f, FindModifier(mine.Modifiers, RoleStat.ProjectileSpeed).Value, 0.001f);
            Assert.AreEqual(59f, FindModifier(mine.Levels[0], RoleStat.Damage).Min, 0.001f);
            Assert.AreEqual(89f, FindModifier(mine.Levels[0], RoleStat.Damage).Max, 0.001f);
            Assert.AreEqual(325f, FindModifier(mine.Levels[1], RoleStat.Damage).Min, 0.001f);
            Assert.AreEqual(487f, FindModifier(mine.Levels[1], RoleStat.Damage).Max, 0.001f);
            Assert.IsFalse(HasModifier(mine.Levels[0], RoleStat.CastTime));
            Assert.IsFalse(HasModifier(mine.Levels[0], RoleStat.CastSpeed));
            Assert.IsFalse(HasModifier(mine.Levels[0], RoleStat.TowerRadius));
            Assert.IsFalse(HasModifier(mine.Levels[0], RoleStat.ProjectileSpeed));
        }

        [Test]
        public void Frostbite_MapsDurationResistAndRadiusBonus()
        {
            var r = SkillGemTowerMap.FromJson(FrostbiteJson);
            var curse = r.GetRolePayload(SkillGemTowerMap.RoleKind.Curse);
            Assert.AreEqual(0.5f, FindModifier(curse.Modifiers, RoleStat.CastTime).Value, 0.001f);
            Assert.AreEqual(100f, FindModifier(curse.Modifiers, RoleStat.CastSpeed).Value, 0.001f);
            Assert.AreEqual(4.5f, FindModifier(curse.Modifiers, RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(0, curse.Effects.Length);
            Assert.AreEqual(
                0.2f,
                FindModifier(curse.Levels[0], RoleStat.TowerRadius).Value,
                0.001f);
            Assert.AreEqual(
                RoleModifierOperation.Add,
                FindModifier(curse.Levels[0], RoleStat.TowerRadius).Operation);
            Assert.AreEqual(8.6f, FindEffect(curse.Levels[0], RoleEffectKind.SkillDuration).Value, 0.001f);
            Assert.AreEqual(-20f, FindEffect(curse.Levels[0], RoleEffectKind.EnemyColdResistance).Value, 0.001f);
            Assert.AreEqual(1f, FindModifier(curse.Levels[1], RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(11.8f, FindEffect(curse.Levels[1], RoleEffectKind.SkillDuration).Value, 0.001f);
            Assert.AreEqual(-36f, FindEffect(curse.Levels[1], RoleEffectKind.EnemyColdResistance).Value, 0.001f);
        }

        [Test]
        public void MissingAttackSpeed_UsesDefaultAttackSpeedModifier()
        {
            var r = SkillGemTowerMap.FromJson(MissingAttackSpeedJson);
            var attack = r.GetRolePayload(SkillGemTowerMap.RoleKind.Attack);
            Assert.AreEqual(100f, FindModifier(attack.Modifiers, RoleStat.AttackSpeed).Value, 0.001f);
            Assert.AreEqual(1f, FindModifier(attack.Modifiers, RoleStat.AttackTime).Value, 0.001f);
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
            var attack = r.GetRolePayload(SkillGemTowerMap.RoleKind.Attack);
            Assert.AreEqual(70f, FindModifier(attack.Modifiers, RoleStat.AttackSpeed).Value, 0.001f);
        }

        static RoleStatModifier FindModifier(RoleLevelDefinition level, RoleStat stat)
        {
            return FindModifier(level.Modifiers, stat);
        }

        static RoleEffectModifier FindEffect(RoleLevelDefinition level, RoleEffectKind kind)
        {
            for (var i = 0; i < level.Effects.Length; i++)
            {
                if (level.Effects[i].Kind == kind)
                    return level.Effects[i];
            }

            Assert.Fail("Missing effect " + kind);
            return default;
        }

        static RoleStatModifier FindModifier(RoleStatModifier[] modifiers, RoleStat stat)
        {
            for (var i = 0; i < modifiers.Length; i++)
            {
                if (modifiers[i].Stat == stat)
                    return modifiers[i];
            }

            Assert.Fail("Missing modifier " + stat);
            return default;
        }

        static bool HasModifier(RoleLevelDefinition level, RoleStat stat)
        {
            return HasModifier(level, stat, RoleModifierOperation.Set)
                || HasModifier(level, stat, RoleModifierOperation.Add)
                || HasModifier(level, stat, RoleModifierOperation.Multiply);
        }

        static bool HasModifier(
            RoleLevelDefinition level,
            RoleStat stat,
            RoleModifierOperation operation)
        {
            for (var i = 0; i < level.Modifiers.Length; i++)
            {
                if (level.Modifiers[i].Stat == stat
                    && level.Modifiers[i].Operation == operation)
                    return true;
            }

            return false;
        }

    }
}
