using NUnit.Framework;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class SkillGemTowerMapTests
    {
        const string CleaveJson =
            "{\"name\":\"Cleave\",\"slug\":\"Cleave\",\"description\":\"The character swings their weapon (or both weapons if dual wielding) in an arc, damaging monsters in an area in front of them.\",\"tags\":[\"Attack\",\"AoE\",\"Melee\"],\"category\":\"attack\",\"header\":{\"attack_speed\":{\"kind\":\"percent\",\"value\":80}},\"levels\":{\"1\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":210},\"# metres to radius\":{\"kind\":\"metres\",\"value\":0.2}},\"5\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":511.2},\"# metres to radius\":{\"kind\":\"metres\",\"value\":1}}},\"radius\":{\"kind\":\"metres\",\"value\":1,\"by_level\":{\"1\":0.2,\"5\":1}}}";

        const string SmiteJson =
            "{\"name\":\"Smite\",\"slug\":\"Smite\",\"tags\":[\"Lightning\",\"Melee\",\"Attack\",\"AoE\",\"Duration\",\"Strike\",\"Aura\"],\"category\":\"attack\",\"header\":{\"attack_speed\":{\"kind\":\"percent\",\"value\":85}},\"radius\":{\"kind\":\"metres\",\"value\":2.1}}";

        const string FireballJson =
            "{\"name\":\"Fireball\",\"slug\":\"Fireball\",\"tags\":[\"Projectile\",\"Spell\",\"AoE\",\"Fire\"],\"category\":\"spell\",\"header\":{\"cast_time\":{\"kind\":\"seconds\",\"value\":0.75}},\"levels\":{\"1\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[19,28]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":1.1}},\"2\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[86,130]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":1.3}},\"3\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[276,414]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":1.5}},\"4\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[790,1184]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":1.6}},\"5\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[1883,2825]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":1.8}},\"6\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[3050,4575]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":2.1}},\"7\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[4898,7347]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":2.2}},\"8\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[6955,10433]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":2.3}},\"9\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[8770,13154]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":2.3}},\"10\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[11041,16562]},\"Base radius is # metre\":{\"kind\":\"metres\",\"value\":2.4}}},\"radius\":{\"kind\":\"metres\",\"value\":1.8,\"by_level\":{\"1\":1.1,\"2\":1.3,\"3\":1.5,\"4\":1.6,\"5\":1.8,\"6\":2.1,\"7\":2.2,\"8\":2.3,\"9\":2.3,\"10\":2.4}}}";

        const string VitalityJson =
            "{\"name\":\"Vitality\",\"slug\":\"Vitality\",\"tags\":[\"Aura\",\"Spell\",\"AoE\"],\"category\":\"aura\",\"header\":{},\"levels\":{\"1\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100},\"You and nearby Allies Regenerate # Life per second\":{\"kind\":\"seconds\",\"value\":19.7}}},\"radius\":{\"kind\":\"metres\",\"value\":1.5}}";

        const string AngerJson =
            "{\"name\":\"Anger\",\"slug\":\"Anger\",\"tags\":[\"Aura\",\"Spell\",\"AoE\",\"Fire\"],\"category\":\"aura\",\"header\":{\"reservation\":{\"kind\":\"percent\",\"value\":{\"amount\":50,\"resource\":\"mana\"}}},\"levels\":{\"1\":{\"You and nearby allies deal # to # additional Fire Damage with Attacks\":{\"kind\":\"flat\",\"value\":[25,36]},\"You and nearby allies deal # to # additional Fire Damage with Spells\":{\"kind\":\"flat\",\"value\":[25,36]},\"# metres to radius\":{\"kind\":\"metres\",\"value\":0.3},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}},\"5\":{\"You and nearby allies deal # to # additional Fire Damage with Attacks\":{\"kind\":\"flat\",\"value\":[109,155]},\"You and nearby allies deal # to # additional Fire Damage with Spells\":{\"kind\":\"flat\",\"value\":[109,155]},\"# metres to radius\":{\"kind\":\"metres\",\"value\":1.9},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}},\"10\":{\"You and nearby allies deal # to # additional Fire Damage with Attacks\":{\"kind\":\"flat\",\"value\":[296,423]},\"You and nearby allies deal # to # additional Fire Damage with Spells\":{\"kind\":\"flat\",\"value\":[296,423]},\"# metres to radius\":{\"kind\":\"metres\",\"value\":3.4},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}}},\"radius\":{\"kind\":\"metres\",\"value\":1.5}}";

        const string PurityOfIceJson =
            "{\"name\":\"Purity of Ice\",\"slug\":\"Purity_of_Ice\",\"tags\":[\"Aura\",\"Spell\",\"AoE\",\"Cold\"],\"category\":\"aura\",\"header\":{\"reservation\":{\"kind\":\"percent\",\"value\":{\"amount\":35,\"resource\":\"mana\"}}},\"levels\":{\"1\":{\"You and nearby allies gain #% additional Cold Resistance\":{\"kind\":\"percent\",\"value\":25},\"#% to maximum Cold Resistance\":{\"kind\":\"percent\",\"value\":0},\"# metres to radius\":{\"kind\":\"metres\",\"value\":0.3},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}},\"10\":{\"You and nearby allies gain #% additional Cold Resistance\":{\"kind\":\"percent\",\"value\":56},\"#% to maximum Cold Resistance\":{\"kind\":\"percent\",\"value\":5},\"# metres to radius\":{\"kind\":\"metres\",\"value\":3.4},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}}},\"radius\":{\"kind\":\"metres\",\"value\":1.5}}";

        const string HasteJson =
            "{\"name\":\"Haste\",\"slug\":\"Haste\",\"tags\":[\"Aura\",\"Spell\",\"AoE\"],\"category\":\"aura\",\"header\":{\"reservation\":{\"kind\":\"percent\",\"value\":{\"amount\":50,\"resource\":\"mana\"}}},\"levels\":{\"1\":{\"You and nearby allies gain #% increased Attack Speed\":{\"kind\":\"percent\",\"value\":16},\"You and nearby allies gain #% increased Cast Speed\":{\"kind\":\"percent\",\"value\":16},\"You and nearby allies gain #% increased Movement Speed\":{\"kind\":\"percent\",\"value\":11},\"# metres to radius\":{\"kind\":\"metres\",\"value\":0.3},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}},\"10\":{\"You and nearby allies gain #% increased Attack Speed\":{\"kind\":\"percent\",\"value\":32},\"You and nearby allies gain #% increased Cast Speed\":{\"kind\":\"percent\",\"value\":32},\"You and nearby allies gain #% increased Movement Speed\":{\"kind\":\"percent\",\"value\":21},\"# metres to radius\":{\"kind\":\"metres\",\"value\":3.4},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}}},\"radius\":{\"kind\":\"metres\",\"value\":1.5}}";

        const string WrathJson =
            "{\"name\":\"Wrath\",\"slug\":\"Wrath\",\"tags\":[\"Aura\",\"Spell\",\"AoE\",\"Lightning\"],\"category\":\"aura\",\"header\":{\"reservation\":{\"kind\":\"percent\",\"value\":{\"amount\":50,\"resource\":\"mana\"}}},\"levels\":{\"1\":{\"You and nearby allies deal # to # additional Lightning Damage with Attacks\":{\"kind\":\"flat\",\"value\":[4,57]},\"You and nearby allies deal #% more Spell Lightning Damage\":{\"kind\":\"percent\",\"value\":16},\"# metres to radius\":{\"kind\":\"metres\",\"value\":0.3},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}},\"10\":{\"You and nearby allies deal # to # additional Lightning Damage with Attacks\":{\"kind\":\"flat\",\"value\":[42,676]},\"You and nearby allies deal #% more Spell Lightning Damage\":{\"kind\":\"percent\",\"value\":26},\"# metres to radius\":{\"kind\":\"metres\",\"value\":3.4},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}}},\"radius\":{\"kind\":\"metres\",\"value\":1.5}}";

        const string FleshAndStoneJson =
            "{\"name\":\"Flesh and Stone\",\"slug\":\"Flesh_and_Stone\",\"tags\":[\"Spell\",\"Aura\",\"AoE\",\"Stance\",\"Physical\"],\"category\":\"aura\",\"header\":{\"reservation\":{\"kind\":\"percent\",\"value\":{\"amount\":25,\"resource\":\"mana\"}}},\"levels\":{\"1\":{\"#% increased Cooldown Recovery Rate\":{\"kind\":\"percent\",\"value\":7},\"While in Sand Stance, Buff makes you take up to #% less Damage from Enemies in Aura\":{\"kind\":\"percent\",\"value\":11},\"While in Blood Stance, Aura makes Enemies take up to #% more Physical Damage from Hits\":{\"kind\":\"percent\",\"value\":12},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}},\"10\":{\"#% increased Cooldown Recovery Rate\":{\"kind\":\"percent\",\"value\":100},\"While in Sand Stance, Buff makes you take up to #% less Damage from Enemies in Aura\":{\"kind\":\"percent\",\"value\":27},\"While in Blood Stance, Aura makes Enemies take up to #% more Physical Damage from Hits\":{\"kind\":\"percent\",\"value\":27},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}}},\"radius\":{\"kind\":\"metres\",\"value\":1.5}}";

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

        const string MoltenStrikeJson =
            "{\"name\":\"Molten Strike\",\"slug\":\"Molten_Strike\",\"tags\":[\"Attack\",\"Projectile\",\"AoE\",\"Melee\",\"Strike\",\"Fire\"],\"category\":\"attack\",\"header\":{},\"levels\":{\"1\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":146,\"poedb_column\":\"Base Damage\"}},\"5\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":329.2,\"poedb_column\":\"Base Damage\"}},\"10\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":700.3,\"poedb_column\":\"Base Damage\"}}}}";
        const string FirestormJson =
            "{\"name\":\"Firestorm\",\"slug\":\"Firestorm\",\"tags\":[\"Spell\",\"AoE\",\"Duration\",\"Fire\"],\"category\":\"spell\",\"header\":{\"cast_time\":{\"kind\":\"seconds\",\"value\":0.75}},\"levels\":{\"1\":{\"Deals # to # Fire Damage\":{\"kind\":\"flat\",\"value\":[44,66]},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}}}}";
        const string EarthquakeJson =
            "{\"name\":\"Earthquake\",\"slug\":\"Earthquake\",\"description\":\"Smashes the ground, dealing damage in an area and cracking the earth. The crack will erupt in a powerful aftershock after a duration. Cracks created before the first one has erupted will not generate their own aftershocks.\",\"tags\":[\"Attack\",\"AoE\",\"Melee\",\"Duration\",\"Slam\"],\"category\":\"attack\",\"header\":{\"attack_speed\":{\"kind\":\"percent\",\"value\":75}},\"levels\":{\"1\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":155.5,\"poedb_column\":\"Base Damage\"}},\"10\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":335.5,\"poedb_column\":\"Base Damage\"}}}}";
        const string SplitArrowJson =
            "{\"name\":\"Split Arrow\",\"slug\":\"Split_Arrow\",\"description\":\"Fires multiple arrows at different targets.\",\"tags\":[\"Attack\",\"Projectile\",\"Bow\"],\"category\":\"attack\",\"header\":{\"attack_speed\":{\"kind\":\"percent\",\"value\":110}},\"levels\":{\"1\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":123.6},\"Fires # Arrows\":{\"kind\":\"flat\",\"value\":[6,1]}},\"10\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":160.8},\"Fires # Arrows\":{\"kind\":\"flat\",\"value\":[13,1]}}}}";
        const string BarrageJson =
            "{\"name\":\"Barrage\",\"slug\":\"Barrage\",\"description\":\"After a short preparation time, you fire individual projectiles repeatedly with a Bow or Wand.\",\"tags\":[\"Attack\",\"Projectile\",\"Bow\"],\"category\":\"attack\",\"header\":{\"attack_speed\":{\"kind\":\"percent\",\"value\":115}},\"explicitMods\":[{\"kind\":\"scaling\",\"text\":\"Fires 6 Projectiles\",\"card_text\":\"Fires 6 Projectiles\"}],\"levels\":{\"1\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":48.2}},\"10\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":60.6}}}}";
        const string IceShotJson =
            "{\"name\":\"Ice Shot\",\"slug\":\"Ice_Shot\",\"description\":\"Fires an arrow that converts some physical damage to cold on its target and converts all physical damage to cold in a cone behind that target.\",\"tags\":[\"Attack\",\"Projectile\",\"AoE\",\"Cold\",\"Bow\"],\"category\":\"attack\",\"header\":{},\"explicitMods\":[{\"kind\":\"scaling\",\"text\":\"Base radius is 2.4 metres\",\"card_text\":\"Base radius is 2.4 metres\"}],\"levels\":{\"1\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":157.4}}}}";
        const string CobraLashJson =
            "{\"name\":\"Cobra Lash\",\"slug\":\"Cobra_Lash\",\"description\":\"Fires a poisonous projectile based on your weapon that will chain between enemies.\",\"tags\":[\"Attack\",\"Projectile\",\"Chaos\"],\"category\":\"attack\",\"header\":{\"attack_speed\":{\"kind\":\"percent\",\"value\":120}},\"levels\":{\"1\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":137.6},\"Chains # Times\":{\"kind\":\"flat\",\"value\":3}},\"10\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":268.2},\"Chains # Times\":{\"kind\":\"flat\",\"value\":7}}}}";






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
            Assert.AreEqual(
                "The character swings their weapon (or both weapons if dual wielding) in an arc, damaging monsters in an area in front of them.",
                r.Description);
            SkillGemTowerMap.ResolveFireBehavior(r.Tags, r.Slug, out var aim, out var delivery);
            Assert.AreEqual(AimMode.Direct, aim);
            Assert.AreEqual(DeliveryPattern.WarpStrike, delivery);
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
        public void ResolveProofMix_CompletedProofSlugs_MatchCrawl()
        {
            AssertHundred(SkillGemTowerMap.ResolveProofMix("Fireball"), DamageType.Fire);
            AssertHundred(SkillGemTowerMap.ResolveProofMix("Firestorm"), DamageType.Fire);
            AssertHundred(SkillGemTowerMap.ResolveProofMix("Burning_Arrow"), DamageType.Fire);
            AssertHundred(SkillGemTowerMap.ResolveProofMix("Frostbolt"), DamageType.Cold);
            AssertHundred(SkillGemTowerMap.ResolveProofMix("Ice_Nova"), DamageType.Cold);
            AssertHundred(SkillGemTowerMap.ResolveProofMix("Arc"), DamageType.Lightning);
            AssertHundred(SkillGemTowerMap.ResolveProofMix("Heavy_Strike"), DamageType.Physical);
            AssertHundred(SkillGemTowerMap.ResolveProofMix("Earthquake"), DamageType.Physical);
            AssertHundred(SkillGemTowerMap.ResolveProofMix("Cleave"), DamageType.Physical);
            AssertHundred(SkillGemTowerMap.ResolveProofMix("Split_Arrow"), DamageType.Physical);
            AssertHundred(SkillGemTowerMap.ResolveProofMix("Barrage"), DamageType.Physical);
            AssertConverted(SkillGemTowerMap.ResolveProofMix("Molten_Strike"), DamageType.Fire, 60);
            AssertConverted(SkillGemTowerMap.ResolveProofMix("Lightning_Arrow"), DamageType.Lightning, 50);
            AssertConverted(SkillGemTowerMap.ResolveProofMix("Ice_Shot"), DamageType.Cold, 60);
            AssertConverted(SkillGemTowerMap.ResolveProofMix("Cobra_Lash"), DamageType.Chaos, 60);
            Assert.IsTrue(DamageMix.IsEmpty(SkillGemTowerMap.ResolveProofMix("Smite")));
        }

        static void AssertHundred(DamageTypeShare[] mix, DamageType type)
        {
            Assert.IsTrue(DamageMix.TryValidate(mix, out _));
            Assert.AreEqual(1, mix.Length);
            Assert.AreEqual(type, mix[0].Type);
            Assert.AreEqual(100, mix[0].Percent);
        }

        static void AssertConverted(DamageTypeShare[] mix, DamageType toType, int convertedPercent)
        {
            Assert.IsTrue(DamageMix.TryValidate(mix, out _));
            Assert.AreEqual(2, mix.Length);
            Assert.AreEqual(DamageType.Physical, mix[0].Type);
            Assert.AreEqual(100 - convertedPercent, mix[0].Percent);
            Assert.AreEqual(toType, mix[1].Type);
            Assert.AreEqual(convertedPercent, mix[1].Percent);
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
            Assert.AreEqual(3, aura.Levels.Length);
            Assert.AreEqual(10, aura.Levels[2].SourceLevel);
            Assert.AreEqual(296f, FindEffect(aura.Levels[2], RoleEffectKind.AllyAddedAttackFireDamage).Min, 0.001f);
            Assert.AreEqual(423f, FindEffect(aura.Levels[2], RoleEffectKind.AllyAddedAttackFireDamage).Max, 0.001f);
            Assert.AreEqual(3.4f, FindModifier(aura.Levels[2], RoleStat.TowerRadius).Value, 0.001f);
            Assert.IsFalse(HasModifier(aura.Modifiers, RoleStat.Damage));
            Assert.IsFalse(HasModifier(aura.Levels[0], RoleStat.Damage));
            Assert.IsFalse(HasModifier(aura.Levels[0], RoleStat.SplashRadius));
            Assert.IsFalse(r.IsActiveCatalogCompatible);
            Assert.AreEqual(0, r.UnsupportedEffectKeys.Length);
        }

        [Test]
        public void AuraCatalog_UnsupportedEffects_StayUnmapped()
        {
            var ice = SkillGemTowerMap.FromJson(PurityOfIceJson);
            var iceRole = ice.GetRolePayload(SkillGemTowerMap.RoleKind.Aura);
            Assert.AreEqual("Purity_of_Ice", ice.Slug);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Aura, ice.RoleKinds[0]);
            Assert.AreEqual(0f, ice.Damage, 0.001f);
            Assert.AreEqual(
                35f,
                FindModifier(iceRole.Modifiers, RoleStat.ReservationPercent).Value,
                0.001f);
            Assert.AreEqual(1.5f, FindModifier(iceRole.Modifiers, RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(0, iceRole.Levels[0].Effects.Length);
            Assert.IsFalse(HasEffect(iceRole.Levels[0], RoleEffectKind.EnemyColdResistance));
            Assert.IsFalse(HasModifier(iceRole.Levels[0], RoleStat.Damage));
            Assert.IsFalse(HasModifier(iceRole.Levels[0], RoleStat.SplashRadius));
            Assert.IsFalse(ice.IsActiveCatalogCompatible);
            CollectionAssert.Contains(
                ice.UnsupportedEffectKeys,
                "You and nearby allies gain #% additional Cold Resistance");
            CollectionAssert.Contains(
                ice.UnsupportedEffectKeys,
                "#% to maximum Cold Resistance");

            var haste = SkillGemTowerMap.FromJson(HasteJson);
            var hasteRole = haste.GetRolePayload(SkillGemTowerMap.RoleKind.Aura);
            Assert.AreEqual(0, hasteRole.Levels[0].Effects.Length);
            Assert.IsFalse(HasModifier(hasteRole.Levels[0], RoleStat.Damage));
            CollectionAssert.Contains(
                haste.UnsupportedEffectKeys,
                "You and nearby allies gain #% increased Attack Speed");
            CollectionAssert.Contains(
                haste.UnsupportedEffectKeys,
                "You and nearby allies gain #% increased Cast Speed");
            CollectionAssert.Contains(
                haste.UnsupportedEffectKeys,
                "You and nearby allies gain #% increased Movement Speed");

            var wrath = SkillGemTowerMap.FromJson(WrathJson);
            var wrathRole = wrath.GetRolePayload(SkillGemTowerMap.RoleKind.Aura);
            Assert.AreEqual(0, wrathRole.Levels[0].Effects.Length);
            Assert.IsFalse(HasEffect(wrathRole.Levels[0], RoleEffectKind.AllyAddedAttackFireDamage));
            CollectionAssert.Contains(
                wrath.UnsupportedEffectKeys,
                "You and nearby allies deal # to # additional Lightning Damage with Attacks");
            CollectionAssert.Contains(
                wrath.UnsupportedEffectKeys,
                "You and nearby allies deal #% more Spell Lightning Damage");

            var flesh = SkillGemTowerMap.FromJson(FleshAndStoneJson);
            var fleshRole = flesh.GetRolePayload(SkillGemTowerMap.RoleKind.Aura);
            Assert.AreEqual(
                25f,
                FindModifier(fleshRole.Modifiers, RoleStat.ReservationPercent).Value,
                0.001f);
            Assert.AreEqual(0, fleshRole.Levels[0].Effects.Length);
            Assert.AreEqual(GemTag.Spell | GemTag.Aura | GemTag.Aoe | GemTag.Stance | GemTag.Physical, flesh.Tags);
            CollectionAssert.Contains(
                flesh.UnsupportedEffectKeys,
                "#% increased Cooldown Recovery Rate");
            CollectionAssert.Contains(
                flesh.UnsupportedEffectKeys,
                "While in Sand Stance, Buff makes you take up to #% less Damage from Enemies in Aura");
            CollectionAssert.Contains(
                flesh.UnsupportedEffectKeys,
                "While in Blood Stance, Aura makes Enemies take up to #% more Physical Damage from Hits");
        }

        [Test]
        public void WarlordsMark_MapsCurseRadiusAndNoCastTime()
        {
            var r = SkillGemTowerMap.FromJson(WarlordsMarkJson);
            var curse = r.GetRolePayload(SkillGemTowerMap.RoleKind.Curse);
            Assert.AreEqual(3f, FindModifier(curse.Modifiers, RoleStat.TowerRadius).Value, 0.001f);
            Assert.IsFalse(HasModifier(curse.Modifiers, RoleStat.CastTime));
            Assert.AreEqual(0f, r.Damage, 0.001f);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Curse, r.RoleKinds[0]);
            SkillGemTowerMap.ResolveFireBehavior(
                r.Tags,
                r.Slug,
                SkillGemTowerMap.RoleKind.Curse,
                out var aim,
                out var delivery);
            Assert.AreEqual(AimMode.Direct, aim);
            Assert.AreEqual(DeliveryPattern.CasterNova, delivery);
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
        public void SpectralThrow_MapsDescriptionWhenLevelsAreOnlyDamagePercent()
        {
            var r = SkillGemTowerMap.FromJson(
                "{\"name\":\"Spectral Throw\",\"slug\":\"Spectral_Throw\",\"tags\":[\"Attack\",\"Projectile\"],\"category\":\"attack\",\"description\":\"Throws a spectral copy of your melee weapon. It flies out and then returns to you, in a spinning attack that damages enemies in its path.\",\"header\":{},\"levels\":{\"1\":{\"damage_percent\":{\"kind\":\"percent\",\"value\":100}}}}");
            Assert.AreEqual(
                "Throws a spectral copy of your melee weapon. It flies out and then returns to you, in a spinning attack that damages enemies in its path.",
                r.Description);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Attack, r.RoleKinds[0]);
            Assert.AreEqual(10f, r.Damage, 0.001f);
        }

        [Test]
        public void SummonSkitterbots_MapsAsAuraNotMine()
        {
            var r = SkillGemTowerMap.FromJson(
                "{\"name\":\"Summon Skitterbots\",\"slug\":\"Summon_Skitterbots\",\"tags\":[\"Trap\",\"Mine\",\"Spell\",\"Minion\",\"Cold\",\"Lightning\",\"AoE\",\"Aura\"],\"category\":\"aura\",\"description\":\"Summon a Chilling Skitterbot and a Shocking Skitterbot, which will trigger your traps and detonate your mines.\",\"header\":{\"reservation\":{\"kind\":\"percent\",\"value\":{\"amount\":35,\"resource\":\"mana\"}}},\"radius\":{\"kind\":\"metres\",\"value\":1.5}}");
            Assert.AreEqual("aura", r.Category);
            Assert.AreEqual(1, r.RoleKinds.Length);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Aura, r.RoleKinds[0]);
            Assert.IsNull(r.GetRolePayload(SkillGemTowerMap.RoleKind.Mine));
            Assert.IsFalse(r.IsActiveCatalogCompatible);
            Assert.AreEqual(0f, r.Damage, 0.001f);
            Assert.AreEqual(1, r.SocketCount);
            Assert.AreEqual(30, r.Cost);
            var aura = r.GetRolePayload(SkillGemTowerMap.RoleKind.Aura);
            Assert.AreEqual(35f, FindModifier(aura.Modifiers, RoleStat.ReservationPercent).Value, 0.001f);
            Assert.AreEqual(1.5f, FindModifier(aura.Modifiers, RoleStat.TowerRadius).Value, 0.001f);
            StringAssert.StartsWith("Summon a Chilling Skitterbot", r.Description);
        }

        [Test]
        public void Frostbite_MapsAuthoredResistAndRadius_IgnoresPoEDuration()
        {
            var r = SkillGemTowerMap.FromJson(FrostbiteJson);
            var curse = r.GetRolePayload(SkillGemTowerMap.RoleKind.Curse);
            Assert.IsFalse(HasModifier(curse.Modifiers, RoleStat.CastTime));
            Assert.AreEqual(3f, FindModifier(curse.Modifiers, RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(0, curse.Effects.Length);
            Assert.AreEqual(3f, FindModifier(curse.Levels[0], RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(
                RoleModifierOperation.Set,
                FindModifier(curse.Levels[0], RoleStat.TowerRadius).Operation);
            Assert.IsFalse(HasEffect(curse.Levels[0], RoleEffectKind.SkillDuration));
            Assert.AreEqual(-30f, FindEffect(curse.Levels[0], RoleEffectKind.EnemyColdResistance).Value, 0.001f);
            Assert.AreEqual(5.25f, FindModifier(curse.Levels[1], RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(-50f, FindEffect(curse.Levels[1], RoleEffectKind.EnemyColdResistance).Value, 0.001f);
        }

        [Test]
        public void CurseCatalog_ElementalWeakness_MapsAuthoredElementalResists()
        {
            var r = SkillGemTowerMap.FromJson(
                "{\"name\":\"Elemental Weakness\",\"slug\":\"Elemental_Weakness\",\"tags\":[\"Spell\",\"AoE\",\"Duration\",\"Curse\",\"Hex\"],\"category\":\"curse\",\"header\":{\"cast_time\":{\"kind\":\"seconds\",\"value\":0.5}},\"levels\":{\"1\":{\"Base duration is # seconds\":{\"kind\":\"seconds\",\"value\":8.6},\"# metres to radius\":{\"kind\":\"metres\",\"value\":0.2},\"Cursed enemies have #% to Elemental Resistances\":{\"kind\":\"percent\",\"value\":-18},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}},\"10\":{\"Base duration is # seconds\":{\"kind\":\"seconds\",\"value\":13.8},\"# metres to radius\":{\"kind\":\"metres\",\"value\":1.9},\"Cursed enemies have #% to Elemental Resistances\":{\"kind\":\"percent\",\"value\":-41},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}}},\"radius\":{\"kind\":\"metres\",\"value\":4.5}}");
            var curse = r.GetRolePayload(SkillGemTowerMap.RoleKind.Curse);
            Assert.AreEqual("Elemental_Weakness", r.Slug);
            Assert.AreEqual("curse", r.Category);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Curse, r.RoleKinds[0]);
            Assert.AreEqual(1, r.RoleKinds.Length);
            Assert.AreEqual(0f, r.Damage, 0.001f);
            Assert.AreEqual(GemTag.Spell | GemTag.Aoe | GemTag.Duration | GemTag.Curse | GemTag.Hex, r.Tags);
            Assert.AreEqual(new[] { 1, 10 }, r.SourceLevels);
            Assert.IsFalse(HasModifier(curse.Modifiers, RoleStat.CastTime));
            Assert.AreEqual(3f, FindModifier(curse.Modifiers, RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(3f, FindModifier(curse.Levels[0], RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(10.55f, FindModifier(curse.Levels[1], RoleStat.TowerRadius).Value, 0.001f);
            Assert.IsFalse(HasEffect(curse.Levels[0], RoleEffectKind.SkillDuration));
            Assert.IsFalse(HasModifier(curse.Levels[0], RoleStat.Damage));
            Assert.AreEqual(-27f, FindEffect(curse.Levels[0], RoleEffectKind.EnemyFireResistance).Value, 0.001f);
            Assert.AreEqual(-27f, FindEffect(curse.Levels[0], RoleEffectKind.EnemyColdResistance).Value, 0.001f);
            Assert.AreEqual(-27f, FindEffect(curse.Levels[0], RoleEffectKind.EnemyLightningResistance).Value, 0.001f);
            Assert.IsFalse(HasEffect(curse.Levels[0], RoleEffectKind.EnemyChaosResistance));
            Assert.AreEqual(-60f, FindEffect(curse.Levels[1], RoleEffectKind.EnemyFireResistance).Value, 0.001f);
            Assert.AreEqual(-60f, FindEffect(curse.Levels[1], RoleEffectKind.EnemyColdResistance).Value, 0.001f);
            Assert.AreEqual(-60f, FindEffect(curse.Levels[1], RoleEffectKind.EnemyLightningResistance).Value, 0.001f);
            SkillGemTowerMap.ResolveFireBehavior(
                r.Tags,
                r.Slug,
                SkillGemTowerMap.RoleKind.Curse,
                out var aim,
                out var delivery);
            Assert.AreEqual(AimMode.Direct, aim);
            Assert.AreEqual(DeliveryPattern.CasterNova, delivery);
        }

        [Test]
        public void CurseCatalog_Enfeeble_ScalesByFactor()
        {
            var enfeeble = SkillGemTowerMap.FromJson(
                "{\"name\":\"Enfeeble\",\"slug\":\"Enfeeble\",\"tags\":[\"Spell\",\"AoE\",\"Duration\",\"Curse\",\"Hex\"],\"category\":\"curse\",\"header\":{\"cast_time\":{\"kind\":\"seconds\",\"value\":0.5}},\"levels\":{\"1\":{\"Cursed enemies have #% reduced Accuracy Rating\":{\"kind\":\"percent\",\"value\":11},\"Cursed Normal or Magic enemies deal #% less Damage\":{\"kind\":\"percent\",\"value\":17},\"Cursed Rare or Unique enemies deal #% less Damage\":{\"kind\":\"percent\",\"value\":10}},\"10\":{\"Cursed enemies have #% reduced Accuracy Rating\":{\"kind\":\"percent\",\"value\":27},\"Cursed Normal or Magic enemies deal #% less Damage\":{\"kind\":\"percent\",\"value\":34},\"Cursed Rare or Unique enemies deal #% less Damage\":{\"kind\":\"percent\",\"value\":26}}},\"radius\":{\"kind\":\"metres\",\"value\":4.5}}");
            var enfeebleRole = enfeeble.GetRolePayload(SkillGemTowerMap.RoleKind.Curse);
            Assert.AreEqual(16f, FindEffect(enfeebleRole.Levels[0], RoleEffectKind.EnemyAccuracyRatingReduced).Value, 0.001f);
            Assert.AreEqual(26f, FindEffect(enfeebleRole.Levels[0], RoleEffectKind.EnemyOutgoingDamageLessNormal).Value, 0.001f);
            Assert.AreEqual(15f, FindEffect(enfeebleRole.Levels[0], RoleEffectKind.EnemyOutgoingDamageLessRare).Value, 0.001f);
            Assert.AreEqual(40f, FindEffect(enfeebleRole.Levels[1], RoleEffectKind.EnemyAccuracyRatingReduced).Value, 0.001f);
            Assert.AreEqual(50f, FindEffect(enfeebleRole.Levels[1], RoleEffectKind.EnemyOutgoingDamageLessNormal).Value, 0.001f);
            Assert.AreEqual(38f, FindEffect(enfeebleRole.Levels[1], RoleEffectKind.EnemyOutgoingDamageLessRare).Value, 0.001f);
        }

        [Test]
        public void CurseCatalog_Hexblast_StayHexOnlyWithoutDamage()
        {
            var hexblast = SkillGemTowerMap.FromJson(
                "{\"name\":\"Hexblast\",\"slug\":\"Hexblast\",\"tags\":[\"Spell\",\"AoE\",\"Chaos\",\"Hex\"],\"category\":\"curse\",\"header\":{\"cast_time\":{\"kind\":\"seconds\",\"value\":0.85}},\"levels\":{\"1\":{\"Deals # to # Chaos Damage\":{\"kind\":\"flat\",\"value\":[204,307]},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}},\"10\":{\"Deals # to # Chaos Damage\":{\"kind\":\"flat\",\"value\":[7916,11874]},\"damage_percent\":{\"kind\":\"percent\",\"value\":100}}},\"radius\":{\"kind\":\"metres\",\"value\":4.5}}");
            var hexblastRole = hexblast.GetRolePayload(SkillGemTowerMap.RoleKind.Curse);
            Assert.AreEqual(0f, hexblast.Damage, 0.001f);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Curse, hexblast.RoleKinds[0]);
            Assert.IsFalse(HasModifier(hexblastRole.Modifiers, RoleStat.CastTime));
            Assert.IsFalse(HasModifier(hexblastRole.Levels[0], RoleStat.Damage));
            Assert.AreEqual(0, hexblastRole.Levels[0].Effects.Length);
            Assert.AreEqual(3f, FindModifier(hexblastRole.Levels[0], RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(10.55f, FindModifier(hexblastRole.Levels[1], RoleStat.TowerRadius).Value, 0.001f);
            SkillGemTowerMap.ResolveFireBehavior(
                hexblast.Tags,
                hexblast.Slug,
                SkillGemTowerMap.RoleKind.Curse,
                out var aim,
                out var delivery);
            Assert.AreEqual(AimMode.Direct, aim);
            Assert.AreEqual(DeliveryPattern.CasterNova, delivery);
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
        public void TagMap_MapsLightningAndDuration()
        {
            var r = SkillGemTowerMap.FromJson(LightningDurationJson);
            Assert.IsTrue((r.Tags & GemTag.Spell) != 0);
            Assert.IsTrue((r.Tags & GemTag.Projectile) != 0);
            Assert.AreEqual(GemTag.Spell | GemTag.Projectile | GemTag.Lightning | GemTag.Duration, r.Tags);
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

        [Test]
        public void MoltenStrike_MapsAttackDamageMultiplyAndProjectileNoSplash()
        {
            var r = SkillGemTowerMap.FromJson(MoltenStrikeJson);
            var attack = r.GetRolePayload(SkillGemTowerMap.RoleKind.Attack);
            Assert.AreEqual("Molten Strike", r.DisplayName);
            Assert.AreEqual("Molten_Strike", r.Slug);
            Assert.AreEqual(20, r.Cost);
            Assert.AreEqual(10f, r.Damage, 0.001f);
            Assert.AreEqual(SkillGemTowerMap.RoleKind.Attack, r.RoleKinds[0]);
            Assert.AreEqual(1f, FindModifier(attack.Modifiers, RoleStat.AttackTime).Value, 0.001f);
            Assert.AreEqual(100f, FindModifier(attack.Modifiers, RoleStat.AttackSpeed).Value, 0.001f);
            Assert.AreEqual(3.5f, FindModifier(attack.Modifiers, RoleStat.TowerRadius).Value, 0.001f);
            Assert.AreEqual(10f, FindModifier(attack.Modifiers, RoleStat.Damage).Value, 0.001f);
            Assert.AreEqual(1f, FindModifier(attack.Modifiers, RoleStat.ProjectileSpeed).Value, 0.001f);
            Assert.IsTrue((r.Tags & GemTag.Attack) != 0);
            Assert.IsTrue((r.Tags & GemTag.Projectile) != 0);
            Assert.IsTrue((r.Tags & GemTag.Aoe) != 0);
            Assert.IsTrue((r.Tags & GemTag.Melee) != 0);
            Assert.AreEqual(GemTag.Attack | GemTag.Projectile | GemTag.Aoe | GemTag.Melee | GemTag.Strike | GemTag.Fire, r.Tags);
            Assert.AreEqual(3, attack.Levels.Length);
            Assert.AreEqual(1, attack.Levels[0].SourceLevel);
            Assert.AreEqual(5, attack.Levels[1].SourceLevel);
            Assert.AreEqual(10, attack.Levels[2].SourceLevel);
            Assert.AreEqual(
                RoleModifierOperation.Multiply,
                FindModifier(attack.Levels[0].Modifiers, RoleStat.Damage).Operation);
            Assert.AreEqual(1.46f, FindModifier(attack.Levels[0].Modifiers, RoleStat.Damage).Value, 0.001f);
            Assert.AreEqual(3.292f, FindModifier(attack.Levels[1].Modifiers, RoleStat.Damage).Value, 0.001f);
            Assert.AreEqual(7.003f, FindModifier(attack.Levels[2].Modifiers, RoleStat.Damage).Value, 0.001f);
            Assert.IsFalse(HasModifier(attack.Modifiers, RoleStat.ProjectileCount));
            Assert.AreEqual(1, attack.EffectPayloads.Length);
            var magma = attack.EffectPayloads[0];
            Assert.AreEqual(EffectPayloadTrigger.OnImpact, magma.Trigger);
            Assert.AreEqual(EffectPayloadAnchor.PrimaryTarget, magma.Anchor);
            Assert.AreEqual(EffectPayloadTravelPattern.Fountain, magma.TravelPattern);
            Assert.AreEqual(EffectPayloadScatterPattern.RandomRing, magma.ScatterPattern);
            Assert.AreEqual(EffectPayloadHitPolicy.PerImpact, magma.HitPolicy);
            Assert.AreEqual(GemTag.Aoe | GemTag.Projectile, magma.Tags);
            Assert.AreEqual(4, magma.Count);
            Assert.AreEqual(0.4f, magma.DamageMultiplier, 0.001f);
            Assert.AreEqual(1f, magma.AoeRadius, 0.001f);
            Assert.AreEqual(1.5f, magma.MinDistance, 0.001f);
            Assert.AreEqual(2.5f, magma.MaxDistance, 0.001f);
            SkillGemTowerMap.ResolveFireBehavior(r.Tags, out var aim, out var delivery);
            Assert.AreEqual(AimMode.Direct, aim);
            Assert.AreEqual(DeliveryPattern.WarpStrike, delivery);
            Assert.IsFalse(HasModifier(attack.Levels[0], RoleStat.SplashRadius));
            Assert.IsFalse(HasModifier(attack.Modifiers, RoleStat.SplashRadius));
            Assert.IsTrue(r.IsActiveCatalogCompatible);
        }

        [Test]
        public void Firestorm_MapsFallingRainPayload()
        {
            var spell = SkillGemTowerMap.FromJson(FirestormJson).GetRolePayload(SkillGemTowerMap.RoleKind.Spell);
            Assert.AreEqual(1, spell.EffectPayloads.Length);
            var rain = spell.EffectPayloads[0];
            Assert.AreEqual(EffectPayloadTrigger.AfterDelay, rain.Trigger);
            Assert.AreEqual(EffectPayloadAnchor.GroundTarget, rain.Anchor);
            Assert.AreEqual(EffectPayloadTravelPattern.FallFromSky, rain.TravelPattern);
            Assert.AreEqual(EffectPayloadScatterPattern.None, rain.ScatterPattern);
            Assert.AreEqual(EffectPayloadHitPolicy.PerImpact, rain.HitPolicy);
            Assert.AreEqual(GemTag.Aoe, rain.Tags);
            Assert.AreEqual(SkillGemTowerMap.FirestormImpactCount, rain.Count);
            Assert.AreEqual(1f, rain.DamageMultiplier, 0.001f);
            Assert.AreEqual(SkillGemTowerMap.FirestormExplosionRadius, rain.AoeRadius, 0.001f);
            Assert.AreEqual(0f, rain.MinDistance, 0.001f);
            Assert.AreEqual(SkillGemTowerMap.FirestormStormRadius, rain.MaxDistance, 0.001f);
            Assert.AreEqual(SkillGemTowerMap.FirestormDropHeight, rain.ArcHeight, 0.001f);
            Assert.AreEqual(0f, rain.DelaySeconds, 0.001f);
            Assert.AreEqual(SkillGemTowerMap.FirestormIntervalSeconds, rain.IntervalSeconds, 0.001f);
        }

        [Test]
        public void Earthquake_MapsSlamSplashAndAftershockPayload()
        {
            var r = SkillGemTowerMap.FromJson(EarthquakeJson);
            var attack = r.GetRolePayload(SkillGemTowerMap.RoleKind.Attack);
            Assert.AreEqual("Earthquake", r.DisplayName);
            Assert.AreEqual(SkillGemTowerMap.EarthquakeSlamRadius, FindModifier(attack.Modifiers, RoleStat.SplashRadius).Value, 0.001f);
            SkillGemTowerMap.ResolveFireBehavior(r.Tags, out var aim, out var delivery);
            Assert.AreEqual(AimMode.Ground, aim);
            Assert.AreEqual(DeliveryPattern.GroundPulse, delivery);
            Assert.AreEqual(1, attack.EffectPayloads.Length);
            var aftershock = attack.EffectPayloads[0];
            Assert.AreEqual(EffectPayloadTrigger.AfterDelay, aftershock.Trigger);
            Assert.AreEqual(EffectPayloadAnchor.GroundTarget, aftershock.Anchor);
            Assert.AreEqual(EffectPayloadTravelPattern.StationaryPulse, aftershock.TravelPattern);
            Assert.AreEqual(EffectPayloadScatterPattern.None, aftershock.ScatterPattern);
            Assert.AreEqual(GemTag.Aoe, aftershock.Tags);
            Assert.AreEqual(1, aftershock.Count);
            Assert.AreEqual(SkillGemTowerMap.EarthquakeAftershockDamageMultiplier, aftershock.DamageMultiplier, 0.001f);
            Assert.AreEqual(SkillGemTowerMap.EarthquakeAftershockRadius, aftershock.AoeRadius, 0.001f);
            Assert.AreEqual(SkillGemTowerMap.EarthquakeAftershockDelaySeconds, aftershock.DelaySeconds, 0.001f);
        }

        [Test]
        public void ResolveFireBehavior_CleaveSlug_IsWarpStrike()
        {
            SkillGemTowerMap.ResolveFireBehavior(
                GemTag.Attack | GemTag.Melee | GemTag.Aoe,
                "Cleave",
                out var aim,
                out var delivery);
            Assert.AreEqual(AimMode.Direct, aim);
            Assert.AreEqual(DeliveryPattern.WarpStrike, delivery);
        }

        [Test]
        public void SplitArrow_MapsArrowCountBowTagAndSpread()
        {
            var r = SkillGemTowerMap.FromJson(SplitArrowJson);
            var attack = r.GetRolePayload(SkillGemTowerMap.RoleKind.Attack);
            Assert.AreEqual("Fires multiple arrows at different targets.", r.Description);
            Assert.IsTrue((r.Tags & GemTag.Bow) != 0);
            Assert.AreEqual(6f, FindModifier(attack.Levels[0], RoleStat.ProjectileCount).Value, 0.001f);
            Assert.AreEqual(13f, FindModifier(attack.Levels[1], RoleStat.ProjectileCount).Value, 0.001f);
            Assert.AreEqual(SkillGemTowerMap.SplitArrowSpreadDegrees, SkillGemTowerMap.ResolveSpreadDegrees(r.Slug), 0.001f);
            Assert.AreEqual(0f, SkillGemTowerMap.ResolveSequentialIntervalSeconds(r.Slug), 0.001f);
            SkillGemTowerMap.ResolveFireBehavior(r.Tags, r.Slug, out var aim, out var delivery);
            Assert.AreEqual(AimMode.Direct, aim);
            Assert.AreEqual(DeliveryPattern.Straight, delivery);
        }

        [Test]
        public void Barrage_MapsSixSequentialProjectiles()
        {
            var r = SkillGemTowerMap.FromJson(BarrageJson);
            var attack = r.GetRolePayload(SkillGemTowerMap.RoleKind.Attack);
            Assert.IsTrue((r.Tags & GemTag.Bow) != 0);
            Assert.AreEqual(
                SkillGemTowerMap.BarrageProjectileCount,
                FindModifier(attack.Modifiers, RoleStat.ProjectileCount).Value,
                0.001f);
            Assert.AreEqual(
                SkillGemTowerMap.BarrageSpreadDegrees,
                SkillGemTowerMap.ResolveSpreadDegrees(r.Slug),
                0.001f);
            Assert.AreEqual(
                SkillGemTowerMap.BarrageSequentialIntervalSeconds,
                SkillGemTowerMap.ResolveSequentialIntervalSeconds(r.Slug),
                0.001f);
        }

        [Test]
        public void IceShot_MapsClassifiedSplashAndColdBowTags()
        {
            var r = SkillGemTowerMap.FromJson(IceShotJson);
            var attack = r.GetRolePayload(SkillGemTowerMap.RoleKind.Attack);
            Assert.IsTrue((r.Tags & GemTag.Cold) != 0);
            Assert.IsTrue((r.Tags & GemTag.Bow) != 0);
            Assert.AreEqual(
                SkillGemTowerMap.IceShotSplashRadius,
                FindModifier(attack.Modifiers, RoleStat.SplashRadius).Value,
                0.001f);
        }

        [Test]
        public void CobraLash_MapsChainCountLikeArc()
        {
            var r = SkillGemTowerMap.FromJson(CobraLashJson);
            var attack = r.GetRolePayload(SkillGemTowerMap.RoleKind.Attack);
            Assert.IsTrue((r.Tags & GemTag.Chaos) != 0);
            Assert.AreEqual(3f, FindModifier(attack.Levels[0], RoleStat.ChainCount).Value, 0.001f);
            Assert.AreEqual(7f, FindModifier(attack.Levels[1], RoleStat.ChainCount).Value, 0.001f);
        }

        [Test]
        public void Fireball_KeepsFireTag()
        {
            var r = SkillGemTowerMap.FromJson(FireballJson);
            Assert.IsTrue((r.Tags & GemTag.Fire) != 0);
        }

        [Test]
        public void ResolveFireBehavior_Slam_IsGroundPulse()
        {
            SkillGemTowerMap.ResolveFireBehavior(
                GemTag.Attack | GemTag.Melee | GemTag.Slam | GemTag.Aoe,
                out var aim,
                out var delivery);
            Assert.AreEqual(AimMode.Ground, aim);
            Assert.AreEqual(DeliveryPattern.GroundPulse, delivery);
        }

        [Test]
        public void ResolveFireBehavior_MeleeStrike_IsWarpStrike()
        {
            SkillGemTowerMap.ResolveFireBehavior(
                GemTag.Attack | GemTag.Melee | GemTag.Strike,
                out var aim,
                out var delivery);
            Assert.AreEqual(AimMode.Direct, aim);
            Assert.AreEqual(DeliveryPattern.WarpStrike, delivery);
        }

        static bool HasModifier(RoleStatModifier[] modifiers, RoleStat stat)
        {
            for (var i = 0; i < modifiers.Length; i++)
            {
                if (modifiers[i].Stat == stat)
                    return true;
            }

            return false;
        }

        static RoleStatModifier FindModifier(RoleLevelDefinition level, RoleStat stat)
        {
            return FindModifier(level.Modifiers, stat);
        }

        static bool HasEffect(RoleLevelDefinition level, RoleEffectKind kind)
        {
            if (level.Effects == null)
                return false;
            for (var i = 0; i < level.Effects.Length; i++)
            {
                if (level.Effects[i].Kind == kind)
                    return true;
            }

            return false;
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
