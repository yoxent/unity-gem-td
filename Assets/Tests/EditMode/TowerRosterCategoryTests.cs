using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class TowerRosterCategoryTests
    {
        [Test]
        public void Null_IsDamaging()
        {
            Assert.AreEqual(TowerRosterCategory.Damaging, TowerRosterCategoryRules.Of(null));
        }

        [Test]
        public void NoRoles_IsDamaging()
        {
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            Assert.AreEqual(TowerRosterCategory.Damaging, TowerRosterCategoryRules.Of(def));
            Object.DestroyImmediate(def);
        }

        [Test]
        public void Attack_IsDamaging()
        {
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            var role = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            def.Roles = new TowerRoleDefinition[] { role };
            Assert.AreEqual(TowerRosterCategory.Damaging, TowerRosterCategoryRules.Of(def));
            Object.DestroyImmediate(role);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void Spell_IsDamaging()
        {
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            var role = ScriptableObject.CreateInstance<SpellRoleDefinition>();
            def.Roles = new TowerRoleDefinition[] { role };
            Assert.AreEqual(TowerRosterCategory.Damaging, TowerRosterCategoryRules.Of(def));
            Object.DestroyImmediate(role);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void CurseRole_IsCurse_NotDamaging()
        {
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            var role = ScriptableObject.CreateInstance<CurseRoleDefinition>();
            def.Roles = new TowerRoleDefinition[] { role };
            Assert.AreEqual(TowerRosterCategory.Curse, TowerRosterCategoryRules.Of(def));
            Object.DestroyImmediate(role);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void AuraOnly_IsAura()
        {
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            var role = ScriptableObject.CreateInstance<AuraRoleDefinition>();
            def.Roles = new TowerRoleDefinition[] { role };
            Assert.AreEqual(TowerRosterCategory.Aura, TowerRosterCategoryRules.Of(def));
            Object.DestroyImmediate(role);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void AttackPlusAura_IsDamaging()
        {
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            var attack = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            var aura = ScriptableObject.CreateInstance<AuraRoleDefinition>();
            def.Roles = new TowerRoleDefinition[] { attack, aura };
            Assert.AreEqual(TowerRosterCategory.Damaging, TowerRosterCategoryRules.Of(def));
            Object.DestroyImmediate(attack);
            Object.DestroyImmediate(aura);
            Object.DestroyImmediate(def);
        }
    }
}
