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
            var recipe = new TargetingRecipe
            {
                Priority1 = TargetingKey.MostArmor,
                Priority2 = TargetingKey.MostHpPct,
                Priority3 = TargetingKey.First
            };
            TargetingService.Apply(recipe, TargetingApplyScope.ThisTower, _towerA, _allTowers);

            Assert.AreEqual(recipe, _towerA.Targeting);
            Assert.AreEqual(TargetingRecipe.Default, _towerB.Targeting);
            Assert.AreEqual(TargetingRecipe.Default, _towerC.Targeting);
        }

        [Test]
        public void Apply_ThisType_UpdatesTowersWithSameDef()
        {
            var recipe = new TargetingRecipe
            {
                Priority1 = TargetingKey.Last,
                Priority2 = TargetingKey.First,
                Priority3 = TargetingKey.First
            };
            TargetingService.Apply(recipe, TargetingApplyScope.ThisType, _towerA, _allTowers);

            Assert.AreEqual(recipe, _towerA.Targeting);
            Assert.AreEqual(recipe, _towerB.Targeting);
            Assert.AreEqual(TargetingRecipe.Default, _towerC.Targeting);
        }

        [Test]
        public void Apply_AllTowers_WritesAllThreeKeys()
        {
            var recipe = new TargetingRecipe
            {
                Priority1 = TargetingKey.Slowest,
                Priority2 = TargetingKey.MostShield,
                Priority3 = TargetingKey.Last
            };
            TargetingService.Apply(recipe, TargetingApplyScope.AllTowers, _towerA, _allTowers);

            Assert.AreEqual(recipe, _towerA.Targeting);
            Assert.AreEqual(recipe, _towerB.Targeting);
            Assert.AreEqual(recipe, _towerC.Targeting);
        }

        [Test]
        public void Apply_DoesNothingWhenSelectedNull()
        {
            var recipe = new TargetingRecipe
            {
                Priority1 = TargetingKey.MostArmor,
                Priority2 = TargetingKey.First,
                Priority3 = TargetingKey.First
            };
            TargetingService.Apply(recipe, TargetingApplyScope.AllTowers, null, _allTowers);

            Assert.AreEqual(TargetingRecipe.Default, _towerA.Targeting);
            Assert.AreEqual(TargetingRecipe.Default, _towerB.Targeting);
            Assert.AreEqual(TargetingRecipe.Default, _towerC.Targeting);
        }

        [Test]
        public void Apply_DoesNothingWhenAllTowersNull()
        {
            var recipe = new TargetingRecipe
            {
                Priority1 = TargetingKey.MostArmor,
                Priority2 = TargetingKey.First,
                Priority3 = TargetingKey.First
            };
            TargetingService.Apply(recipe, TargetingApplyScope.AllTowers, _towerA, null);

            Assert.AreEqual(TargetingRecipe.Default, _towerA.Targeting);
        }
    }
}
