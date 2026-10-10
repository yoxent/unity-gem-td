using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class DamageMixPipelineTests
    {
        [Test]
        public void ResolveBaseline_CopiesMixFractions()
        {
            var role = CreateFiringSpellRole();
            role.Mix = new[]
            {
                new DamageTypeShare { Type = DamageType.Physical, Percent = 60 },
                new DamageTypeShare { Type = DamageType.Fire, Percent = 40 }
            };
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            def.Roles = new TowerRoleDefinition[] { role };
            def.Damage = 10f;
            var tower = new TowerInstance(default, def);
            var spec = new GemModifierPipeline().ResolveBaseline(tower);
            Assert.AreEqual(0.6f, spec.MixPhysical, 0.0001f);
            Assert.AreEqual(0.4f, spec.MixFire, 0.0001f);
            Object.DestroyImmediate(role);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void ResolveBaseline_EmptyMix_IsUntyped()
        {
            var role = CreateFiringSpellRole();
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            def.Roles = new TowerRoleDefinition[] { role };
            var tower = new TowerInstance(default, def);
            var spec = new GemModifierPipeline().ResolveBaseline(tower);
            Assert.AreEqual(0f, spec.MixPhysical);
            Assert.AreEqual(0f, spec.MixFire);
            Object.DestroyImmediate(role);
            Object.DestroyImmediate(def);
        }

        static SpellRoleDefinition CreateFiringSpellRole()
        {
            var role = ScriptableObject.CreateInstance<SpellRoleDefinition>();
            role.Modifiers = new[]
            {
                RoleStatModifier.Single(RoleStat.CastTime, RoleModifierOperation.Set, 0.75f),
                RoleStatModifier.Single(RoleStat.CastSpeed, RoleModifierOperation.Set, 100f)
            };
            return role;
        }
    }
}
