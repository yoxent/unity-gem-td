using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Balance;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class BalanceCalculatorTests
    {
        EnemyDefinition _enemy;

        [SetUp]
        public void SetUp()
        {
            _enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            _enemy.MaxHealth = 20f;
            _enemy.MoveSpeed = 2f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemy);
        }

        [Test]
        public void Evaluate_FixedDamage_ReportsHitsAndTime()
        {
            var spec = SkillSpec.FromBase(10f);

            var result = BalanceCalculator.Evaluate(spec, 1f, _enemy);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(10f, result.EffectiveDamageMin, 1e-4f);
            Assert.AreEqual(10f, result.EffectiveDamageMax, 1e-4f);
            Assert.AreEqual(2, result.BestCaseActionsToKill);
            Assert.AreEqual(2, result.WorstCaseActionsToKill);
            Assert.AreEqual(1f, result.BestCaseTimeToKill, 1e-4f);
            Assert.AreEqual(1f, result.WorstCaseTimeToKill, 1e-4f);
        }

        [Test]
        public void Evaluate_DamageRange_ReportsBestAndWorstCases()
        {
            var spec = SkillSpec.FromBase(8f, 10f);

            var result = BalanceCalculator.Evaluate(spec, 0.5f, _enemy);

            Assert.AreEqual(10f, result.EffectiveDamageMax, 1e-4f);
            Assert.AreEqual(8f, result.EffectiveDamageMin, 1e-4f);
            Assert.AreEqual(2, result.BestCaseActionsToKill);
            Assert.AreEqual(3, result.WorstCaseActionsToKill);
            Assert.AreEqual(0.5f, result.BestCaseTimeToKill, 1e-4f);
            Assert.AreEqual(1f, result.WorstCaseTimeToKill, 1e-4f);
        }

        [Test]
        public void Evaluate_HealthScale_ReportsScaledHealthAndActions()
        {
            var spec = SkillSpec.FromBase(10f);

            var result = BalanceCalculator.Evaluate(spec, 1f, _enemy, healthScale: 1.08f);

            Assert.AreEqual(21.6f, result.ScaledHealth, 1e-4f);
            Assert.AreEqual(3, result.WorstCaseActionsToKill);
        }

        [Test]
        public void EvaluatePayloads_SumsValidPayloadDamage()
        {
            var spec = SkillSpec.FromBase(10f);
            var payload = new EffectPayloadDefinition
            {
                Count = 2,
                DamageMultiplier = 1.5f,
                AoeRadius = 2f
            };

            var result = BalanceCalculator.EvaluatePayloads(
                spec,
                spec,
                new[] { payload });

            Assert.AreEqual(2, result.PayloadCount);
            Assert.AreEqual(30f, result.TotalRawDamageMin, 1e-4f);
            Assert.AreEqual(30f, result.TotalRawDamageMax, 1e-4f);
            Assert.AreEqual(2f, result.MaxAoeRadius, 1e-4f);
        }

        [Test]
        public void Evaluate_UntypedArmor_UsesFlatMitigation()
        {
            _enemy.MaxHealth = 40f;
            _enemy.Armor = 5;
            var spec = SkillSpec.FromBase(10f);

            var result = BalanceCalculator.Evaluate(spec, 1f, _enemy);

            Assert.AreEqual(5f, result.EffectiveDamageMin, 1e-4f);
            Assert.AreEqual(8, result.WorstCaseActionsToKill);
            Assert.AreEqual(7f, result.WorstCaseTimeToKill, 1e-4f);
        }

        [Test]
        public void Evaluate_TypedPhysicalArmor_UsesNonlinearMitigation()
        {
            _enemy.MaxHealth = 40f;
            _enemy.Armor = 5;
            var spec = SkillSpec.FromBase(10f);
            spec.MixPhysical = 1f;

            var result = BalanceCalculator.Evaluate(spec, 1f, _enemy);

            var expectedDamage = 10f * (1f - 5f / (5f + 5f * 10f));
            Assert.AreEqual(expectedDamage, result.EffectiveDamageMin, 1e-4f);
            Assert.AreEqual(5, result.WorstCaseActionsToKill);
        }

        [Test]
        public void EvaluateWave_SumsEntriesAndAppliesHealthScale()
        {
            var armored = ScriptableObject.CreateInstance<EnemyDefinition>();
            armored.MaxHealth = 40f;
            armored.LeakDamage = 2;
            armored.KillGold = 8;

            var wave = ScriptableObject.CreateInstance<WaveDefinition>();
            wave.WaveNumber = 2;
            wave.SpawnInterval = 0.4f;
            wave.Entries = new[]
            {
                new WaveSpawnEntry { Enemy = _enemy, Count = 4 },
                new WaveSpawnEntry { Enemy = armored, Count = 2 }
            };
            _enemy.LeakDamage = 1;
            _enemy.KillGold = 5;

            var result = BalanceCalculator.EvaluateWave(wave, 1.08f);

            Assert.AreEqual(6, result.EnemyCount);
            Assert.AreEqual(160f, result.BaseHealth, 1e-4f);
            Assert.AreEqual(172.8f, result.ScaledHealth, 1e-4f);
            Assert.AreEqual(8, result.TotalLeakDamage);
            Assert.AreEqual(36, result.TotalKillGold);
            Assert.AreEqual(2f, result.SpawnDuration, 1e-4f);

            Object.DestroyImmediate(wave);
            Object.DestroyImmediate(armored);
        }

        [Test]
        public void ComputeNextTowerCost_AppliesSameTypeIncrement()
        {
            var tower = ScriptableObject.CreateInstance<TowerDefinition>();
            tower.Cost = 50;
            tower.BuildIncrement = 25;

            Assert.AreEqual(100, BalanceCalculator.ComputeNextTowerCost(tower, 2));

            Object.DestroyImmediate(tower);
        }

        [Test]
        public void ComputeGoldBeforeWave_IncludesPreviousKillsAndClearReward()
        {
            var config = ScriptableObject.CreateInstance<RunConfig>();
            config.StartingGold = 100;
            config.EndWaveGold = 50;

            var wave = ScriptableObject.CreateInstance<WaveDefinition>();
            wave.WaveNumber = 1;
            wave.Entries = new[]
            {
                new WaveSpawnEntry { Enemy = _enemy, Count = 4 }
            };
            _enemy.KillGold = 5;

            Assert.AreEqual(
                170,
                BalanceCalculator.ComputeGoldBeforeWave(
                    config,
                    new[] { wave },
                    2));

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(wave);
        }
    }
}
