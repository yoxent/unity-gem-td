using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GemTD.Tests.EditMode
{
    /// <summary>
    /// Reads the real catalog assets. A hand-edited YAML block can deserialize as
    /// empty without any error, so these assert the shipped data rather than a
    /// fixture built in code.
    /// </summary>
    public sealed class CritGemAssetTests
    {
        const string StrikesPath =
            "Assets/Data/Gems/Catalog/Gem_IncreasedCriticalStrikes.asset";
        const string DamagePath =
            "Assets/Data/Gems/Catalog/Gem_IncreasedCriticalDamage.asset";

        [Test]
        public void IncreasedCriticalStrikes_CarriesChanceModifier()
        {
            var gem = AssetDatabase.LoadAssetAtPath<GemDefinition>(StrikesPath);
            Assert.IsNotNull(gem, "Missing asset: " + StrikesPath);
            Assert.AreEqual(GemCategory.Offensive, gem.Category);
            Assert.AreEqual(1, gem.Modifiers.Length, "Modifiers did not deserialize.");

            var mod = gem.Modifiers[0];
            Assert.AreEqual(GemStat.CritChance, mod.Stat);
            Assert.AreEqual(RoleModifierOperation.Multiply, mod.Operation);
            Assert.AreEqual(1.93f, mod.Resolve(GemRarity.Lesser).Value, 1e-3f);
            Assert.AreEqual(2.17f, mod.Resolve(GemRarity.Normal).Value, 1e-3f);
            Assert.AreEqual(2.41f, mod.Resolve(GemRarity.Greater).Value, 1e-3f);
        }

        [Test]
        public void IncreasedCriticalDamage_CarriesMultiplierModifier()
        {
            var gem = AssetDatabase.LoadAssetAtPath<GemDefinition>(DamagePath);
            Assert.IsNotNull(gem, "Missing asset: " + DamagePath);
            Assert.AreEqual(GemCategory.Offensive, gem.Category);
            Assert.AreEqual(1, gem.Modifiers.Length, "Modifiers did not deserialize.");

            var mod = gem.Modifiers[0];
            Assert.AreEqual(GemStat.CritMultiplier, mod.Stat);
            Assert.AreEqual(RoleModifierOperation.Add, mod.Operation);
            Assert.AreEqual(1.22f, mod.Resolve(GemRarity.Lesser).Value, 1e-3f);
            Assert.AreEqual(1.38f, mod.Resolve(GemRarity.Normal).Value, 1e-3f);
            Assert.AreEqual(1.54f, mod.Resolve(GemRarity.Greater).Value, 1e-3f);
        }

        [Test]
        public void CritGems_SocketOnAFiringTower_AndChangeItsSpec()
        {
            var tower = ScriptableObject.CreateInstance<TowerDefinition>();
            tower.Roles = new TowerRoleDefinition[]
            {
                ScriptableObject.CreateInstance<AttackRoleDefinition>(),
            };
            tower.Tags = GemTag.Attack | GemTag.Projectile | GemTag.Critical;
            tower.SocketCount = 2;

            var strikes = AssetDatabase.LoadAssetAtPath<GemDefinition>(StrikesPath);
            var damage = AssetDatabase.LoadAssetAtPath<GemDefinition>(DamagePath);
            var instance = new TowerInstance(Vector2Int.zero, tower);
            try
            {
                Assert.IsTrue(GemTags.CanSocket(tower, strikes), "Crit chance gem must fit a crit tower.");
                Assert.IsTrue(instance.TrySocket(strikes, 0, allowSocket: true));
                Assert.IsTrue(instance.TrySocket(damage, 1, allowSocket: true));

                var pipeline = new GemModifierPipeline();
                var spec = pipeline.Resolve(instance, new System.Collections.Generic.List<ISkillModifier>());

                Assert.AreEqual(0.05f * 2.17f, spec.CritChance, 1e-3f);
                Assert.AreEqual(1.5f + 1.38f, spec.CritMultiplier, 1e-3f);
            }
            finally
            {
                Object.DestroyImmediate(tower);
            }
        }

        [Test]
        public void CritGems_DoNotFitACurseOrAuraTower()
        {
            var strikes = AssetDatabase.LoadAssetAtPath<GemDefinition>(StrikesPath);
            var curse = ScriptableObject.CreateInstance<TowerDefinition>();
            curse.Roles = new TowerRoleDefinition[]
            {
                ScriptableObject.CreateInstance<CurseRoleDefinition>(),
            };
            curse.Tags = GemTag.Curse | GemTag.Aoe;
            var aura = ScriptableObject.CreateInstance<TowerDefinition>();
            aura.Roles = new TowerRoleDefinition[]
            {
                ScriptableObject.CreateInstance<AuraRoleDefinition>(),
            };
            aura.Tags = GemTag.Aura;
            try
            {
                Assert.IsFalse(
                    GemTags.CanSocket(curse, strikes),
                    "A curse tower cannot crit, so a Critical gem must not fit.");
                Assert.IsFalse(
                    GemTags.CanSocket(aura, strikes),
                    "An aura-only tower cannot crit, so a Critical gem must not fit.");
            }
            finally
            {
                Object.DestroyImmediate(curse);
                Object.DestroyImmediate(aura);
            }
        }
    }
}
