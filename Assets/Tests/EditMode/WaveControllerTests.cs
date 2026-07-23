using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Core;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Run;

namespace GemTD.Tests.EditMode
{
    public sealed class WaveControllerTests
    {
        RunClock _clock;
        RunStateMachine _states;
        RunEconomy _economy;
        EnemyDefinition _enemyDef;
        WaveDefinition _wave1;
        WaveDefinition _wave2;
        WaveDefinition _wave3;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
            _clock = new RunClock();
            _states = new RunStateMachine(_clock);
            _economy = new RunEconomy(0, 20);

            _enemyDef = ScriptableObject.CreateInstance<EnemyDefinition>();

            _wave1 = CreateWave(1, _enemyDef, count: 2, interval: 1f);
            _wave2 = CreateWave(2, _enemyDef, count: 3, interval: 0.5f);
            _wave3 = CreateWave(3, _enemyDef, count: 4, interval: 0.25f);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_wave1);
            UnityEngine.Object.DestroyImmediate(_wave2);
            UnityEngine.Object.DestroyImmediate(_wave3);
            UnityEngine.Object.DestroyImmediate(_enemyDef);
            GameEvents.ClearAll();
        }

        [Test]
        public void StartWave_FromPlan_TransitionsToCombatAndSetsWaveNumber()
        {
            var controller = CreateController(_wave1);
            EnterPlanReady();

            controller.StartWave();

            Assert.AreEqual(RunStateId.Combat, _states.Current);
            Assert.AreEqual(1, controller.CurrentWaveNumber);
        }

        [Test]
        public void Tick_SpawnsOnIntervalUntilQueueEmpty()
        {
            var controller = CreateController(_wave1);
            var gate = new TestSpawnerGate();
            EnterPlanReady();
            controller.StartWave();

            controller.Tick(0f, gate.Gate);
            Assert.AreEqual(1, gate.SpawnCount);

            controller.Tick(1f, gate.Gate);
            Assert.AreEqual(2, gate.SpawnCount);

            controller.Tick(1f, gate.Gate);
            Assert.AreEqual(2, gate.SpawnCount);
        }

        [Test]
        public void Tick_WhenQueueEmptyAndNoLiveEnemies_GrantsGoldAndClearsToPlan()
        {
            var controller = CreateController(new[] { _wave1 }, endWaveGold: 25);
            var gate = new TestSpawnerGate();
            EnterPlanReady();
            controller.StartWave();

            controller.Tick(0f, gate.Gate);
            controller.Tick(1f, gate.Gate);
            Assert.AreEqual(RunStateId.Combat, _states.Current);

            gate.ClearLive();
            controller.Tick(0f, gate.Gate);

            Assert.AreEqual(25, _economy.Gold);
            Assert.AreEqual(RunStateId.Plan, _states.Current);
        }

        [Test]
        public void Tick_DoesNotClearWhileEnemiesRemain()
        {
            var controller = CreateController(_wave1);
            var gate = new TestSpawnerGate();
            EnterPlanReady();
            controller.StartWave();

            controller.Tick(0f, gate.Gate);
            controller.Tick(1f, gate.Gate);

            controller.Tick(0f, gate.Gate);

            Assert.AreEqual(RunStateId.Combat, _states.Current);
            Assert.AreEqual(0, _economy.Gold);
        }

        [Test]
        public void StartWave_FlattensEntriesInSpawnOrder()
        {
            var enemyA = ScriptableObject.CreateInstance<EnemyDefinition>();
            var enemyB = ScriptableObject.CreateInstance<EnemyDefinition>();
            var wave = CreateWave(
                1,
                new[]
                {
                    new WaveSpawnEntry { Enemy = enemyA, Count = 2 },
                    new WaveSpawnEntry { Enemy = enemyB, Count = 1 },
                },
                interval: 0f);

            try
            {
                var controller = CreateController(wave);
                var gate = new TestSpawnerGate();
                EnterPlanReady();
                controller.StartWave();

                controller.Tick(0f, gate.Gate);
                controller.Tick(0f, gate.Gate);
                controller.Tick(0f, gate.Gate);

                Assert.AreEqual(3, gate.SpawnCount);
                Assert.AreEqual(enemyA, gate.SpawnedEnemies[0]);
                Assert.AreEqual(enemyA, gate.SpawnedEnemies[1]);
                Assert.AreEqual(enemyB, gate.SpawnedEnemies[2]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(wave);
                UnityEngine.Object.DestroyImmediate(enemyA);
                UnityEngine.Object.DestroyImmediate(enemyB);
            }
        }

        [Test]
        public void Clear_WaveWithOfferDraft_GoesDraft()
        {
            _wave1.OfferDraftAfterClear = true;
            var controller = CreateController(_wave1);
            var gate = new TestSpawnerGate();
            EnterPlanReady();
            ClearWave(controller, gate, expectedSpawns: 2);
            Assert.AreEqual(RunStateId.Draft, _states.Current);
        }

        [Test]
        public void Clear_FinalWave_GoesVictory_NoFurtherStartWave()
        {
            _wave1.EndsCampaign = true;
            var controller = CreateController(_wave1);
            var gate = new TestSpawnerGate();
            EnterPlanReady();
            ClearWave(controller, gate, expectedSpawns: 2);
            Assert.AreEqual(RunStateId.VictorySummary, _states.Current);
            Assert.Throws<InvalidOperationException>(() => controller.StartWave());
        }

        [Test]
        public void SixWaveFixture_DraftOn2And4_VictoryOn6()
        {
            var waves = new WaveDefinition[6];
            for (var i = 0; i < 6; i++)
            {
                waves[i] = CreateWave(i + 1, _enemyDef, count: 1, interval: 0f);
                waves[i].OfferDraftAfterClear = i == 1 || i == 3;
                waves[i].EndsCampaign = i == 5;
            }

            try
            {
                var controller = CreateController(waves);
                var gate = new TestSpawnerGate();

                for (var w = 0; w < 6; w++)
                {
                    if (_states.Current == RunStateId.Draft)
                        _states.DraftResolved();
                    if (_states.Current != RunStateId.Plan)
                        EnterPlanReady();
                    else if (!_states.ExpandSatisfiedThisCycle)
                        _states.NotifyExpandDone();

                    ClearWave(controller, gate, expectedSpawns: 1);

                    if (w == 1 || w == 3)
                        Assert.AreEqual(RunStateId.Draft, _states.Current);
                    else if (w == 5)
                        Assert.AreEqual(RunStateId.VictorySummary, _states.Current);
                    else
                        Assert.AreEqual(RunStateId.Plan, _states.Current);
                }
            }
            finally
            {
                for (var i = 0; i < waves.Length; i++)
                    UnityEngine.Object.DestroyImmediate(waves[i]);
            }
        }

        WaveController CreateController(params WaveDefinition[] waves) =>
            CreateController(waves, endWaveGold: 25);

        WaveController CreateController(WaveDefinition[] waves, int endWaveGold) =>
            new WaveController(waves, _states, _economy, endWaveGold);

        void EnterPlanReady()
        {
            if (_states.Current == RunStateId.Boot)
                _states.StartRun();
            if (_states.Current == RunStateId.Draft)
                _states.DraftResolved();
            if (_states.Current == RunStateId.Plan && !_states.ExpandSatisfiedThisCycle)
                _states.NotifyExpandDone();
        }

        void ClearWave(WaveController controller, TestSpawnerGate gate, int expectedSpawns)
        {
            var spawnsAtStart = gate.SpawnCount;
            controller.StartWave();
            var elapsed = 0f;
            while (gate.SpawnCount - spawnsAtStart < expectedSpawns)
            {
                controller.Tick(0.5f, gate.Gate);
                elapsed += 0.5f;
                if (elapsed > 30f)
                    Assert.Fail("Timed out waiting for spawns.");
            }

            gate.ClearLive();
            controller.Tick(0f, gate.Gate);
        }

        static WaveDefinition CreateWave(int number, EnemyDefinition enemy, int count, float interval) =>
            CreateWave(number, new[] { new WaveSpawnEntry { Enemy = enemy, Count = count } }, interval);

        static WaveDefinition CreateWave(int number, WaveSpawnEntry[] entries, float interval)
        {
            var wave = ScriptableObject.CreateInstance<WaveDefinition>();
            wave.WaveNumber = number;
            wave.Entries = entries;
            wave.SpawnInterval = interval;
            return wave;
        }

        sealed class TestSpawnerGate
        {
            readonly EnemyRegistry _registry = new EnemyRegistry();
            readonly List<EnemyDefinition> _spawnedEnemies = new List<EnemyDefinition>();
            public int SpawnCount { get; private set; }
            public IReadOnlyList<EnemyDefinition> SpawnedEnemies => _spawnedEnemies;
            public EnemySpawnerGate Gate { get; }

            public TestSpawnerGate()
            {
                Gate = new EnemySpawnerGate(SpawnEnemy, () => _registry.Count);
            }

            void SpawnEnemy(EnemyDefinition def)
            {
                SpawnCount++;
                _spawnedEnemies.Add(def);
                _registry.Register(new EnemyRuntime());
            }

            public void ClearLive()
            {
                while (_registry.Count > 0)
                    _registry.Unregister(_registry.GetAt(0));
            }

            public void ResetCounts()
            {
                SpawnCount = 0;
                _spawnedEnemies.Clear();
            }
        }
    }
}
