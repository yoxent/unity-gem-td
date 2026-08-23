using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
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
        public void NewInstances_StartAtIndependentLevelTwenty()
        {
            var first = new TowerInstance(Vector2Int.zero, _definition);
            var second = new TowerInstance(new Vector2Int(1, 0), _definition);

            first.SetLevel(24);

            Assert.AreEqual(24, first.Level);
            Assert.AreEqual(20, second.Level);
            Assert.AreEqual(20, new TowerInstance(Vector2Int.zero, _definition).Level);
        }

        [Test]
        public void SetLevel_ClampsToPositiveSourceLevel()
        {
            var tower = new TowerInstance(Vector2Int.zero, _definition);

            tower.SetLevel(0);

            Assert.AreEqual(1, tower.Level);
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
    }
}
