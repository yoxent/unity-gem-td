using System;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class RoleStatResolverTests
    {
        AttackRoleDefinition _role;

        [SetUp]
        public void SetUp()
        {
            _role = ScriptableObject.CreateInstance<AttackRoleDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_role);
        }

        [Test]
        public void ResolveStat_AppliesLevelSetThenAddThenMultiply()
        {
            _role.Modifiers = new[]
            {
                Modifier(RoleStat.AttackTime, RoleModifierOperation.Set, 10f)
            };
            _role.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 10,
                    Modifiers = new[]
                    {
                        Modifier(RoleStat.AttackTime, RoleModifierOperation.Set, 8f),
                        Modifier(RoleStat.AttackTime, RoleModifierOperation.Add, 2f),
                        Modifier(RoleStat.AttackTime, RoleModifierOperation.Multiply, 0.5f)
                    }
                }
            };
            Assert.AreEqual(5f, _role.ResolveStat(RoleStat.AttackTime, 10), 0.001f);
        }

        [Test]
        public void ResolveStat_IgnoresModifiersForAnotherStat()
        {
            _role.Modifiers = new[]
            {
                Modifier(RoleStat.AttackTime, RoleModifierOperation.Set, 10f),
                Modifier(RoleStat.AttackSpeed, RoleModifierOperation.Set, 20f)
            };

            Assert.AreEqual(10f, _role.ResolveStat(RoleStat.AttackTime, 10), 0.001f);
        }

        [Test]
        public void ResolveStat_UsesExactLevel()
        {
            AddLevels(1, 5, 10);

            Assert.AreEqual(5f, _role.ResolveStat(RoleStat.AttackTime, 5), 0.001f);
        }

        [Test]
        public void ResolveStat_UsesGreatestLevelBelowRequest()
        {
            AddLevels(1, 5, 10);

            Assert.AreEqual(5f, _role.ResolveStat(RoleStat.AttackTime, 8), 0.001f);
        }

        [Test]
        public void ResolveStat_ClampsRequestBelowFirstAndAboveLast()
        {
            AddLevels(1, 5, 10);

            Assert.AreEqual(1f, _role.ResolveStat(RoleStat.AttackTime, 1), 0.001f);
            Assert.AreEqual(1f, _role.ResolveStat(RoleStat.AttackTime, 0), 0.001f);
            Assert.AreEqual(10f, _role.ResolveStat(RoleStat.AttackTime, 99), 0.001f);
        }

        [Test]
        public void ResolveStat_ClampsInvalidTimingAndSpeed()
        {
            _role.Modifiers = new[]
            {
                Modifier(RoleStat.AttackTime, RoleModifierOperation.Set, -1f),
                Modifier(RoleStat.AttackSpeed, RoleModifierOperation.Set, 0f)
            };

            Assert.AreEqual(0f, _role.ResolveStat(RoleStat.AttackTime, 10), 0.001f);
            Assert.AreEqual(0.01f, _role.ResolveStat(RoleStat.AttackSpeed, 10), 0.001f);
        }

        [Test]
        public void ResolveStat_ClampsReservationToPercentRange()
        {
            var aura = ScriptableObject.CreateInstance<AuraRoleDefinition>();
            try
            {
                aura.Modifiers = new[]
                {
                    Modifier(RoleStat.ReservationPercent, RoleModifierOperation.Set, 125f),
                    Modifier(RoleStat.ReservationPercent, RoleModifierOperation.Add, 10f)
                };

                Assert.AreEqual(100f, aura.ResolveStat(RoleStat.ReservationPercent, 10), 0.001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(aura);
            }
        }

        [Test]
        public void ResolveStatValue_RangeSetThenSingleMultiply_ScalesBothEnds()
        {
            _role.Modifiers = new[]
            {
                RoleStatModifier.Range(RoleStat.Damage, RoleModifierOperation.Set, 5f, 10f),
                RoleStatModifier.Single(RoleStat.Damage, RoleModifierOperation.Multiply, 2f)
            };

            var value = _role.ResolveStatValue(RoleStat.Damage, 10);
            Assert.AreEqual(10f, value.Min, 0.001f);
            Assert.AreEqual(20f, value.Max, 0.001f);
            Assert.AreEqual(15f, _role.ResolveStat(RoleStat.Damage, 10), 0.001f);
        }

        [Test]
        public void ResolveStatValue_RangeAddUsesEachEnd()
        {
            _role.Modifiers = new[]
            {
                RoleStatModifier.Range(RoleStat.Damage, RoleModifierOperation.Set, 5f, 10f),
                RoleStatModifier.Range(RoleStat.Damage, RoleModifierOperation.Add, 1f, 3f)
            };

            var value = _role.ResolveStatValue(RoleStat.Damage, 10);
            Assert.AreEqual(6f, value.Min, 0.001f);
            Assert.AreEqual(13f, value.Max, 0.001f);
        }

        [Test]
        public void SampleHitDamage_InclusiveIntegerWhenRangeSpansInts()
        {
            Assert.AreEqual(8f, RoleStatValue.SampleHitDamage(8f, 8f), 0.001f);
            for (var i = 0; i < 40; i++)
            {
                var rolled = RoleStatValue.SampleHitDamage(5f, 10f);
                Assert.GreaterOrEqual(rolled, 5f);
                Assert.LessOrEqual(rolled, 10f);
            }
        }

        [Test]
        public void FireballSnapshots_ResolveFlatDamageSplashCadenceAndProjectileSpeed()
        {
            var spell = ScriptableObject.CreateInstance<SpellRoleDefinition>();
            try
            {
                spell.Modifiers = new[]
                {
                    RoleStatModifier.Single(RoleStat.CastTime, RoleModifierOperation.Set, 0.75f),
                    RoleStatModifier.Single(RoleStat.CastSpeed, RoleModifierOperation.Set, 100f),
                    RoleStatModifier.Single(RoleStat.ProjectileSpeed, RoleModifierOperation.Set, 1f)
                };
                spell.Levels = new[]
                {
                    FireballLevel(1, 19f, 28f, 1.1f),
                    FireballLevel(5, 1883f, 2825f, 1.8f),
                    FireballLevel(10, 11041f, 16562f, 2.4f)
                };

                var level1 = spell.ResolveStatValue(RoleStat.Damage, 1);
                var level5 = spell.ResolveStatValue(RoleStat.Damage, 5);
                var level10 = spell.ResolveStatValue(RoleStat.Damage, 10);
                Assert.AreEqual(19f, level1.Min, 0.001f);
                Assert.AreEqual(28f, level1.Max, 0.001f);
                Assert.AreEqual(1883f, level5.Min, 0.001f);
                Assert.AreEqual(2825f, level5.Max, 0.001f);
                Assert.AreEqual(11041f, level10.Min, 0.001f);
                Assert.AreEqual(16562f, level10.Max, 0.001f);
                Assert.AreEqual(1.1f, spell.ResolveStat(RoleStat.SplashRadius, 1), 0.001f);
                Assert.AreEqual(2.4f, spell.ResolveStat(RoleStat.SplashRadius, 10), 0.001f);
                Assert.AreEqual(0.75f, spell.GetBaseFireInterval(1), 0.001f);
                Assert.AreEqual(1f, spell.ResolveStat(RoleStat.ProjectileSpeed, 1), 0.001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(spell);
            }
        }

        [Test]
        public void GetChainCount_ReturnsLevelSetWholeNumber()
        {
            var spell = ScriptableObject.CreateInstance<SpellRoleDefinition>();
            try
            {
                spell.Levels = new[]
                {
                    new RoleLevelDefinition
                    {
                        SourceLevel = 1,
                        Modifiers = new[]
                        {
                            Modifier(RoleStat.ChainCount, RoleModifierOperation.Set, 4f)
                        }
                    },
                    new RoleLevelDefinition
                    {
                        SourceLevel = 10,
                        Modifiers = new[]
                        {
                            Modifier(RoleStat.ChainCount, RoleModifierOperation.Set, 11f)
                        }
                    }
                };

                Assert.AreEqual(4, spell.GetChainCount(1));
                Assert.AreEqual(11, spell.GetChainCount(10));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(spell);
            }
        }

        [Test]
        public void ResolveEffect_AppliesConstantThenLevelScaling()
        {
            var aura = ScriptableObject.CreateInstance<AuraRoleDefinition>();
            try
            {
                aura.Effects = new[]
                {
                    RoleEffectModifier.Single(
                        RoleEffectKind.AllyOutgoingDamageMultiplier,
                        RoleModifierOperation.Set,
                        1.2f)
                };
                aura.Levels = new[]
                {
                    new RoleLevelDefinition
                    {
                        SourceLevel = 1,
                        Modifiers = Array.Empty<RoleStatModifier>(),
                        Effects = Array.Empty<RoleEffectModifier>()
                    },
                    new RoleLevelDefinition
                    {
                        SourceLevel = 5,
                        Modifiers = Array.Empty<RoleStatModifier>(),
                        Effects = new[]
                        {
                            RoleEffectModifier.Single(
                                RoleEffectKind.AllyOutgoingDamageMultiplier,
                                RoleModifierOperation.Add,
                                0.3f)
                        }
                    }
                };

                Assert.AreEqual(
                    1.2f,
                    aura.ResolveEffect(RoleEffectKind.AllyOutgoingDamageMultiplier, 1),
                    0.001f);
                Assert.AreEqual(
                    1.5f,
                    aura.ResolveEffect(RoleEffectKind.AllyOutgoingDamageMultiplier, 5),
                    0.001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(aura);
            }
        }

        [Test]
        public void ResolveEffect_AngerFireAndFrostbiteResistUseLevelSnapshots()
        {
            var aura = ScriptableObject.CreateInstance<AuraRoleDefinition>();
            var curse = ScriptableObject.CreateInstance<CurseRoleDefinition>();
            try
            {
                aura.Levels = new[]
                {
                    new RoleLevelDefinition
                    {
                        SourceLevel = 1,
                        Effects = new[]
                        {
                            RoleEffectModifier.Range(
                                RoleEffectKind.AllyAddedAttackFireDamage,
                                RoleModifierOperation.Set,
                                25f,
                                36f)
                        }
                    }
                };
                curse.Levels = new[]
                {
                    new RoleLevelDefinition
                    {
                        SourceLevel = 1,
                        Effects = new[]
                        {
                            RoleEffectModifier.Single(
                                RoleEffectKind.EnemyColdResistance,
                                RoleModifierOperation.Set,
                                -20f)
                        }
                    }
                };

                var fire = aura.ResolveEffectValue(RoleEffectKind.AllyAddedAttackFireDamage, 1);
                Assert.AreEqual(25f, fire.Min, 0.001f);
                Assert.AreEqual(36f, fire.Max, 0.001f);
                Assert.AreEqual(
                    -20f,
                    curse.ResolveEffect(RoleEffectKind.EnemyColdResistance, 1),
                    0.001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(aura);
                UnityEngine.Object.DestroyImmediate(curse);
            }
        }

        static RoleLevelDefinition FireballLevel(
            int sourceLevel,
            float damageMin,
            float damageMax,
            float splashRadius)
        {
            return new RoleLevelDefinition
            {
                SourceLevel = sourceLevel,
                Modifiers = new[]
                {
                    RoleStatModifier.Single(RoleStat.SplashRadius, RoleModifierOperation.Set, splashRadius),
                    RoleStatModifier.Range(RoleStat.Damage, RoleModifierOperation.Set, damageMin, damageMax)
                }
            };
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

        void AddLevels(int first, int second, int third)
        {
            _role.Levels = new[]
            {
                Level(first),
                Level(second),
                Level(third)
            };
        }

        static RoleLevelDefinition Level(int sourceLevel)
        {
            return new RoleLevelDefinition
            {
                SourceLevel = sourceLevel,
                Modifiers = new[]
                {
                    Modifier(RoleStat.AttackTime, RoleModifierOperation.Set, sourceLevel)
                }
            };
        }
    }
}
