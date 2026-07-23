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
        TowerDefinition _ballista;

        [SetUp]
        public void SetUp()
        {
            _lmp = ScriptableObject.CreateInstance<GemDefinition>();
            _lmp.Id = GemId.Lmp;
            _lmp.DisplayName = "LMP";

            _chain = ScriptableObject.CreateInstance<GemDefinition>();
            _chain.Id = GemId.Chain;
            _chain.DisplayName = "Chain";

            _ballista = ScriptableObject.CreateInstance<TowerDefinition>();
            _ballista.DisplayName = "Ballista";
            _ballista.SocketCount = 2;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_lmp);
            Object.DestroyImmediate(_chain);
            Object.DestroyImmediate(_ballista);
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

            Assert.IsTrue(inventory.TryTake(GemId.Lmp, out var taken));
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
        public void TowerRuntime_SocketsLengthMatchesDefSocketCount()
        {
            var tower = new TowerRuntime(new Vector2Int(2, 3), _ballista);

            Assert.AreEqual(2, tower.Sockets.Length);
            Assert.IsNull(tower.Sockets[0]);
            Assert.IsNull(tower.Sockets[1]);
            Assert.AreEqual(new Vector2Int(2, 3), tower.Cell);
            Assert.AreSame(_ballista, tower.Def);
        }

        [Test]
        public void TrySocket_SucceedsWhenAllowSocketTrue()
        {
            var tower = new TowerRuntime(Vector2Int.zero, _ballista);

            Assert.IsTrue(tower.TrySocket(_lmp, 0, allowSocket: true));
            Assert.AreSame(_lmp, tower.Sockets[0]);
        }

        [Test]
        public void TrySocket_FailsWhenAllowSocketFalse()
        {
            var tower = new TowerRuntime(Vector2Int.zero, _ballista);

            Assert.IsFalse(tower.TrySocket(_lmp, 0, allowSocket: false));
            Assert.IsNull(tower.Sockets[0]);
        }

        [Test]
        public void TryUnsocket_SucceedsWhenAllowSocketTrue()
        {
            var tower = new TowerRuntime(Vector2Int.zero, _ballista);
            tower.TrySocket(_lmp, 0, allowSocket: true);

            Assert.IsTrue(tower.TryUnsocket(0, out var gem, allowSocket: true));
            Assert.AreSame(_lmp, gem);
            Assert.IsNull(tower.Sockets[0]);
        }

        [Test]
        public void TryUnsocket_FailsWhenAllowSocketFalse()
        {
            var tower = new TowerRuntime(Vector2Int.zero, _ballista);
            tower.TrySocket(_lmp, 0, allowSocket: true);

            Assert.IsFalse(tower.TryUnsocket(0, out _, allowSocket: false));
            Assert.AreSame(_lmp, tower.Sockets[0]);
        }

        [Test]
        public void HasSocketedGems_TrueWhenAnySocketFilled()
        {
            var tower = new TowerRuntime(Vector2Int.zero, _ballista);
            Assert.IsFalse(tower.HasSocketedGems);

            tower.TrySocket(_lmp, 0, allowSocket: true);
            Assert.IsTrue(tower.HasSocketedGems);
        }

        [Test]
        public void TrySocket_FailsWhenIndexOccupied()
        {
            var tower = new TowerRuntime(Vector2Int.zero, _ballista);
            tower.TrySocket(_lmp, 0, allowSocket: true);

            Assert.IsFalse(tower.TrySocket(_chain, 0, allowSocket: true));
        }

        [Test]
        public void TrySocket_FailsWhenIndexOutOfRange()
        {
            var tower = new TowerRuntime(Vector2Int.zero, _ballista);

            Assert.IsFalse(tower.TrySocket(_lmp, 2, allowSocket: true));
            Assert.IsFalse(tower.TrySocket(_lmp, -1, allowSocket: true));
        }
    }
}
