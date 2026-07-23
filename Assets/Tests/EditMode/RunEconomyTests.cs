using NUnit.Framework;
using UnityEngine;
using GemTD.Core;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Run;

namespace GemTD.Tests.EditMode
{
    public sealed class RunEconomyTests
    {
        [SetUp]
        [TearDown]
        public void ClearEvents() => GameEvents.ClearAll();

        [Test]
        public void TrySpend_SucceedsWhenAffordable()
        {
            var economy = new RunEconomy(100, 20);
            var reportedGold = -1;
            GameEvents.GoldChanged += g => reportedGold = g;

            Assert.IsTrue(economy.TrySpend(30));
            Assert.AreEqual(70, economy.Gold);
            Assert.AreEqual(70, reportedGold);
        }

        [Test]
        public void TrySpend_FailsWhenInsufficient()
        {
            var economy = new RunEconomy(10, 20);
            var reported = false;
            GameEvents.GoldChanged += _ => reported = true;

            Assert.IsFalse(economy.TrySpend(15));
            Assert.AreEqual(10, economy.Gold);
            Assert.IsFalse(reported);
        }

        [Test]
        public void RefundFull_AddsGoldAndRaisesEvent()
        {
            var economy = new RunEconomy(50, 20);
            var reportedGold = -1;
            GameEvents.GoldChanged += g => reportedGold = g;

            economy.RefundFull(25);

            Assert.AreEqual(75, economy.Gold);
            Assert.AreEqual(75, reportedGold);
        }

        [Test]
        public void LoseLife_ReducesLivesAndRaisesEvent()
        {
            var economy = new RunEconomy(100, 20);
            var reportedLives = -1;
            GameEvents.LivesChanged += l => reportedLives = l;

            economy.LoseLife();

            Assert.AreEqual(19, economy.Lives);
            Assert.AreEqual(19, reportedLives);
            Assert.IsFalse(economy.IsDefeated);
        }

        [Test]
        public void LoseLife_WhenLivesReachZero_SetsDefeated()
        {
            var economy = new RunEconomy(100, 1);

            economy.LoseLife();

            Assert.AreEqual(0, economy.Lives);
            Assert.IsTrue(economy.IsDefeated);
        }

        [Test]
        public void LoseLife_AppliesLeakDamageAmount()
        {
            var economy = new RunEconomy(100, 20);

            economy.LoseLife(5);

            Assert.AreEqual(15, economy.Lives);
            Assert.IsFalse(economy.IsDefeated);
        }

        [Test]
        public void GrantKillGold_AddsGold()
        {
            var economy = new RunEconomy(0, 20);
            economy.GrantKillGold(5);
            Assert.AreEqual(5, economy.Gold);
        }

        [Test]
        public void GrantEndWaveGold_AddsGold()
        {
            var economy = new RunEconomy(0, 20);
            economy.GrantEndWaveGold(25);
            Assert.AreEqual(25, economy.Gold);
        }

        [Test]
        public void RunConfig_HasLockedDefaults()
        {
            var config = ScriptableObject.CreateInstance<RunConfig>();
            Assert.AreEqual(100, config.StartingGold);
            Assert.AreEqual(20, config.StartingLives);
            Assert.AreEqual(25, config.EndWaveGold);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void EnemyDefinition_KillGoldDefaultIsFive()
        {
            var enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            Assert.AreEqual(5, enemy.KillGold);
            Object.DestroyImmediate(enemy);
        }
    }
}
