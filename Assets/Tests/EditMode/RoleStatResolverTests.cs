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
            Object.DestroyImmediate(_role);
        }

        [Test]
        public void ResolveStat_AppliesLevelSetThenAddThenMultiply()
        {
            _role.AttackTime = 10f;
            _role.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 20,
                    Modifiers = new[]
                    {
                        Modifier(RoleStat.AttackTime, RoleModifierOperation.Set, 8f)
                    }
                }
            };
            _role.Modifiers = new[]
            {
                Modifier(RoleStat.AttackTime, RoleModifierOperation.Add, 2f),
                Modifier(RoleStat.AttackTime, RoleModifierOperation.Multiply, 0.5f)
            };

            Assert.AreEqual(5f, _role.ResolveStat(RoleStat.AttackTime, 20), 0.001f);
        }

        [Test]
        public void ResolveStat_IgnoresModifiersForAnotherStat()
        {
            _role.AttackTime = 10f;
            _role.Modifiers = new[]
            {
                Modifier(RoleStat.AttackSpeed, RoleModifierOperation.Set, 20f)
            };

            Assert.AreEqual(10f, _role.ResolveStat(RoleStat.AttackTime, 20), 0.001f);
        }

        [Test]
        public void ResolveStat_UsesExactLevel()
        {
            AddLevels(4, 12, 40);

            Assert.AreEqual(12f, _role.ResolveStat(RoleStat.AttackTime, 12), 0.001f);
        }

        [Test]
        public void ResolveStat_UsesGreatestLevelBelowRequest()
        {
            AddLevels(4, 12, 40);

            Assert.AreEqual(12f, _role.ResolveStat(RoleStat.AttackTime, 20), 0.001f);
        }

        [Test]
        public void ResolveStat_ClampsRequestBelowFirstAndAboveLast()
        {
            AddLevels(4, 12, 40);

            Assert.AreEqual(4f, _role.ResolveStat(RoleStat.AttackTime, 1), 0.001f);
            Assert.AreEqual(40f, _role.ResolveStat(RoleStat.AttackTime, 99), 0.001f);
        }

        [Test]
        public void ResolveStat_ClampsInvalidTimingAndSpeed()
        {
            _role.AttackTime = -1f;
            _role.AttackSpeed = 0f;

            Assert.AreEqual(0f, _role.ResolveStat(RoleStat.AttackTime, 20), 0.001f);
            Assert.AreEqual(0.01f, _role.ResolveStat(RoleStat.AttackSpeed, 20), 0.001f);
        }

        [Test]
        public void ResolveStat_ClampsReservationToPercentRange()
        {
            var aura = ScriptableObject.CreateInstance<AuraRoleDefinition>();
            try
            {
                aura.ReservationPercent = 125f;
                aura.Modifiers = new[]
                {
                    Modifier(RoleStat.ReservationPercent, RoleModifierOperation.Add, 10f)
                };

                Assert.AreEqual(100f, aura.ResolveStat(RoleStat.ReservationPercent, 20), 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(aura);
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
