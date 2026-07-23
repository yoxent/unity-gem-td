using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;

namespace GemTD.Tests.EditMode
{
    public sealed class DraftServiceTests
    {
        readonly List<GemDefinition> _pool = new List<GemDefinition>(6);
        readonly List<Object> _destroy = new List<Object>(8);

        [SetUp]
        public void SetUp()
        {
            _pool.Clear();
            _destroy.Clear();
            _pool.Add(MakeGem(GemId.Lmp, "LMP"));
            _pool.Add(MakeGem(GemId.Chain, "Chain"));
            _pool.Add(MakeGem(GemId.FasterAttacks, "Faster"));
            _pool.Add(MakeGem(GemId.IncreasedAccuracy, "Accuracy"));
            _pool.Add(MakeGem(GemId.SlowerProjectiles, "Slower"));
            _pool.Add(MakeGem(GemId.AttackEcho, "Echo"));
        }

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
        public void BeginOffer_ProducesThreeUniqueGems()
        {
            var draft = new DraftService(new System.Random(1));
            draft.BeginOffer(_pool, allowSkip: true);

            Assert.AreEqual(3, draft.CurrentOffer.Count);
            Assert.IsTrue(draft.IsActive);
            Assert.AreNotSame(draft.CurrentOffer[0], draft.CurrentOffer[1]);
            Assert.AreNotSame(draft.CurrentOffer[1], draft.CurrentOffer[2]);
            Assert.AreNotSame(draft.CurrentOffer[0], draft.CurrentOffer[2]);
            Assert.AreNotEqual(draft.CurrentOffer[0].Id, draft.CurrentOffer[1].Id);
            Assert.AreNotEqual(draft.CurrentOffer[1].Id, draft.CurrentOffer[2].Id);
            Assert.AreNotEqual(draft.CurrentOffer[0].Id, draft.CurrentOffer[2].Id);
        }

        [Test]
        public void TrySkip_Starter_Rejected()
        {
            var draft = new DraftService(new System.Random(1));
            draft.BeginOffer(_pool, allowSkip: false);
            var economy = new RunEconomy(0, 20);

            Assert.IsFalse(draft.TrySkip(economy, 75, out _));
            Assert.AreEqual(0, economy.Gold);
            Assert.IsTrue(draft.IsActive);
        }

        [Test]
        public void TrySkip_MidRun_Grants75AndResolves()
        {
            var draft = new DraftService(new System.Random(1));
            draft.BeginOffer(_pool, allowSkip: true);
            var economy = new RunEconomy(0, 20);

            Assert.IsTrue(draft.TrySkip(economy, 75, out var resolved));
            Assert.IsTrue(resolved);
            Assert.AreEqual(75, economy.Gold);
            Assert.IsFalse(draft.IsActive);
        }

        [Test]
        public void TryPick_WhenFull_EntersReplaceConfirm_NoMeansStay()
        {
            var draft = new DraftService(new System.Random(1));
            draft.BeginOffer(_pool, allowSkip: true);
            var inventory = FillInventory(10);
            var first = draft.CurrentOffer[0];

            Assert.IsTrue(draft.TryPick(0, inventory, out var resolved));
            Assert.IsFalse(resolved);
            Assert.AreEqual(DraftReplacePhase.AwaitingConfirm, draft.ReplacePhase);
            Assert.AreSame(first, draft.PendingReplaceGem);

            draft.ConfirmReplaceNo();
            Assert.AreEqual(DraftReplacePhase.None, draft.ReplacePhase);
            Assert.IsNull(draft.PendingReplaceGem);
            Assert.IsTrue(draft.IsActive);
            Assert.AreEqual(3, draft.CurrentOffer.Count);
        }

        [Test]
        public void TryPick_Full_Yes_ThenDiscardSlot_AddsAndResolves()
        {
            var draft = new DraftService(new System.Random(1));
            draft.BeginOffer(_pool, allowSkip: true);
            var inventory = FillInventory(10);
            var picked = draft.CurrentOffer[1];

            Assert.IsTrue(draft.TryPick(1, inventory, out _));
            draft.ConfirmReplaceYes();
            Assert.AreEqual(DraftReplacePhase.AwaitingInventoryPick, draft.ReplacePhase);

            Assert.IsTrue(draft.TryCompleteReplace(0, inventory, out var resolved));
            Assert.IsTrue(resolved);
            Assert.IsFalse(draft.IsActive);

            var found = false;
            for (var i = 0; i < inventory.Slots.Count; i++)
            {
                if (ReferenceEquals(inventory.Slots[i], picked))
                    found = true;
            }

            Assert.IsTrue(found);
        }

        [Test]
        public void CancelReplace_AfterYes_DoesNotTakeCard()
        {
            var draft = new DraftService(new System.Random(2));
            draft.BeginOffer(_pool, allowSkip: true);
            var inventory = FillInventory(10);
            var beforeOccupied = inventory.OccupiedCount;

            Assert.IsTrue(draft.TryPick(0, inventory, out _));
            draft.ConfirmReplaceYes();
            draft.CancelReplace();

            Assert.AreEqual(DraftReplacePhase.None, draft.ReplacePhase);
            Assert.IsNull(draft.PendingReplaceGem);
            Assert.IsTrue(draft.IsActive);
            Assert.AreEqual(3, draft.CurrentOffer.Count);
            Assert.AreEqual(beforeOccupied, inventory.OccupiedCount);
        }

        [Test]
        public void TryPick_WithFreeSlot_AddsAndResolves()
        {
            var draft = new DraftService(new System.Random(3));
            draft.BeginOffer(_pool, allowSkip: false);
            var inventory = new GemInventory(10);
            var picked = draft.CurrentOffer[0];

            Assert.IsTrue(draft.TryPick(0, inventory, out var resolved));
            Assert.IsTrue(resolved);
            Assert.AreSame(picked, inventory.Slots[0]);
            Assert.IsFalse(draft.IsActive);
        }

        GemDefinition MakeGem(GemId id, string name)
        {
            var gem = ScriptableObject.CreateInstance<GemDefinition>();
            gem.Id = id;
            gem.DisplayName = name;
            _destroy.Add(gem);
            return gem;
        }

        GemInventory FillInventory(int capacity)
        {
            var inventory = new GemInventory(capacity);
            for (var i = 0; i < capacity; i++)
            {
                var filler = MakeGem(GemId.Lmp, "Fill" + i);
                Assert.IsTrue(inventory.TryAdd(filler));
            }

            return inventory;
        }
    }
}
