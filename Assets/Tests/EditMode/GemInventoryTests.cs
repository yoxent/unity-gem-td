using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class GemInventoryTests
    {
        GemDefinition _lmp;
        GemDefinition _chain;
        TowerDefinition _tower;

        [SetUp]
        public void SetUp()
        {
            _lmp = ScriptableObject.CreateInstance<GemDefinition>();
            _lmp.Id = GemId.MultipleProjectiles;
            _lmp.DisplayName = "LMP";

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
            Object.DestroyImmediate(_lmp);
            Object.DestroyImmediate(_chain);
            Object.DestroyImmediate(_tower);
        }

        [Test]
        public void Seed_PopulatesSlots()
        {
            var inventory = new GemInventory(6);
            inventory.Seed(new[] { _lmp, _chain });

            Assert.AreEqual(6, inventory.Slots.Count);
            Assert.AreSame(_lmp, inventory.Slots[0]);
            Assert.AreSame(_chain, inventory.Slots[1]);
            Assert.IsNull(inventory.Slots[2]);
        }

        [Test]
        public void TryAdd_AddsToFirstEmptySlot()
        {
            var inventory = new GemInventory(2);
            Assert.IsTrue(inventory.TryAdd(_lmp));
            Assert.IsTrue(inventory.TryAdd(_chain));
            Assert.AreSame(_lmp, inventory.Slots[0]);
            Assert.AreSame(_chain, inventory.Slots[1]);
        }

        [Test]
        public void TryAdd_FailsWhenFull()
        {
            var inventory = new GemInventory(1);
            Assert.IsTrue(inventory.TryAdd(_lmp));
            Assert.IsFalse(inventory.TryAdd(_chain));
        }

        [Test]
        public void TryTake_RemovesMatchingGem()
        {
            var inventory = new GemInventory(6);
            inventory.Seed(new[] { _lmp, _chain });

            Assert.IsTrue(inventory.TryTake(GemId.MultipleProjectiles, out var taken));
            Assert.AreSame(_lmp, taken);
            Assert.IsNull(inventory.Slots[0]);
            Assert.AreSame(_chain, inventory.Slots[1]);
        }

        [Test]
        public void TryTake_FailsWhenNotPresent()
        {
            var inventory = new GemInventory(6);
            inventory.Seed(new[] { _lmp });

            Assert.IsFalse(inventory.TryTake(GemId.Chain, out _));
        }

        [Test]
        public void FreeSlotCount_ReflectsOccupied()
        {
            var inv = new GemInventory(10);
            Assert.AreEqual(10, inv.FreeSlotCount);
            inv.TryAdd(_lmp);
            Assert.AreEqual(9, inv.FreeSlotCount);
            Assert.AreEqual(1, inv.OccupiedCount);
        }

        [Test]
        public void TryDiscardAt_RemovesGem()
        {
            var inv = new GemInventory(10);
            inv.Seed(new[] { _lmp, _chain });
            Assert.IsTrue(inv.TryDiscardAt(0, out var gone));
            Assert.AreSame(_lmp, gone);
            Assert.IsNull(inv.Slots[0]);
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
            Assert.IsTrue(inv.TryAddAt(2, _lmp));
            Assert.AreSame(_lmp, inv.Slots[2]);
        }

        [Test]
        public void TryAddAt_FailsWhenIndexOccupied()
        {
            var inv = new GemInventory(2);
            Assert.IsTrue(inv.TryAddAt(0, _lmp));
            Assert.IsFalse(inv.TryAddAt(0, _chain));
            Assert.AreSame(_lmp, inv.Slots[0]);
        }

        [Test]
        public void TryMoveOrSwapAt_MovesToEmptySlot()
        {
            var inv = new GemInventory(3);
            inv.Seed(new[] { _lmp });

            Assert.IsTrue(inv.TryMoveOrSwapAt(0, 2));
            Assert.IsNull(inv.Slots[0]);
            Assert.AreSame(_lmp, inv.Slots[2]);
        }

        [Test]
        public void TryMoveOrSwapAt_SwapsWhenDestinationOccupied()
        {
            var inv = new GemInventory(3);
            inv.Seed(new[] { _lmp, _chain });

            Assert.IsTrue(inv.TryMoveOrSwapAt(0, 1));
            Assert.AreSame(_chain, inv.Slots[0]);
            Assert.AreSame(_lmp, inv.Slots[1]);
        }

        [Test]
        public void TowerInstance_SocketsLengthMatchesDefSocketCount()
        {
            var tower = new TowerInstance(new Vector2Int(2, 3), _tower);

            Assert.AreEqual(2, tower.Sockets.Length);
            Assert.IsNull(tower.Sockets[0]);
            Assert.IsNull(tower.Sockets[1]);
            Assert.AreEqual(new Vector2Int(2, 3), tower.Cell);
            Assert.AreSame(_tower, tower.Def);
        }

        [Test]
        public void TrySocket_SucceedsWhenAllowSocketTrue()
        {
            var tower = new TowerInstance(Vector2Int.zero, _tower);

            Assert.IsTrue(tower.TrySocket(_lmp, 0, allowSocket: true));
            Assert.AreSame(_lmp, tower.Sockets[0]);
        }

        [Test]
        public void TrySocket_FailsWhenAllowSocketFalse()
        {
            var tower = new TowerInstance(Vector2Int.zero, _tower);

            Assert.IsFalse(tower.TrySocket(_lmp, 0, allowSocket: false));
            Assert.IsNull(tower.Sockets[0]);
        }

        [Test]
        public void TryUnsocket_SucceedsWhenAllowSocketTrue()
        {
            var tower = new TowerInstance(Vector2Int.zero, _tower);
            tower.TrySocket(_lmp, 0, allowSocket: true);

            Assert.IsTrue(tower.TryUnsocket(0, out var gem, allowSocket: true));
            Assert.AreSame(_lmp, gem);
            Assert.IsNull(tower.Sockets[0]);
        }

        [Test]
        public void TryUnsocket_FailsWhenAllowSocketFalse()
        {
            var tower = new TowerInstance(Vector2Int.zero, _tower);
            tower.TrySocket(_lmp, 0, allowSocket: true);

            Assert.IsFalse(tower.TryUnsocket(0, out _, allowSocket: false));
            Assert.AreSame(_lmp, tower.Sockets[0]);
        }

        [Test]
        public void HasSocketedGems_TrueWhenAnySocketFilled()
        {
            var tower = new TowerInstance(Vector2Int.zero, _tower);
            Assert.IsFalse(tower.HasSocketedGems);

            tower.TrySocket(_lmp, 0, allowSocket: true);
            Assert.IsTrue(tower.HasSocketedGems);
        }

        [Test]
        public void TrySocket_FailsWhenIndexOccupied()
        {
            var tower = new TowerInstance(Vector2Int.zero, _tower);
            tower.TrySocket(_lmp, 0, allowSocket: true);

            Assert.IsFalse(tower.TrySocket(_chain, 0, allowSocket: true));
        }

        [Test]
        public void TrySocket_FailsWhenIndexOutOfRange()
        {
            var tower = new TowerInstance(Vector2Int.zero, _tower);

            Assert.IsFalse(tower.TrySocket(_lmp, 2, allowSocket: true));
            Assert.IsFalse(tower.TrySocket(_lmp, -1, allowSocket: true));
        }

        [Test]
        public void TrySocket_RejectsDuplicateGemIdOnSameTower()
        {
            var tower = new TowerInstance(Vector2Int.zero, _tower);
            var lmp2 = ScriptableObject.CreateInstance<GemDefinition>();
            lmp2.Id = GemId.MultipleProjectiles;
            try
            {
                Assert.IsTrue(tower.TrySocket(_lmp, 0, allowSocket: true));
                Assert.IsFalse(tower.TrySocket(lmp2, 1, allowSocket: true));
                Assert.IsNull(tower.Sockets[1]);
            }
            finally
            {
                Object.DestroyImmediate(lmp2);
            }
        }
    }
}
