using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class TowerCostCalculatorTests
    {
        TowerDefinition _firstTower;
        TowerDefinition _secondTower;
        readonly List<TowerInstance> _roster = new List<TowerInstance>(8);

        [SetUp]
        public void SetUp()
        {
            _firstTower = ScriptableObject.CreateInstance<TowerDefinition>();
            _firstTower.DisplayName = "First Tower";
            _firstTower.Cost = 50;
            _firstTower.BuildIncrement = 25;

            _secondTower = ScriptableObject.CreateInstance<TowerDefinition>();
            _secondTower.DisplayName = "Second Tower";
            _secondTower.Cost = 60;
            _secondTower.BuildIncrement = 30;

            _roster.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _roster.Clear();
            Object.DestroyImmediate(_firstTower);
            Object.DestroyImmediate(_secondTower);
        }

        [Test]
        public void FirstTower_UsesBaseCost()
        {
            Assert.AreEqual(50, TowerCostCalculator.ComputePlaceCost(_firstTower, _roster));
        }

        [Test]
        public void SecondSameType_AddsOneIncrement()
        {
            AddTower(_firstTower, 50);
            Assert.AreEqual(75, TowerCostCalculator.ComputePlaceCost(_firstTower, _roster));
        }

        [Test]
        public void ThirdSameType_AddsTwoIncrements()
        {
            AddTower(_firstTower, 50);
            AddTower(_firstTower, 75);
            Assert.AreEqual(100, TowerCostCalculator.ComputePlaceCost(_firstTower, _roster));
        }

        [Test]
        public void OtherTypes_DoNotAffectCost()
        {
            AddTower(_firstTower, 50);
            Assert.AreEqual(60, TowerCostCalculator.ComputePlaceCost(_secondTower, _roster));
        }

        [Test]
        public void SellLowersNextBuildCost_ForSameType()
        {
            AddTower(_firstTower, 50);
            AddTower(_firstTower, 75);
            AddTower(_firstTower, 100);
            Assert.AreEqual(125, TowerCostCalculator.ComputePlaceCost(_firstTower, _roster));
            _roster.RemoveAt(2);
            Assert.AreEqual(100, TowerCostCalculator.ComputePlaceCost(_firstTower, _roster));
        }

        [Test]
        public void PerTowerIncrement_AppliesFromDefinition()
        {
            AddTower(_secondTower, 60);
            Assert.AreEqual(90, TowerCostCalculator.ComputePlaceCost(_secondTower, _roster));
        }

        void AddTower(TowerDefinition def, int purchaseCost)
        {
            _roster.Add(new TowerInstance(new Vector2Int(_roster.Count, 0), def, purchaseCost));
        }
    }
}
