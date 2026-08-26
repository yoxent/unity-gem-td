using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class TowerInstanceTests
    {
        TowerDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<TowerDefinition>();
            _definition.SocketCount = 2;
            _definition.Cost = 75;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_definition);
        }

        [Test]
        public void NewInstances_StartAtIndependentLevelOne()
        {
            var first = new TowerInstance(Vector2Int.zero, _definition);
            var second = new TowerInstance(new Vector2Int(1, 0), _definition);

            first.SetLevel(10);

            Assert.AreEqual(10, first.Level);
            Assert.AreEqual(1, second.Level);
            Assert.AreEqual(1, new TowerInstance(Vector2Int.zero, _definition).Level);
        }

        [Test]
        public void SetLevel_ClampsToOneThroughTen()
        {
            var tower = new TowerInstance(Vector2Int.zero, _definition);

            tower.SetLevel(0);

            Assert.AreEqual(1, tower.Level);

            tower.SetLevel(99);

            Assert.AreEqual(10, tower.Level);
        }

        [Test]
        public void Constructor_PreservesPlacementState()
        {
            var tower = new TowerInstance(new Vector2Int(2, 3), _definition, purchaseCost: 90);

            Assert.AreEqual(new Vector2Int(2, 3), tower.Cell);
            Assert.AreSame(_definition, tower.Def);
            Assert.AreEqual(2, tower.Sockets.Length);
            Assert.AreEqual(90, tower.PurchaseCost);
            Assert.AreEqual(0, tower.UpgradeSpend);
            Assert.AreEqual(TargetingRecipe.Default, tower.Targeting);
        }

        [Test]
        public void TrySocket_AllowsTwoDifferentNoneGems()
        {
            var tower = new TowerInstance(Vector2Int.zero, _definition);
            var first = ScriptableObject.CreateInstance<GemDefinition>();
            first.Id = GemId.None;
            first.Tags = GemTag.Support;
            var second = ScriptableObject.CreateInstance<GemDefinition>();
            second.Id = GemId.None;
            second.Tags = GemTag.Support;
            try
            {
                Assert.IsTrue(tower.TrySocket(first, 0, allowSocket: true));
                Assert.IsTrue(tower.TrySocket(second, 1, allowSocket: true));
                Assert.AreSame(first, tower.Sockets[0]);
                Assert.AreSame(second, tower.Sockets[1]);
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void TrySocket_RejectsSameNoneAssetTwice()
        {
            var tower = new TowerInstance(Vector2Int.zero, _definition);
            var gem = ScriptableObject.CreateInstance<GemDefinition>();
            gem.Id = GemId.None;
            gem.Tags = GemTag.Support;
            try
            {
                Assert.IsTrue(tower.TrySocket(gem, 0, allowSocket: true));
                Assert.IsFalse(tower.TrySocket(gem, 1, allowSocket: true));
            }
            finally
            {
                Object.DestroyImmediate(gem);
            }
        }

        [Test]
        public void LevelIndex_DrivesCombatLevelClampedAtTen()
        {
            var tower = new TowerInstance(Vector2Int.zero, _definition);
            tower.LevelIndex = 4;
            Assert.AreEqual(4, tower.LevelIndex);
            Assert.AreEqual(5, tower.Level);

            tower.LevelIndex = 20;
            Assert.AreEqual(20, tower.LevelIndex);
            Assert.AreEqual(10, tower.Level);
        }
    }
}
