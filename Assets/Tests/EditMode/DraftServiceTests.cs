using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class DraftServiceTests
    {
        readonly List<Object> _destroy = new List<Object>(16);

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _destroy.Count; i++)
            {
                if (_destroy[i] != null)
                    Object.DestroyImmediate(_destroy[i]);
            }
            _destroy.Clear();
        }

        [Test]
        public void BeginOffer_FourTowers_FourUnique()
        {
            var draft = new DraftService(new System.Random(1));
            draft.BeginOffer(MakeFourTowerCatalog(6), allowSkip: true);

            Assert.AreEqual(4, draft.CurrentOffer.Count);
            Assert.IsTrue(draft.IsActive);
            for (var i = 0; i < 4; i++)
            {
                Assert.IsTrue(draft.CurrentOffer[i].IsTower);
                for (var j = i + 1; j < 4; j++)
                    Assert.AreNotSame(draft.CurrentOffer[i].Tower, draft.CurrentOffer[j].Tower);
            }
        }

        [Test]
        public void TrySkip_Starter_Rejected()
        {
            var draft = new DraftService(new System.Random(1));
            draft.BeginOffer(MakeFourTowerCatalog(4), allowSkip: false);
            var economy = new RunEconomy(0, 20);

            Assert.IsFalse(draft.TrySkip(economy, 75, out _));
            Assert.AreEqual(0, economy.Gold);
            Assert.IsTrue(draft.IsActive);
        }

        [Test]
        public void TrySkip_MidRun_GrantsGoldAndResolves()
        {
            var draft = new DraftService(new System.Random(1));
            draft.BeginOffer(MakeFourTowerCatalog(4), allowSkip: true);
            var economy = new RunEconomy(0, 20);

            Assert.IsTrue(draft.TrySkip(economy, 75, out var resolved));
            Assert.IsTrue(resolved);
            Assert.AreEqual(75, economy.Gold);
            Assert.IsFalse(draft.IsActive);
        }

        [Test]
        public void TryPick_Tower_ResolvesWithoutInventory()
        {
            var draft = new DraftService(new System.Random(1));
            draft.BeginOffer(MakeFourTowerCatalog(4), allowSkip: false);
            var inventory = new GemInventory(10);

            var picked = draft.CurrentOffer[0].Tower;
            Assert.IsTrue(draft.TryPick(0, inventory, out var resolved));
            Assert.IsTrue(resolved);
            Assert.IsFalse(draft.IsActive);
            Assert.AreEqual(0, inventory.OccupiedCount);
            Assert.AreEqual(1, draft.Roster.Count);
            Assert.IsTrue(draft.Roster.Contains(picked));
            Assert.AreEqual(0, draft.Roster.GetLevelIndex(picked));
        }

        [Test]
        public void TryPick_DamagingCapOne_LaterOffersOnlyOwnedUpgrade()
        {
            var catalog = MakeFourTowerCatalog(4);
            var draft = new DraftService(new System.Random(1), new TowerRosterCaps(1, 0, 0));
            var inventory = new GemInventory(10);
            draft.BeginOffer(catalog, allowSkip: false);
            var picked = draft.CurrentOffer[0].Tower;
            Assert.IsTrue(draft.TryPick(0, inventory, out _));
            Assert.AreEqual(1, draft.Roster.Count);

            draft.BeginOffer(catalog, allowSkip: true);
            Assert.Greater(draft.CurrentOffer.Count, 0);
            for (var i = 0; i < draft.CurrentOffer.Count; i++)
            {
                Assert.IsTrue(draft.CurrentOffer[i].IsTower);
                Assert.AreSame(picked, draft.CurrentOffer[i].Tower);
            }
        }

        [Test]
        public void TryPick_SameTowerTwice_IncrementsRosterLevel()
        {
            var catalog = MakeFourTowerCatalog(1);
            var only = catalog.TowerPool.Towers[0];
            var draft = new DraftService(new System.Random(1));
            var inventory = new GemInventory(10);

            draft.BeginOffer(catalog, allowSkip: true);
            Assert.IsTrue(draft.TryPick(0, inventory, out _));

            draft.BeginOffer(catalog, allowSkip: true);
            Assert.IsTrue(draft.TryPick(0, inventory, out var resolved));
            Assert.IsTrue(resolved);
            Assert.AreEqual(1, draft.Roster.Count);
            Assert.AreEqual(1, draft.Roster.GetLevelIndex(only));
        }

        [Test]
        public void TryPick_Gem_WhenFull_EntersReplaceConfirm_NoMeansStay()
        {
            var catalog = MakeCampaignCatalog(5, 3);
            catalog.RarityTable = MakeRarityTable(lesser: 0f, normal: 0f, greater: 1f);
            var draft = new DraftService(new System.Random(1));
            draft.BeginOffer(catalog, allowSkip: true);
            var inventory = FillInventory(10);
            var gemIndex = FirstGemIndex(draft);
            var first = draft.CurrentOffer[gemIndex];

            Assert.IsTrue(draft.TryPick(gemIndex, inventory, out var resolved));
            Assert.IsFalse(resolved);
            Assert.AreEqual(DraftReplacePhase.AwaitingConfirm, draft.ReplacePhase);
            Assert.AreEqual(first.Gem.Def, draft.PendingReplaceGem.Def);
            Assert.AreEqual(first.Gem.Rarity, draft.PendingReplaceGem.Rarity);

            draft.ConfirmReplaceNo();
            Assert.AreEqual(DraftReplacePhase.None, draft.ReplacePhase);
            Assert.IsTrue(draft.PendingReplaceGem.IsEmpty);
            Assert.IsTrue(draft.IsActive);
            Assert.AreEqual(4, draft.CurrentOffer.Count);
        }

        [Test]
        public void TryPick_Gem_WithFreeSlot_AddsAndResolves()
        {
            var draft = new DraftService(new System.Random(3));
            draft.BeginOffer(MakeCampaignCatalog(5, 3), allowSkip: false);
            var inventory = new GemInventory(10);
            var gemIndex = FirstGemIndex(draft);
            var picked = draft.CurrentOffer[gemIndex];

            Assert.IsTrue(draft.TryPick(gemIndex, inventory, out var resolved));
            Assert.IsTrue(resolved);
            Assert.AreSame(picked.Gem.Def, inventory.Slots[0].Def);
            Assert.AreEqual(picked.Gem.Rarity, inventory.Slots[0].Rarity);
            Assert.IsFalse(draft.IsActive);
        }

        [Test]
        public void TryPick_FullGem_YesThenDiscardSlot_AddsAndResolves()
        {
            var catalog = MakeCampaignCatalog(5, 3);
            catalog.RarityTable = MakeRarityTable(lesser: 0f, normal: 0f, greater: 1f);
            var draft = new DraftService(new System.Random(1));
            draft.BeginOffer(catalog, allowSkip: true);
            var inventory = FillInventory(10);
            var gemIndex = FirstGemIndex(draft);
            var picked = draft.CurrentOffer[gemIndex];

            Assert.IsTrue(draft.TryPick(gemIndex, inventory, out _));
            Assert.AreSame(picked.Gem.Def, draft.PendingReplaceGem.Def);
            Assert.AreEqual(picked.Gem.Rarity, draft.PendingReplaceGem.Rarity);
            draft.ConfirmReplaceYes();
            Assert.AreEqual(DraftReplacePhase.AwaitingInventoryPick, draft.ReplacePhase);

            Assert.IsTrue(draft.TryCompleteReplace(0, inventory, out var resolved));
            Assert.IsTrue(resolved);
            Assert.IsFalse(draft.IsActive);

            var found = false;
            for (var i = 0; i < inventory.Slots.Count; i++)
            {
                if (ReferenceEquals(inventory.Slots[i].Def, picked.Gem.Def)
                    && inventory.Slots[i].Rarity == picked.Gem.Rarity)
                    found = true;
            }

            Assert.IsTrue(found);
        }

        [Test]
        public void TryBan_Gem_ExcludesFamilyAcrossRarities()
        {
            var catalog = MakeCampaignCatalog(8, 4);
            var rarityTable = MakeRarityTable(lesser: 0f, normal: 0f, greater: 1f);
            catalog.RarityTable = rarityTable;
            var draft = new DraftService(new System.Random(9));
            draft.BeginOffer(catalog, allowSkip: true);
            var gemIndex = FirstGemIndex(draft);
            var bannedFamily = draft.CurrentOffer[gemIndex].Gem.Def;
            Assert.AreEqual(GemRarity.Greater, draft.CurrentOffer[gemIndex].Gem.Rarity);
            Assert.IsTrue(draft.TrySelect(gemIndex));
            Assert.IsTrue(draft.TryBan(new RunEconomy(1000, 20)));

            rarityTable.LesserWeight = 1f;
            rarityTable.GreaterWeight = 0f;
            draft.BeginOffer(catalog, allowSkip: true);

            for (var i = 0; i < draft.CurrentOffer.Count; i++)
            {
                if (!draft.CurrentOffer[i].IsGem)
                    continue;
                Assert.AreEqual(GemRarity.Lesser, draft.CurrentOffer[i].Gem.Rarity);
                Assert.AreNotSame(bannedFamily, draft.CurrentOffer[i].Gem.Def);
            }
        }

        [Test]
        public void TryReroll_PaysDoublingCost_ExcludesPriorOffer()
        {
            var draft = new DraftService(new System.Random(1));
            draft.BeginOffer(MakeFourTowerCatalog(8), allowSkip: true);
            var first = new TowerDefinition[draft.CurrentOffer.Count];
            for (var i = 0; i < draft.CurrentOffer.Count; i++)
                first[i] = draft.CurrentOffer[i].Tower;

            var economy = new RunEconomy(200, 20);
            Assert.AreEqual(50, draft.NextRerollCost);
            Assert.IsTrue(draft.TryReroll(economy));
            Assert.AreEqual(150, economy.Gold);
            Assert.AreEqual(100, draft.NextRerollCost);
            Assert.IsTrue(draft.IsActive);

            for (var i = 0; i < draft.CurrentOffer.Count; i++)
            {
                for (var j = 0; j < first.Length; j++)
                    Assert.AreNotSame(draft.CurrentOffer[i].Tower, first[j]);
            }

            Assert.IsTrue(draft.TryReroll(economy));
            Assert.AreEqual(50, economy.Gold);
            Assert.AreEqual(200, draft.NextRerollCost);
        }

        [Test]
        public void TryBan_EmptiesSlot_DoesNotResetCostOnNextOffer()
        {
            var draft = new DraftService(new System.Random(1));
            draft.BeginOffer(MakeFourTowerCatalog(6), allowSkip: false);
            Assert.IsTrue(draft.TrySelect(0));
            var banned = draft.CurrentOffer[0].Tower;
            var economy = new RunEconomy(1000, 20);

            Assert.AreEqual(250, draft.NextBanCost);
            Assert.IsTrue(draft.TryBan(economy));
            Assert.AreEqual(750, economy.Gold);
            Assert.AreEqual(500, draft.NextBanCost);
            Assert.IsFalse(draft.CurrentOffer[0].IsFilled);
            Assert.AreEqual(3, draft.FilledCardCount);

            draft.BeginOffer(MakeFourTowerCatalog(6), allowSkip: true);
            Assert.AreEqual(500, draft.NextBanCost);
            Assert.AreEqual(50, draft.NextRerollCost);
            for (var i = 0; i < draft.CurrentOffer.Count; i++)
                Assert.AreNotSame(draft.CurrentOffer[i].Tower, banned);
        }

        [Test]
        public void TryBan_StarterLastCard_BlockedWhenRerollUnaffordable()
        {
            var draft = new DraftService(new System.Random(2));
            draft.BeginOffer(MakeFourTowerCatalog(4), allowSkip: false);
            var economy = new RunEconomy(1500, 20);

            for (var n = 0; n < 3; n++)
            {
                var idx = FirstFilledIndex(draft);
                Assert.IsTrue(draft.TrySelect(idx));
                Assert.IsTrue(draft.TryBan(economy));
            }

            Assert.AreEqual(1, draft.FilledCardCount);
            Assert.AreEqual(0, economy.Gold);
            var last = FirstFilledIndex(draft);
            Assert.IsTrue(draft.TrySelect(last));
            Assert.IsFalse(draft.CanBan(economy));
            Assert.IsFalse(draft.TryBan(economy));
        }

        static int FirstFilledIndex(DraftService draft)
        {
            for (var i = 0; i < draft.CurrentOffer.Count; i++)
            {
                if (draft.CurrentOffer[i].IsFilled)
                    return i;
            }

            Assert.Fail("offer had no filled card");
            return -1;
        }

        static int FirstGemIndex(DraftService draft)
        {
            for (var i = 0; i < draft.CurrentOffer.Count; i++)
            {
                if (draft.CurrentOffer[i].IsGem)
                    return i;
            }

            Assert.Fail("campaign offer had no gem");
            return -1;
        }

        DraftCatalog MakeFourTowerCatalog(int towers)
        {
            var catalog = ScriptableObject.CreateInstance<DraftCatalog>();
            _destroy.Add(catalog);
            catalog.Mix = DraftMixKind.FourTowers;
            catalog.TowerPool = MakeTowerPool(towers);
            return catalog;
        }

        DraftCatalog MakeCampaignCatalog(int gems, int towers)
        {
            var catalog = ScriptableObject.CreateInstance<DraftCatalog>();
            _destroy.Add(catalog);
            catalog.Mix = DraftMixKind.TwoGemsOneTowerContested;
            catalog.GemPool = MakeGemPool(gems);
            catalog.TowerPool = MakeTowerPool(towers);
            return catalog;
        }

        DraftPoolCatalog MakeGemPool(int n)
        {
            var pool = ScriptableObject.CreateInstance<DraftPoolCatalog>();
            _destroy.Add(pool);
            pool.Gems = new GemDefinition[n];
            for (var i = 0; i < n; i++)
            {
                var gem = ScriptableObject.CreateInstance<GemDefinition>();
                gem.Id = (GemId)(i + 1);
                gem.DisplayName = "G" + i;
                gem.DraftWeight = 1f;
                _destroy.Add(gem);
                pool.Gems[i] = gem;
            }

            return pool;
        }

        TowerCatalog MakeTowerPool(int n)
        {
            var pool = ScriptableObject.CreateInstance<TowerCatalog>();
            _destroy.Add(pool);
            pool.Towers = new TowerDefinition[n];
            for (var i = 0; i < n; i++)
            {
                var tower = ScriptableObject.CreateInstance<TowerDefinition>();
                tower.DisplayName = "T" + i;
                _destroy.Add(tower);
                pool.Towers[i] = tower;
            }

            return pool;
        }

        GemRarityTable MakeRarityTable(float lesser, float normal, float greater)
        {
            var table = ScriptableObject.CreateInstance<GemRarityTable>();
            table.LesserWeight = lesser;
            table.NormalWeight = normal;
            table.GreaterWeight = greater;
            _destroy.Add(table);
            return table;
        }

        GemInventory FillInventory(int capacity)
        {
            var inventory = new GemInventory(capacity);
            for (var i = 0; i < capacity; i++)
            {
                var filler = ScriptableObject.CreateInstance<GemDefinition>();
                // Distinct families: same-id triples fuse in TryAdd and would leave free slots.
                filler.Id = (GemId)(i + 1);
                filler.DisplayName = "Fill" + i;
                _destroy.Add(filler);
                Assert.IsTrue(inventory.TryAdd(filler));
            }

            Assert.AreEqual(0, inventory.FreeSlotCount);
            return inventory;
        }
    }
}
