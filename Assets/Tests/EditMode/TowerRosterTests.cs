using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class TowerRosterTests
    {
        TowerDefinition _a;
        TowerDefinition _b;

        [SetUp]
        public void SetUp()
        {
            _a = ScriptableObject.CreateInstance<TowerDefinition>();
            _a.DisplayName = "Alpha";
            _b = ScriptableObject.CreateInstance<TowerDefinition>();
            _b.DisplayName = "Beta";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_a);
            Object.DestroyImmediate(_b);
        }

        [Test]
        public void StartsEmpty()
        {
            var roster = new TowerRoster();
            Assert.AreEqual(0, roster.Count);
            Assert.IsFalse(roster.Contains(_a));
        }

        [Test]
        public void ApplyPick_NewType_UnlocksAtLevelIndexZero()
        {
            var roster = new TowerRoster();
            roster.ApplyPick(_a);

            Assert.AreEqual(1, roster.Count);
            Assert.IsTrue(roster.Contains(_a));
            Assert.IsTrue(roster.TryGetAt(0, out var def));
            Assert.AreSame(_a, def);
            Assert.AreEqual(0, roster.GetLevelIndex(_a));
            Assert.AreEqual(1, roster.GetDisplayLevel(_a));
        }

        [Test]
        public void ApplyPick_OwnedType_IncrementsLevelIndex()
        {
            var roster = new TowerRoster();
            roster.ApplyPick(_a);
            roster.ApplyPick(_a);

            Assert.AreEqual(1, roster.Count);
            Assert.AreEqual(1, roster.GetLevelIndex(_a));
            Assert.AreEqual(2, roster.GetDisplayLevel(_a));
        }

        [Test]
        public void ApplyPick_SecondType_AppendsWithoutResettingFirst()
        {
            var roster = new TowerRoster();
            roster.ApplyPick(_a);
            roster.ApplyPick(_a);
            roster.ApplyPick(_b);

            Assert.AreEqual(2, roster.Count);
            Assert.AreEqual(1, roster.GetLevelIndex(_a));
            Assert.AreEqual(0, roster.GetLevelIndex(_b));
        }

        [Test]
        public void ApplyLevels_WritesLevelIndexOntoMatchingPlacedTowers()
        {
            var roster = new TowerRoster();
            roster.ApplyPick(_a);
            roster.ApplyPick(_a);

            var placed = new List<TowerInstance>
            {
                new TowerInstance(Vector2Int.zero, _a),
                new TowerInstance(new Vector2Int(1, 0), _b),
                new TowerInstance(new Vector2Int(2, 0), _a)
            };

            roster.ApplyLevels(placed);

            Assert.AreEqual(1, placed[0].LevelIndex);
            Assert.AreEqual(0, placed[1].LevelIndex);
            Assert.AreEqual(1, placed[2].LevelIndex);
            Assert.AreEqual(2, placed[0].Level);
            Assert.AreEqual(1, placed[1].Level);
            Assert.AreEqual(2, placed[2].Level);
        }

        [Test]
        public void ApplyLevels_SelectsRoleLevelModifiersAndEffects()
        {
            var role = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            role.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 1,
                    Modifiers = new[]
                    {
                        RoleStatModifier.Single(RoleStat.Damage, RoleModifierOperation.Set, 10f)
                    },
                    Effects = new[]
                    {
                        RoleEffectModifier.Single(
                            RoleEffectKind.AllyOutgoingDamageMultiplier,
                            RoleModifierOperation.Set,
                            1f)
                    }
                },
                new RoleLevelDefinition
                {
                    SourceLevel = 2,
                    Modifiers = new[]
                    {
                        RoleStatModifier.Single(RoleStat.Damage, RoleModifierOperation.Set, 20f)
                    },
                    Effects = new[]
                    {
                        RoleEffectModifier.Single(
                            RoleEffectKind.AllyOutgoingDamageMultiplier,
                            RoleModifierOperation.Set,
                            1.5f)
                    }
                }
            };
            _a.Roles = new TowerRoleDefinition[] { role };

            try
            {
                var roster = new TowerRoster();
                roster.ApplyPick(_a);
                roster.ApplyPick(_a);

                var placed = new TowerInstance(Vector2Int.zero, _a);
                roster.ApplyLevels(new List<TowerInstance> { placed });

                Assert.AreEqual(1, placed.LevelIndex);
                Assert.AreEqual(2, placed.Level);
                Assert.AreEqual(20f, _a.GetDamageRange(placed.Level).Min, 0.001f);
                Assert.AreEqual(1.5f, role.ResolveEffect(RoleEffectKind.AllyOutgoingDamageMultiplier, placed.Level), 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(role);
            }
        }

        [Test]
        public void ApplyPick_EleventhUniqueType_Ignored()
        {
            var roster = new TowerRoster();
            var extras = new List<TowerDefinition>(TowerRoster.MaxSlots);
            for (var i = 0; i < TowerRoster.MaxSlots; i++)
            {
                var def = ScriptableObject.CreateInstance<TowerDefinition>();
                def.DisplayName = "Cap" + i;
                extras.Add(def);
                roster.ApplyPick(def);
            }

            try
            {
                Assert.IsTrue(roster.IsFull);
                roster.ApplyPick(_a);
                Assert.AreEqual(TowerRoster.MaxSlots, roster.Count);
                Assert.IsFalse(roster.Contains(_a));

                roster.ApplyPick(extras[0]);
                Assert.AreEqual(1, roster.GetLevelIndex(extras[0]));
            }
            finally
            {
                for (var i = 0; i < extras.Count; i++)
                    Object.DestroyImmediate(extras[i]);
            }
        }

        [Test]
        public void FormatOfferLabel_UnlockThenUpgrade()
        {
            var roster = new TowerRoster();
            var card = DraftOfferCard.FromTower(_a);

            Assert.AreEqual("Alpha\nUnlock", TowerRoster.FormatOfferLabel(card, roster));

            roster.ApplyPick(_a);
            Assert.AreEqual("Alpha\nUpgrade to level 2", TowerRoster.FormatOfferLabel(card, roster));
        }
    }
}
