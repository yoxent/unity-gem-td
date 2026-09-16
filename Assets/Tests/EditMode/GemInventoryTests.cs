using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class GemInventoryTests
    {
        GemDefinition _multipleProjectiles;
        GemDefinition _chain;
        TowerDefinition _tower;

        [SetUp]
        public void SetUp()
        {
            _multipleProjectiles = ScriptableObject.CreateInstance<GemDefinition>();
            _multipleProjectiles.Id = GemId.MultipleProjectiles;
            _multipleProjectiles.DisplayName = "Multiple Projectiles";

            _chain = ScriptableObject.CreateInstance<GemDefinition>();
            _chain.Id = GemId.Chain;
            _chain.DisplayName = "Chain";

            _tower = ScriptableObject.CreateInstance<TowerDefinition>();
            _tower.DisplayName = "Test Tower";
            _tower.SocketCount = 2;
            _tower.Tags = GemTag.Attack | GemTag.Projectile;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_multipleProjectiles);
            Object.DestroyImmediate(_chain);
            Object.DestroyImmediate(_tower);
        }

        [Test]
        public void Seed_PopulatesSlots()
        {
            var inventory = new GemInventory(6);
            inventory.Seed(new[] { _multipleProjectiles, _chain });

            Assert.AreEqual(6, inventory.Slots.Count);
            Assert.AreSame(_multipleProjectiles, inventory.Slots[0].Def);
            Assert.AreEqual(GemRarity.Normal, inventory.Slots[0].Rarity);
            Assert.AreSame(_chain, inventory.Slots[1].Def);
            Assert.AreEqual(GemRarity.Normal, inventory.Slots[1].Rarity);
            Assert.IsTrue(inventory.Slots[2].IsEmpty);
        }

        [Test]
        public void TryAdd_AddsToFirstEmptySlot()
        {
            var inventory = new GemInventory(2);
            Assert.IsTrue(inventory.TryAdd(_multipleProjectiles));
            Assert.IsTrue(inventory.TryAdd(_chain));
            Assert.AreSame(_multipleProjectiles, inventory.Slots[0].Def);
            Assert.AreSame(_chain, inventory.Slots[1].Def);
        }

        [Test]
        public void TryAdd_FailsWhenFull()
        {
            var inventory = new GemInventory(1);
            Assert.IsTrue(inventory.TryAdd(_multipleProjectiles));
            Assert.IsFalse(inventory.TryAdd(_chain));
        }

        [Test]
        public void TryTake_RemovesMatchingGem()
        {
            var inventory = new GemInventory(6);
            inventory.Seed(new[] { _multipleProjectiles, _chain });

            Assert.IsTrue(inventory.TryTake(GemId.MultipleProjectiles, out var taken));
            Assert.AreSame(_multipleProjectiles, taken.Def);
            Assert.IsTrue(inventory.Slots[0].IsEmpty);
            Assert.AreSame(_chain, inventory.Slots[1].Def);
        }

        [Test]
        public void TryAddAndTake_PreserveRarity()
        {
            var inventory = new GemInventory(2);
            var instance = new GemInstance(_chain, GemRarity.Greater);

            Assert.IsTrue(inventory.TryAdd(instance));
            Assert.IsTrue(inventory.TryTake(GemId.Chain, out var taken));
            Assert.AreSame(_chain, taken.Def);
            Assert.AreEqual(GemRarity.Greater, taken.Rarity);
        }

        [Test]
        public void TryTake_FailsWhenNotPresent()
        {
            var inventory = new GemInventory(6);
            inventory.Seed(new[] { _multipleProjectiles });

            Assert.IsFalse(inventory.TryTake(GemId.Chain, out _));
        }

        [Test]
        public void FreeSlotCount_ReflectsOccupied()
        {
            var inv = new GemInventory(10);
            Assert.AreEqual(10, inv.FreeSlotCount);
            inv.TryAdd(_multipleProjectiles);
            Assert.AreEqual(9, inv.FreeSlotCount);
            Assert.AreEqual(1, inv.OccupiedCount);
        }

        [Test]
        public void TryDiscardAt_RemovesGem()
        {
            var inv = new GemInventory(10);
            inv.Seed(new[] { _multipleProjectiles, _chain });
            Assert.IsTrue(inv.TryDiscardAt(0, out var gone));
            Assert.AreSame(_multipleProjectiles, gone.Def);
            Assert.IsTrue(inv.Slots[0].IsEmpty);
            Assert.AreEqual(9, inv.FreeSlotCount);
        }

        [Test]
        public void TryDiscardAt_FailsOnEmptySlot()
        {
            var inv = new GemInventory(10);
            Assert.IsFalse(inv.TryDiscardAt(0, out _));
        }

        [Test]
        public void TryAddAt_AddsToExactIndex()
        {
            var inv = new GemInventory(3);
            Assert.IsTrue(inv.TryAddAt(2, _multipleProjectiles));
            Assert.AreSame(_multipleProjectiles, inv.Slots[2].Def);
        }

        [Test]
        public void TryAddAt_FailsWhenIndexOccupied()
        {
            var inv = new GemInventory(2);
            Assert.IsTrue(inv.TryAddAt(0, _multipleProjectiles));
            Assert.IsFalse(inv.TryAddAt(0, _chain));
            Assert.AreSame(_multipleProjectiles, inv.Slots[0].Def);
        }

        [Test]
        public void TryMoveOrSwapAt_MovesToEmptySlot()
        {
            var inv = new GemInventory(3);
            inv.Seed(new[] { _multipleProjectiles });

            Assert.IsTrue(inv.TryMoveOrSwapAt(0, 2));
            Assert.IsTrue(inv.Slots[0].IsEmpty);
            Assert.AreSame(_multipleProjectiles, inv.Slots[2].Def);
        }

        [Test]
        public void TryMoveOrSwapAt_SwapsWhenDestinationOccupied()
        {
            var inv = new GemInventory(3);
            inv.Seed(new[] { _multipleProjectiles, _chain });

            Assert.IsTrue(inv.TryMoveOrSwapAt(0, 1));
            Assert.AreSame(_chain, inv.Slots[0].Def);
            Assert.AreSame(_multipleProjectiles, inv.Slots[1].Def);
        }

        [Test]
        public void TowerInstance_SocketsLengthMatchesDefSocketCount()
        {
            var tower = new TowerInstance(new Vector2Int(2, 3), _tower);

            Assert.AreEqual(2, tower.Sockets.Length);
            Assert.IsTrue(tower.Sockets[0].IsEmpty);
            Assert.IsTrue(tower.Sockets[1].IsEmpty);
            Assert.AreEqual(new Vector2Int(2, 3), tower.Cell);
            Assert.AreSame(_tower, tower.Def);
        }

        [Test]
        public void TrySocket_SucceedsWhenAllowSocketTrue()
        {
            var tower = new TowerInstance(Vector2Int.zero, _tower);

            Assert.IsTrue(tower.TrySocket(_multipleProjectiles, 0, allowSocket: true));
            Assert.AreSame(_multipleProjectiles, tower.Sockets[0].Def);
            Assert.AreEqual(GemRarity.Normal, tower.Sockets[0].Rarity);
        }

        [Test]
        public void TrySocket_FailsWhenAllowSocketFalse()
        {
            var tower = new TowerInstance(Vector2Int.zero, _tower);

            Assert.IsFalse(tower.TrySocket(_multipleProjectiles, 0, allowSocket: false));
            Assert.IsTrue(tower.Sockets[0].IsEmpty);
        }

        [Test]
        public void TryUnsocket_SucceedsWhenAllowSocketTrue()
        {
            var tower = new TowerInstance(Vector2Int.zero, _tower);
            tower.TrySocket(_multipleProjectiles, 0, allowSocket: true);

            Assert.IsTrue(tower.TryUnsocket(0, out var gem, allowSocket: true));
            Assert.AreSame(_multipleProjectiles, gem.Def);
            Assert.IsTrue(tower.Sockets[0].IsEmpty);
        }

        [Test]
        public void TryUnsocket_FailsWhenAllowSocketFalse()
        {
            var tower = new TowerInstance(Vector2Int.zero, _tower);
            tower.TrySocket(_multipleProjectiles, 0, allowSocket: true);

            Assert.IsFalse(tower.TryUnsocket(0, out _, allowSocket: false));
            Assert.AreSame(_multipleProjectiles, tower.Sockets[0].Def);
        }

        [Test]
        public void HasSocketedGems_TrueWhenAnySocketFilled()
        {
            var tower = new TowerInstance(Vector2Int.zero, _tower);
            Assert.IsFalse(tower.HasSocketedGems);

            tower.TrySocket(_multipleProjectiles, 0, allowSocket: true);
            Assert.IsTrue(tower.HasSocketedGems);
        }

        [Test]
        public void TrySocket_FailsWhenIndexOccupied()
        {
            var tower = new TowerInstance(Vector2Int.zero, _tower);
            tower.TrySocket(_multipleProjectiles, 0, allowSocket: true);

            Assert.IsFalse(tower.TrySocket(_chain, 0, allowSocket: true));
        }

        [Test]
        public void TrySocket_FailsWhenIndexOutOfRange()
        {
            var tower = new TowerInstance(Vector2Int.zero, _tower);

            Assert.IsFalse(tower.TrySocket(_multipleProjectiles, 2, allowSocket: true));
            Assert.IsFalse(tower.TrySocket(_multipleProjectiles, -1, allowSocket: true));
        }

        [Test]
        public void TrySocket_RejectsDuplicateGemIdOnSameTower()
        {
            var tower = new TowerInstance(Vector2Int.zero, _tower);
            var multipleProjectiles2 = ScriptableObject.CreateInstance<GemDefinition>();
            multipleProjectiles2.Id = GemId.MultipleProjectiles;
            try
            {
                Assert.IsTrue(tower.TrySocket(_multipleProjectiles, 0, allowSocket: true));
                Assert.IsFalse(tower.TrySocket(multipleProjectiles2, 1, allowSocket: true));
                Assert.IsTrue(tower.Sockets[1].IsEmpty);
            }
            finally
            {
                Object.DestroyImmediate(multipleProjectiles2);
            }
        }
    }
}
