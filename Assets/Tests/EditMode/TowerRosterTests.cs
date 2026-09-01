using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
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
        public void ApplyPick_SixthDamaging_Ignored_WhenCapIsFive()
        {
            var roster = new TowerRoster(new TowerRosterCaps(5, 2, 2));
            var extras = new List<TowerDefinition>(6);
            for (var i = 0; i < 6; i++)
            {
                var def = ScriptableObject.CreateInstance<TowerDefinition>();
                def.DisplayName = "D" + i;
                extras.Add(def);
                roster.ApplyPick(def);
            }

            try
            {
                Assert.AreEqual(5, roster.Count);
                Assert.IsFalse(roster.Contains(extras[5]));
                Assert.AreEqual(0, roster.Remaining(TowerRosterCategory.Damaging));
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
        public void ApplyPick_ThirdCurse_Ignored_WhenCapIsTwo()
        {
            var roster = new TowerRoster(new TowerRosterCaps(5, 2, 2));
            var a = MakeCurse("A");
            var b = MakeCurse("B");
            var c = MakeCurse("C");
            try
            {
                roster.ApplyPick(a);
                roster.ApplyPick(b);
                roster.ApplyPick(c);
                Assert.AreEqual(2, roster.Count);
                Assert.IsFalse(roster.Contains(c));
                Assert.AreEqual(2, roster.CountIn(TowerRosterCategory.Curse));
            }
            finally
            {
                DestroyCurse(a);
                DestroyCurse(b);
                DestroyCurse(c);
            }
        }

        [Test]
        public void ApplyPick_CapZero_NeverUnlocks()
        {
            var roster = new TowerRoster(new TowerRosterCaps(0, 2, 2));
            roster.ApplyPick(_a);
            Assert.AreEqual(0, roster.Count);
            Assert.IsFalse(roster.CanUnlock(_a));
        }

        [Test]
        public void MaxSlots_FollowsSum()
        {
            Assert.AreEqual(9, new TowerRoster(new TowerRosterCaps(5, 2, 2)).MaxSlots);
            Assert.AreEqual(8, new TowerRoster(new TowerRosterCaps(4, 2, 2)).MaxSlots);
            Assert.AreEqual(10, new TowerRoster(new TowerRosterCaps(6, 2, 2)).MaxSlots);
        }

        TowerDefinition MakeCurse(string name)
        {
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            def.DisplayName = name;
            var role = ScriptableObject.CreateInstance<CurseRoleDefinition>();
            def.Roles = new TowerRoleDefinition[] { role };
            return def;
        }

        static void DestroyCurse(TowerDefinition def)
        {
            if (def == null)
                return;
            if (def.Roles != null)
            {
                for (var i = 0; i < def.Roles.Length; i++)
                {
                    if (def.Roles[i] != null)
                        Object.DestroyImmediate(def.Roles[i]);
                }
            }

            Object.DestroyImmediate(def);
        }

        [Test]
        public void FormatOfferLabel_IsNameOnly_StatusIsNewOrUpgrade()
        {
            var roster = new TowerRoster();
            var card = DraftOfferCard.FromTower(_a);

            Assert.AreEqual("Alpha", TowerRoster.FormatOfferLabel(card, roster));
            Assert.AreEqual("New", TowerRoster.FormatOfferStatus(card, roster));

            roster.ApplyPick(_a);
            Assert.AreEqual("Alpha", TowerRoster.FormatOfferLabel(card, roster));
            Assert.AreEqual("Upgrade", TowerRoster.FormatOfferStatus(card, roster));
        }

        [Test]
        public void FormatOfferStatus_GemHasNoStatus()
        {
            var gem = ScriptableObject.CreateInstance<GemDefinition>();
            gem.DisplayName = "Chain";
            var card = DraftOfferCard.FromGem(GemInstance.FromDefinition(gem));
            Assert.AreEqual("Chain", TowerRoster.FormatOfferLabel(card, null));
            Assert.AreEqual("", TowerRoster.FormatOfferStatus(card, null));
            Object.DestroyImmediate(gem);
        }

        [Test]
        public void DraftOfferCard_ExposesTowerAndGemDescription()
        {
            _a.Description = "Swings in an arc.";
            Assert.AreEqual("Swings in an arc.", DraftOfferCard.FromTower(_a).Description);

            var gem = ScriptableObject.CreateInstance<GemDefinition>();
            gem.DisplayName = "Chain";
            gem.Description = "Projectiles chain to nearby enemies.";
            Assert.AreEqual(
                "Projectiles chain to nearby enemies.",
                DraftOfferCard.FromGem(GemInstance.FromDefinition(gem)).Description);
            Object.DestroyImmediate(gem);
        }
    }
}
