using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class RoleRangeAndSplashTests
    {
        [Test]
        public void AttackRole_ProvidesFireAndPlacementRange()
        {
            var role = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            var def = CreateDefinition(role);
            try
            {
                role.TowerRadius = 12f;

                Assert.AreEqual(12f, def.GetFireTowerRadius(20), 0.001f);
                Assert.AreEqual(12f, def.GetPlacementTowerRadius(20), 0.001f);
            }
            finally
            {
                Destroy(def, role);
            }
        }

        [Test]
        public void SpellRole_ProvidesFireAndPlacementRange()
        {
            var role = ScriptableObject.CreateInstance<SpellRoleDefinition>();
            var def = CreateDefinition(role);
            try
            {
                role.TowerRadius = 18f;

                Assert.AreEqual(18f, def.GetFireTowerRadius(20), 0.001f);
                Assert.AreEqual(18f, def.GetPlacementTowerRadius(20), 0.001f);
            }
            finally
            {
                Destroy(def, role);
            }
        }

        [Test]
        public void AuraRole_ProvidesInfluenceAndPlacementRange()
        {
            var role = ScriptableObject.CreateInstance<AuraRoleDefinition>();
            var def = CreateDefinition(role);
            try
            {
                role.TowerRadius = 3f;

                Assert.AreEqual(3f, def.GetAuraTowerRadius(20), 0.001f);
                Assert.AreEqual(3f, def.GetPlacementTowerRadius(20), 0.001f);
                Assert.AreEqual(0f, def.GetFireTowerRadius(20), 0.001f);
            }
            finally
            {
                Destroy(def, role);
            }
        }

        [Test]
        public void Hybrid_UsesAttackRangeForFireAndAuraRangeForInfluence()
        {
            var attack = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            var aura = ScriptableObject.CreateInstance<AuraRoleDefinition>();
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            def.Roles = new TowerRoleDefinition[] { aura, attack };
            try
            {
                attack.TowerRadius = 11f;
                aura.TowerRadius = 3f;

                Assert.AreEqual(11f, def.GetFireTowerRadius(20), 0.001f);
                Assert.AreEqual(3f, def.GetAuraTowerRadius(20), 0.001f);
                Assert.AreEqual(11f, def.GetPlacementTowerRadius(20), 0.001f);
            }
            finally
            {
                Destroy(def, attack, aura);
            }
        }

        [Test]
        public void DamageRole_SplashIsIndependentOfTowerRadius()
        {
            var role = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            var def = CreateDefinition(role);
            try
            {
                role.TowerRadius = 20f;
                role.Modifiers = new[]
                {
                    Modifier(RoleStat.SplashRadius, RoleModifierOperation.Set, 0.5f)
                };

                Assert.AreEqual(20f, def.GetFireTowerRadius(20), 0.001f);
                Assert.AreEqual(0.5f, def.GetSplashRadius(20), 0.001f);
            }
            finally
            {
                Destroy(def, role);
            }
        }

        [Test]
        public void NonAoeDamageRole_DefaultsToZeroSplash()
        {
            var role = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            var def = CreateDefinition(role);
            try
            {
                def.Tags = GemTag.Attack | GemTag.Projectile;

                Assert.AreEqual(0f, def.GetSplashRadius(20), 0.001f);
            }
            finally
            {
                Destroy(def, role);
            }
        }

        [Test]
        public void AoeTagWithoutSplashModifier_RemainsZero()
        {
            var role = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            var def = CreateDefinition(role);
            try
            {
                def.Tags = GemTag.Attack | GemTag.Projectile | GemTag.Aoe;

                Assert.AreEqual(0f, def.GetSplashRadius(20), 0.001f);
            }
            finally
            {
                Destroy(def, role);
            }
        }

        [Test]
        public void AuthoredLevelSplashModifier_ResolvesExplicitValue()
        {
            var role = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            var def = CreateDefinition(role);
            try
            {
                def.Tags = GemTag.Attack | GemTag.Projectile | GemTag.Aoe;
                role.Levels = new[]
                {
                    new RoleLevelDefinition
                    {
                        SourceLevel = 20,
                        Modifiers = new[]
                        {
                            new RoleStatModifier
                            {
                                Stat = RoleStat.SplashRadius,
                                Operation = RoleModifierOperation.Set,
                                Value = 2f
                            }
                        }
                    }
                };

                var tower = new TowerInstance(Vector2Int.zero, def);
                tower.SetLevel(20);

                Assert.AreEqual(2f, def.GetSplashRadius(tower.Level), 0.001f);
            }
            finally
            {
                Destroy(def, role);
            }
        }

        [Test]
        public void RoleCadence_UsesResolvedLevelTimingAndActionsPerSecond()
        {
            var role = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            var def = CreateDefinition(role);
            try
            {
                role.AttackTime = 2f;
                role.AttackSpeed = 100f;
                role.Levels = new[]
                {
                    new RoleLevelDefinition
                    {
                        SourceLevel = 20,
                        Modifiers = new[]
                        {
                            new RoleStatModifier
                            {
                                Stat = RoleStat.AttackTime,
                                Operation = RoleModifierOperation.Set,
                                Value = 1f
                            }
                        }
                    }
                };

                Assert.AreEqual(1f, def.GetBaseFireInterval(20), 0.001f);
                Assert.AreEqual(1f, def.GetBaseActionsPerSecond(20), 0.001f);
            }
            finally
            {
                Destroy(def, role);
            }
        }

        [Test]
        public void AuraRole_HasNoFireCadence()
        {
            var role = ScriptableObject.CreateInstance<AuraRoleDefinition>();
            var def = CreateDefinition(role);
            try
            {
                Assert.AreEqual(0f, def.GetBaseFireInterval(20), 0.001f);
                Assert.AreEqual(0f, def.GetBaseActionsPerSecond(20), 0.001f);
                Assert.IsFalse(def.IsFireable);
            }
            finally
            {
                Destroy(def, role);
            }
        }

        static RoleStatModifier Modifier(RoleStat stat, RoleModifierOperation operation, float value)
        {
            return new RoleStatModifier
            {
                Stat = stat,
                Operation = operation,
                Value = value
            };
        }

        static TowerDefinition CreateDefinition(TowerRoleDefinition role)
        {
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            def.Roles = new[] { role };
            return def;
        }

        static void Destroy(TowerDefinition def, params Object[] roles)
        {
            Object.DestroyImmediate(def);
            for (var i = 0; i < roles.Length; i++)
                Object.DestroyImmediate(roles[i]);
        }
    }
}
