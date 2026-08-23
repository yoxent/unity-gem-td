using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class TowerRoleFireIntervalTests
    {
        [Test]
        public void AttackRole_UsesAttackTimeAndSpeed()
        {
            var role = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            role.AttackTime = 1f;
            role.AttackSpeed = 80f;
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            def.Roles = new TowerRoleDefinition[] { role };

            Assert.AreEqual(1.25f, def.BaseFireInterval, 0.001f);
            Assert.IsTrue(def.UsesAttackSpeed);

            var spec = AttackSpec.FromBase(10f);
            spec.AttackSpeedMultiplier = 2f;
            Assert.AreEqual(0.625f, def.FireInterval(spec), 0.001f);
        }

        [Test]
        public void SpellRole_IgnoresFasterAttacks()
        {
            var role = ScriptableObject.CreateInstance<SpellRoleDefinition>();
            role.CastTime = 0.75f;
            role.CastSpeed = 100f;
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            def.Roles = new TowerRoleDefinition[] { role };

            var spec = AttackSpec.FromBase(10f);
            spec.AttackSpeedMultiplier = 2f;
            Assert.AreEqual(0.75f, def.FireInterval(spec), 0.001f);

            spec.CastSpeedMultiplier = 2f;
            Assert.AreEqual(0.375f, def.FireInterval(spec), 0.001f);
        }

        [Test]
        public void UnsetRole_HasNoFireInterval()
        {
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            Assert.AreEqual(0f, def.BaseFireInterval, 0.001f);
            Assert.IsFalse(def.IsFireable);
        }

        [Test]
        public void AttackPlusAura_FiresAsAttack()
        {
            var attack = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            attack.AttackTime = 0.5f;
            attack.AttackSpeed = 100f;
            var aura = ScriptableObject.CreateInstance<AuraRoleDefinition>();
            aura.TowerRadius = 1.5f;
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            def.Roles = new TowerRoleDefinition[] { aura, attack };

            Assert.IsTrue(def.HasRole<AttackRoleDefinition>());
            Assert.IsTrue(def.HasRole<AuraRoleDefinition>());
            Assert.IsInstanceOf<AttackRoleDefinition>(def.FireRole);
            Assert.AreEqual(0.5f, def.BaseFireInterval, 0.001f);
            Assert.IsTrue(def.UsesAttackSpeed);
        }
    }
}
