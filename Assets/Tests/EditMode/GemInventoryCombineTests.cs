using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;

namespace GemTD.Tests.EditMode
{
    public sealed class GemInventoryCombineTests
    {
        GemDefinition _chain;
        GemDefinition _fork;

        [SetUp]
        public void SetUp()
        {
            _chain = ScriptableObject.CreateInstance<GemDefinition>();
            _chain.Id = GemId.Chain;
            _chain.DisplayName = "Chain";

            _fork = ScriptableObject.CreateInstance<GemDefinition>();
            _fork.Id = GemId.Fork;
            _fork.DisplayName = "Fork";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_chain);
            Object.DestroyImmediate(_fork);
        }

        [Test]
        public void TryAdd_ThreeLesserSameFamily_BecomesOneNormalAtLowestIndex()
        {
            var inv = new GemInventory(10);
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Lesser)));
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Lesser)));
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Lesser)));

            Assert.AreEqual(1, inv.OccupiedCount);
            Assert.AreSame(_chain, inv.Slots[0].Def);
            Assert.AreEqual(GemRarity.Normal, inv.Slots[0].Rarity);
            Assert.IsTrue(inv.Slots[1].IsEmpty);
            Assert.IsTrue(inv.Slots[2].IsEmpty);
        }

        [Test]
        public void TryAdd_ThreeNormalSameFamily_BecomesOneGreater()
        {
            var inv = new GemInventory(10);
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Normal)));
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Normal)));
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Normal)));

            Assert.AreEqual(1, inv.OccupiedCount);
            Assert.AreEqual(GemRarity.Greater, inv.Slots[0].Rarity);
            Assert.AreSame(_chain, inv.Slots[0].Def);
        }

        [Test]
        public void TryAdd_ThreeGreaterSameFamily_DoesNotCombine()
        {
            var inv = new GemInventory(10);
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Greater)));
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Greater)));
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Greater)));

            Assert.AreEqual(3, inv.OccupiedCount);
            Assert.AreEqual(GemRarity.Greater, inv.Slots[0].Rarity);
            Assert.AreEqual(GemRarity.Greater, inv.Slots[1].Rarity);
            Assert.AreEqual(GemRarity.Greater, inv.Slots[2].Rarity);
        }

        [Test]
        public void TryAdd_TwoLesserAndOneNormalSameFamily_DoesNotCombine()
        {
            var inv = new GemInventory(10);
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Lesser)));
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Lesser)));
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Normal)));

            Assert.AreEqual(3, inv.OccupiedCount);
            Assert.AreEqual(GemRarity.Lesser, inv.Slots[0].Rarity);
            Assert.AreEqual(GemRarity.Lesser, inv.Slots[1].Rarity);
            Assert.AreEqual(GemRarity.Normal, inv.Slots[2].Rarity);
        }

        [Test]
        public void TryAdd_ThreeLesserDifferentFamilies_DoesNotCombine()
        {
            var inv = new GemInventory(10);
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Lesser)));
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Lesser)));
            Assert.IsTrue(inv.TryAdd(new GemInstance(_fork, GemRarity.Lesser)));

            Assert.AreEqual(3, inv.OccupiedCount);
        }

        [Test]
        public void TryAdd_ThreeLesserWithTwoNormalAlready_CascadesToGreater()
        {
            var inv = new GemInventory(10);
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Normal)));
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Normal)));
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Lesser)));
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Lesser)));
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Lesser)));

            Assert.AreEqual(1, inv.OccupiedCount);
            Assert.AreSame(_chain, inv.Slots[0].Def);
            Assert.AreEqual(GemRarity.Greater, inv.Slots[0].Rarity);
        }

        [Test]
        public void TryAdd_FourLesser_LeavesOneLesserAndOneNormal()
        {
            var inv = new GemInventory(10);
            for (var i = 0; i < 4; i++)
                Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Lesser)));

            Assert.AreEqual(2, inv.OccupiedCount);
            Assert.AreEqual(GemRarity.Normal, inv.Slots[0].Rarity);
            Assert.AreSame(_chain, inv.Slots[0].Def);
            Assert.AreEqual(GemRarity.Lesser, inv.Slots[1].Rarity);
            Assert.AreSame(_chain, inv.Slots[1].Def);
        }

        [Test]
        public void TryAddAt_ThirdMatching_CombinesIntoLowestConsumedIndex()
        {
            var inv = new GemInventory(10);
            Assert.IsTrue(inv.TryAddAt(2, new GemInstance(_chain, GemRarity.Lesser)));
            Assert.IsTrue(inv.TryAddAt(5, new GemInstance(_chain, GemRarity.Lesser)));
            Assert.IsTrue(inv.TryAddAt(7, new GemInstance(_chain, GemRarity.Lesser)));

            Assert.AreEqual(1, inv.OccupiedCount);
            Assert.AreSame(_chain, inv.Slots[2].Def);
            Assert.AreEqual(GemRarity.Normal, inv.Slots[2].Rarity);
            Assert.IsTrue(inv.Slots[5].IsEmpty);
            Assert.IsTrue(inv.Slots[7].IsEmpty);
        }

        [Test]
        public void TryMoveOrSwapAt_DoesNotFuseTwoMatchingGems()
        {
            var inv = new GemInventory(10);
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Lesser)));
            Assert.IsTrue(inv.TryAdd(new GemInstance(_chain, GemRarity.Lesser)));

            Assert.IsTrue(inv.TryMoveOrSwapAt(0, 4));

            Assert.AreEqual(2, inv.OccupiedCount);
            Assert.IsTrue(inv.Slots[0].IsEmpty);
            Assert.AreEqual(GemRarity.Lesser, inv.Slots[1].Rarity);
            Assert.AreEqual(GemRarity.Lesser, inv.Slots[4].Rarity);
        }

        [Test]
        public void Seed_ThreeSameFamily_CombinesToGreater()
        {
            var inv = new GemInventory(10);
            inv.Seed(new[] { _chain, _chain, _chain });

            Assert.AreEqual(1, inv.OccupiedCount);
            Assert.AreEqual(GemRarity.Greater, inv.Slots[0].Rarity);
            Assert.AreSame(_chain, inv.Slots[0].Def);
        }
    }
}
