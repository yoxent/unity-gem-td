using System;
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
        public void StartWave_FromBuild_TransitionsToCombatAndSetsWaveNumber()
        {
            var controller = CreateController(_wave1);
            EnterBuild();

            controller.StartWave();

            Assert.AreEqual(RunStateId.Combat, _states.Current);
            Assert.AreEqual(1, controller.CurrentWaveNumber);
        }

        [Test]
        public void Tick_SpawnsOnIntervalUntilQueueEmpty()
        {
            var controller = CreateController(_wave1);
            var gate = new TestSpawnerGate();
            EnterBuild();
            controller.StartWave();

            controller.Tick(0f, gate.Gate);
            Assert.AreEqual(1, gate.SpawnCount);

            controller.Tick(1f, gate.Gate);
            Assert.AreEqual(2, gate.SpawnCount);

            controller.Tick(1f, gate.Gate);
            Assert.AreEqual(2, gate.SpawnCount);
        }

        [Test]
        public void Tick_WhenQueueEmptyAndNoLiveEnemies_GrantsGoldAndClearsWave()
        {
            var controller = CreateController(new[] { _wave1 }, endWaveGold: 25);
            var gate = new TestSpawnerGate();
            EnterBuild();
            controller.StartWave();

            controller.Tick(0f, gate.Gate);
            controller.Tick(1f, gate.Gate);
            Assert.AreEqual(RunStateId.Combat, _states.Current);

            gate.ClearLive();
            controller.Tick(0f, gate.Gate);

            Assert.AreEqual(25, _economy.Gold);
            Assert.AreEqual(RunStateId.Expand, _states.Current);
        }

        [Test]
        public void Tick_DoesNotClearWhileEnemiesRemain()
        {
            var controller = CreateController(_wave1);
            var gate = new TestSpawnerGate();
            EnterBuild();
            controller.StartWave();

            controller.Tick(0f, gate.Gate);
            controller.Tick(1f, gate.Gate);

            controller.Tick(0f, gate.Gate);

            Assert.AreEqual(RunStateId.Combat, _states.Current);
            Assert.AreEqual(0, _economy.Gold);
        }

        [Test]
        public void StartWave_AfterWaveThree_ReusesLastDefinition()
        {
            var controller = CreateController(_wave1, _wave2, _wave3);
            var gate = new TestSpawnerGate();
            EnterBuild();

            ClearWave(controller, gate, expectedSpawns: 2);
            EnterBuild();
            ClearWave(controller, gate, expectedSpawns: 3);
            EnterBuild();
            ClearWave(controller, gate, expectedSpawns: 4);
            EnterBuild();

            gate.ResetCounts();
            controller.StartWave();
            Assert.AreEqual(4, controller.CurrentWaveNumber);

            controller.Tick(0f, gate.Gate);
            controller.Tick(0.25f, gate.Gate);
            controller.Tick(0.25f, gate.Gate);
            controller.Tick(0.25f, gate.Gate);
            Assert.AreEqual(4, gate.SpawnCount);
        }

        WaveController CreateController(params WaveDefinition[] waves) =>
            CreateController(waves, endWaveGold: 25);

        WaveController CreateController(WaveDefinition[] waves, int endWaveGold) =>
            new WaveController(waves, _states, _economy, endWaveGold);

        void EnterBuild()
        {
            _states.StartRun();
            _states.ExpandConfirmed();
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
            Assert.AreEqual(RunStateId.Expand, _states.Current);
        }

        static WaveDefinition CreateWave(int number, EnemyDefinition enemy, int count, float interval)
        {
            var wave = ScriptableObject.CreateInstance<WaveDefinition>();
            wave.WaveNumber = number;
            wave.Enemy = enemy;
            wave.Count = count;
            wave.SpawnInterval = interval;
            return wave;
        }

        sealed class TestSpawnerGate
        {
            readonly EnemyRegistry _registry = new EnemyRegistry();
            public int SpawnCount { get; private set; }
            public EnemySpawnerGate Gate { get; }

            public TestSpawnerGate()
            {
                Gate = new EnemySpawnerGate(SpawnEnemy, () => _registry.Count);
            }

            void SpawnEnemy(EnemyDefinition def)
            {
                SpawnCount++;
                _registry.Register(new EnemyRuntime());
            }

            public void ClearLive()
            {
                while (_registry.Count > 0)
                    _registry.Unregister(_registry.GetAt(0));
            }

            public void ResetCounts() => SpawnCount = 0;
        }
    }
}
