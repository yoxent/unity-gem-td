using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class TowerCostCalculatorTests
    {
        TowerDefinition _ballista;
        TowerDefinition _cannon;
        readonly List<TowerRuntime> _roster = new List<TowerRuntime>(8);

        [SetUp]
        public void SetUp()
        {
            _ballista = ScriptableObject.CreateInstance<TowerDefinition>();
            _ballista.DisplayName = "Ballista";
            _ballista.Cost = 50;
            _ballista.BuildIncrement = 25;

            _cannon = ScriptableObject.CreateInstance<TowerDefinition>();
            _cannon.DisplayName = "Cannon";
            _cannon.Cost = 60;
            _cannon.BuildIncrement = 30;

            _roster.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _roster.Clear();
            Object.DestroyImmediate(_ballista);
            Object.DestroyImmediate(_cannon);
        }

        [Test]
        public void FirstTower_UsesBaseCost()
        {
            Assert.AreEqual(50, TowerCostCalculator.ComputePlaceCost(_ballista, _roster));
        }

        [Test]
        public void SecondSameType_AddsOneIncrement()
        {
            AddTower(_ballista, 50);
            Assert.AreEqual(75, TowerCostCalculator.ComputePlaceCost(_ballista, _roster));
        }

        [Test]
        public void ThirdSameType_AddsTwoIncrements()
        {
            AddTower(_ballista, 50);
            AddTower(_ballista, 75);
            Assert.AreEqual(100, TowerCostCalculator.ComputePlaceCost(_ballista, _roster));
        }

        [Test]
        public void OtherTypes_DoNotAffectCost()
        {
            AddTower(_ballista, 50);
            Assert.AreEqual(60, TowerCostCalculator.ComputePlaceCost(_cannon, _roster));
        }

        [Test]
        public void SellLowersNextBuildCost_ForSameType()
        {
            AddTower(_ballista, 50);
            AddTower(_ballista, 75);
            AddTower(_ballista, 100);
            _roster.RemoveAt(2);
            Assert.AreEqual(75, TowerCostCalculator.ComputePlaceCost(_ballista, _roster));
        }

        [Test]
        public void PerTowerIncrement_AppliesFromDefinition()
        {
            AddTower(_cannon, 60);
            Assert.AreEqual(90, TowerCostCalculator.ComputePlaceCost(_cannon, _roster));
        }

        void AddTower(TowerDefinition def, int purchaseCost)
        {
            _roster.Add(new TowerRuntime(new Vector2Int(_roster.Count, 0), def, purchaseCost));
        }
    }
}
