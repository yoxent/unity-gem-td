using System;
using System.Collections.Generic;
using System.IO;
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
            _states = new RunStateMachine(new SpeedControl(_clock), _clock);
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
            PlayerProfile.ResetForTests();
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
        public void NextWaveNumber_IsOneBeforeStart_ThenTwoAfterClear()
        {
            var controller = CreateController(_wave1, _wave2);
            Assert.AreEqual(1, controller.NextWaveNumber);

            var gate = new TestSpawnerGate();
            EnterPlanReady();
            controller.StartWave();
            Assert.AreEqual(1, controller.NextWaveNumber);

            controller.Tick(0f, gate.Gate);
            controller.Tick(1f, gate.Gate);
            gate.ClearLive();
            controller.Tick(0f, gate.Gate);

            Assert.AreEqual(2, controller.NextWaveNumber);
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
        public void BuildSpawnQueue_SkipsAuthoredBossEntries_RegardlessOfBossEnemyWiring()
        {
            var authoredBoss = ScriptableObject.CreateInstance<EnemyDefinition>();
            authoredBoss.IsBoss = true;
            var wave = CreateWave(
                1,
                new[]
                {
                    new WaveSpawnEntry { Enemy = _enemyDef, Count = 2 },
                    new WaveSpawnEntry { Enemy = authoredBoss, Count = 1 },
                },
                interval: 0f);

            try
            {
                // No bossEnemy wired, and wave 1 isn't a cadence boss wave — authored boss
                // entry must still never reach the spawn queue (cadence owns all bosses).
                var controller = CreateController(new[] { wave }, endWaveGold: 25, bossEnemy: null);
                var gate = new TestSpawnerGate();
                EnterPlanReady();
                controller.StartWave(spawnTipCount: 1);

                Assert.AreEqual(0, controller.CurrentBossCount);
                controller.Tick(0f, gate.Gate);

                Assert.AreEqual(2, gate.SpawnCount);
                Assert.AreEqual(_enemyDef, gate.SpawnedEnemies[0]);
                Assert.AreEqual(_enemyDef, gate.SpawnedEnemies[1]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(wave);
                UnityEngine.Object.DestroyImmediate(authoredBoss);
            }
        }

        [Test]
        public void StartWave_NonBossWave_CurrentBossCountIsZero_EvenWithBossEnemyWired()
        {
            var boss = ScriptableObject.CreateInstance<EnemyDefinition>();
            boss.IsBoss = true;
            try
            {
                var controller = CreateController(new[] { _wave1 }, endWaveGold: 25, bossEnemy: boss);
                EnterPlanReady();

                controller.StartWave(spawnTipCount: 4);

                Assert.AreEqual(0, controller.CurrentBossCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(boss);
            }
        }

        [Test]
        public void StartWave_BossWaveTen_InjectsOneCadenceBossAfterRegulars()
        {
            var boss = ScriptableObject.CreateInstance<EnemyDefinition>();
            boss.IsBoss = true;
            var waves = BuildWaveArrayUpTo(10, regularCountForLastWave: 2);

            try
            {
                var controller = CreateController(waves, endWaveGold: 25, bossEnemy: boss);
                var gate = new TestSpawnerGate();
                AdvanceThroughWaves(controller, gate, waveCount: 9);

                EnterPlanReady();
                controller.StartWave(spawnTipCount: 3); // min(10/10, 3) = 1 boss
                Assert.AreEqual(1, controller.CurrentBossCount);

                gate.ResetCounts();
                controller.Tick(0f, gate.Gate); // interval 0 — one Tick drains the queue

                Assert.AreEqual(3, gate.SpawnCount); // 2 regulars + 1 boss
                Assert.AreEqual(_enemyDef, gate.SpawnedEnemies[0]);
                Assert.AreEqual(_enemyDef, gate.SpawnedEnemies[1]);
                Assert.AreEqual(boss, gate.SpawnedEnemies[2]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(boss);
                for (var i = 0; i < waves.Length; i++)
                    UnityEngine.Object.DestroyImmediate(waves[i]);
            }
        }

        [Test]
        public void StartWave_BossWaveTwenty_InjectsTwoBosses_CappedAtLiveTipCount()
        {
            var boss = ScriptableObject.CreateInstance<EnemyDefinition>();
            boss.IsBoss = true;
            var waves = BuildWaveArrayUpTo(20, regularCountForLastWave: 1);

            try
            {
                var controller = CreateController(waves, endWaveGold: 25, bossEnemy: boss);
                var gate = new TestSpawnerGate();
                AdvanceThroughWaves(controller, gate, waveCount: 19);

                EnterPlanReady();
                // Design: min(wave/10, tipCount). Wave 20 wants 2 but only 1 tip exists.
                controller.StartWave(spawnTipCount: 1);
                Assert.AreEqual(1, controller.CurrentBossCount);

                gate.ResetCounts();
                controller.Tick(0f, gate.Gate);

                Assert.AreEqual(2, gate.SpawnCount); // 1 regular + 1 boss (capped)
                Assert.AreEqual(boss, gate.SpawnedEnemies[1]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(boss);
                for (var i = 0; i < waves.Length; i++)
                    UnityEngine.Object.DestroyImmediate(waves[i]);
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
        public void Clear_EndsCampaignFalse_DoesNotVictory()
        {
            _wave1.EndsCampaign = false;
            var controller = CreateController(new[] { _wave1 }, endWaveGold: 25, endWave: 50);
            var gate = new TestSpawnerGate();
            EnterPlanReady();
            ClearWave(controller, gate, expectedSpawns: 2);
            Assert.AreEqual(RunStateId.Plan, _states.Current);
        }

        [Test]
        public void BeyondCatalog_ReusesLastTemplate_UntilEndWave_ThenVictory()
        {
            // 2 authored waves; EndWave=4 → waves 3–4 reuse last template; clear 4 → Victory.
            var controller = CreateController(new[] { _wave1, _wave2 }, endWaveGold: 10, endWave: 4);
            var gate = new TestSpawnerGate();

            EnterPlanReady();
            ClearWave(controller, gate, expectedSpawns: 2); // wave 1
            Assert.AreEqual(RunStateId.Plan, _states.Current);

            EnterPlanReady();
            ClearWave(controller, gate, expectedSpawns: 3); // wave 2
            Assert.AreEqual(RunStateId.Plan, _states.Current);

            EnterPlanReady();
            ClearWave(controller, gate, expectedSpawns: 3); // wave 3 (reuse wave2)
            Assert.AreEqual(3, controller.CurrentWaveNumber);
            Assert.AreEqual(RunStateId.Plan, _states.Current);

            EnterPlanReady();
            ClearWave(controller, gate, expectedSpawns: 3); // wave 4 → Victory
            Assert.AreEqual(4, controller.CurrentWaveNumber);
            Assert.AreEqual(RunStateId.VictorySummary, _states.Current);
            Assert.Throws<InvalidOperationException>(() => controller.StartWave());
        }

        [Test]
        public void Clear_AtEndWave_WithOfferDraft_GoesVictory_NotDraft()
        {
            _wave1.OfferDraftAfterClear = true;
            var capped = false;
            var controller = new WaveController(
                new[] { _wave1 }, _states, _economy, 25, null, endWave: 1, () => capped = true);
            var gate = new TestSpawnerGate();

            EnterPlanReady();
            ClearWave(controller, gate, expectedSpawns: 2);
            Assert.IsTrue(capped);
            Assert.AreEqual(RunStateId.VictorySummary, _states.Current);
        }

        [Test]
        public void Endless_AllowsWavesPastEndWave_NoVictory()
        {
            var boss = ScriptableObject.CreateInstance<EnemyDefinition>();
            boss.IsBoss = true;
            try
            {
                var controller = new WaveController(
                    new[] { _wave1 }, _states, _economy, 20, boss, endWave: 1);
                var gate = new TestSpawnerGate();

                EnterPlanReady();
                ClearWave(controller, gate, expectedSpawns: 2);
                Assert.AreEqual(RunStateId.VictorySummary, _states.Current);

                controller.BeginEndless();
                _states.EnterEndless();
                Assert.IsTrue(controller.IsEndless);

                // Wave 2 past EndWave=1 — default tipCount 1 → 1 boss + 2 regulars.
                ClearWave(controller, gate, expectedSpawns: 2 + 1);
                Assert.AreEqual(2, controller.CurrentWaveNumber);
                Assert.AreEqual(1, controller.CurrentBossCount);
                Assert.AreNotEqual(RunStateId.VictorySummary, _states.Current);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(boss);
            }
        }

        [Test]
        public void Endless_Clear_DoesNotVictory_AndHalvesEndWaveGold()
        {
            var controller = new WaveController(
                new[] { _wave1 }, _states, _economy, 100, null, endWave: 1);
            var gate = new TestSpawnerGate();

            EnterPlanReady();
            ClearWave(controller, gate, expectedSpawns: 2);
            // Wave 1 end-gold (campaign): 100
            Assert.AreEqual(100, _economy.Gold);

            controller.BeginEndless();
            _states.EnterEndless();
            ClearWave(controller, gate, expectedSpawns: 2);
            // Wave 2 endless: ScaleEndWaveGold(100,2)=108, then ×0.5 → 54
            Assert.AreEqual(100 + 54, _economy.Gold);
            Assert.AreEqual(RunStateId.Plan, _states.Current);
        }

        [Test]
        public void Endless_Clear_UpdatesHighestWave()
        {
            var path = Path.Combine(Path.GetTempPath(), "gemtd-wave-profile-" + Path.GetRandomFileName() + ".json");
            PlayerProfile.Initialize(new JsonFileGemTdSaveStore(path));
            try
            {
                var controller = new WaveController(
                    new[] { _wave1 }, _states, _economy, 100, null, endWave: 1);
                var gate = new TestSpawnerGate();

                EnterPlanReady();
                ClearWave(controller, gate, expectedSpawns: 2);
                Assert.AreEqual(0, PlayerProfile.GetHighestWaveCleared());

                controller.BeginEndless();
                _states.EnterEndless();
                ClearWave(controller, gate, expectedSpawns: 2);
                Assert.AreEqual(2, PlayerProfile.GetHighestWaveCleared());
                Assert.IsTrue(PlayerProfile.LastUpdateWasNewBest);
            }
            finally
            {
                PlayerProfile.ResetForTests();
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public void ShouldOfferDraft_AuthoredFlagInsideCatalog_CadenceBeyond()
        {
            Assert.IsTrue(WaveController.ShouldOfferDraft(12, 15, authoredOffer: true));
            Assert.IsFalse(WaveController.ShouldOfferDraft(15, 15, authoredOffer: false));
            Assert.IsTrue(WaveController.ShouldOfferDraft(16, 15, authoredOffer: false));
            Assert.IsTrue(WaveController.ShouldOfferDraft(20, 15, authoredOffer: false));
            Assert.IsFalse(WaveController.ShouldOfferDraft(17, 15, authoredOffer: false));
        }

        [Test]
        public void BeyondCatalog_WaveFour_OffersDraft()
        {
            var controller = CreateController(new[] { _wave1 }, endWaveGold: 10, endWave: 8);
            var gate = new TestSpawnerGate();
            for (var w = 1; w <= 4; w++)
            {
                if (_states.Current == RunStateId.Draft)
                    _states.DraftResolved();
                EnterPlanReady();
                ClearWave(controller, gate, expectedSpawns: 2);
            }

            Assert.AreEqual(4, controller.CurrentWaveNumber);
            Assert.AreEqual(RunStateId.Draft, _states.Current);
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

        WaveController CreateController(WaveDefinition[] waves, int endWaveGold, EnemyDefinition bossEnemy) =>
            new WaveController(waves, _states, _economy, endWaveGold, bossEnemy);

        WaveController CreateController(WaveDefinition[] waves, int endWaveGold, int endWave) =>
            new WaveController(waves, _states, _economy, endWaveGold, bossEnemy: null, endWave: endWave);

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

        /// <summary>
        /// Builds a <paramref name="waveCount"/>-length array of trivial 1-enemy/0-interval
        /// waves (indices 0..waveCount-2), with the final wave (index waveCount-1, i.e. wave
        /// number == waveCount) carrying <paramref name="regularCountForLastWave"/> regulars
        /// of <c>_enemyDef</c> so boss-cadence tests can drive up to a target wave number.
        /// </summary>
        WaveDefinition[] BuildWaveArrayUpTo(int waveCount, int regularCountForLastWave)
        {
            var waves = new WaveDefinition[waveCount];
            for (var i = 0; i < waveCount - 1; i++)
                waves[i] = CreateWave(i + 1, _enemyDef, count: 1, interval: 0f);
            waves[waveCount - 1] = CreateWave(waveCount, _enemyDef, count: regularCountForLastWave, interval: 0f);
            return waves;
        }

        /// <summary>Clears <paramref name="waveCount"/> trivial 1-enemy waves (see BuildWaveArrayUpTo) in order.</summary>
        void AdvanceThroughWaves(WaveController controller, TestSpawnerGate gate, int waveCount)
        {
            for (var w = 0; w < waveCount; w++)
            {
                EnterPlanReady();
                ClearWave(controller, gate, expectedSpawns: 1);
            }
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
