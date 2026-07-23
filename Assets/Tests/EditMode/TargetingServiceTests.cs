using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class TargetingServiceTests
    {
        TowerDefinition _sharedDef;
        TowerDefinition _otherDef;
        TowerRuntime _towerA;
        TowerRuntime _towerB;
        TowerRuntime _towerC;
        List<TowerRuntime> _allTowers;

        [SetUp]
        public void SetUp()
        {
            _sharedDef = ScriptableObject.CreateInstance<TowerDefinition>();
            _sharedDef.DisplayName = "Shared";

            _otherDef = ScriptableObject.CreateInstance<TowerDefinition>();
            _otherDef.DisplayName = "Other";

            _towerA = new TowerRuntime(new Vector2Int(0, 0), _sharedDef);
            _towerB = new TowerRuntime(new Vector2Int(1, 0), _sharedDef);
            _towerC = new TowerRuntime(new Vector2Int(2, 0), _otherDef);

            _allTowers = new List<TowerRuntime> { _towerA, _towerB, _towerC };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_sharedDef);
            Object.DestroyImmediate(_otherDef);
        }

        [Test]
        public void Apply_ThisTower_UpdatesSelectedOnly()
        {
            TargetingService.Apply(TargetingMode.Closest, TargetingApplyScope.ThisTower, _towerA, _allTowers);

            Assert.AreEqual(TargetingMode.Closest, _towerA.TargetingMode);
            Assert.AreEqual(TargetingMode.First, _towerB.TargetingMode);
            Assert.AreEqual(TargetingMode.First, _towerC.TargetingMode);
        }

        [Test]
        public void Apply_ThisType_UpdatesTowersWithSameDef()
        {
            TargetingService.Apply(TargetingMode.Last, TargetingApplyScope.ThisType, _towerA, _allTowers);

            Assert.AreEqual(TargetingMode.Last, _towerA.TargetingMode);
            Assert.AreEqual(TargetingMode.Last, _towerB.TargetingMode);
            Assert.AreEqual(TargetingMode.First, _towerC.TargetingMode);
        }

        [Test]
        public void Apply_AllTowers_UpdatesEveryTower()
        {
            TargetingService.Apply(TargetingMode.Strongest, TargetingApplyScope.AllTowers, _towerA, _allTowers);

            Assert.AreEqual(TargetingMode.Strongest, _towerA.TargetingMode);
            Assert.AreEqual(TargetingMode.Strongest, _towerB.TargetingMode);
            Assert.AreEqual(TargetingMode.Strongest, _towerC.TargetingMode);
        }

        [Test]
        public void Apply_DoesNothingWhenSelectedNull()
        {
            TargetingService.Apply(TargetingMode.Closest, TargetingApplyScope.AllTowers, null, _allTowers);

            Assert.AreEqual(TargetingMode.First, _towerA.TargetingMode);
            Assert.AreEqual(TargetingMode.First, _towerB.TargetingMode);
            Assert.AreEqual(TargetingMode.First, _towerC.TargetingMode);
        }

        [Test]
        public void Apply_DoesNothingWhenAllTowersNull()
        {
            TargetingService.Apply(TargetingMode.Closest, TargetingApplyScope.AllTowers, _towerA, null);

            Assert.AreEqual(TargetingMode.First, _towerA.TargetingMode);
        }
    }
}
