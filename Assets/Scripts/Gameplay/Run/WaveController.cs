using System;
using System.Collections.Generic;
using GemTD.Core;
using GemTD.Gameplay.Enemies;

namespace GemTD.Gameplay.Run
{
    public sealed class WaveController
    {
        readonly WaveDefinition[] _waves;
        readonly RunStateMachine _states;
        readonly RunEconomy _economy;
        readonly int _endWaveGold;
        readonly EnemyDefinition _bossEnemy;
        readonly List<EnemyDefinition> _spawnQueue = new List<EnemyDefinition>();

        int _nextWaveIndex;
        int _spawnIndex;
        float _spawnTimer;
        WaveDefinition _activeWave;
        bool _waveCleared;

        public int CurrentWaveNumber { get; private set; }

        public int NextWaveNumber => _nextWaveIndex + 1;

        /// <summary>Bosses injected into the current wave's spawn queue by cadence (Task 6).</summary>
        public int CurrentBossCount { get; private set; }

        public WaveController(
            WaveDefinition[] waves,
            RunStateMachine states,
            RunEconomy economy,
            int endWaveGold,
            EnemyDefinition bossEnemy = null)
        {
            _waves = waves ?? throw new ArgumentNullException(nameof(waves));
            if (_waves.Length == 0)
                throw new ArgumentException("At least one wave definition is required.", nameof(waves));

            _states = states ?? throw new ArgumentNullException(nameof(states));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _endWaveGold = endWaveGold;
            _bossEnemy = bossEnemy;
        }

        /// <summary>
        /// <paramref name="spawnTipCount"/> is the live tip count for the combat about to
        /// start (from <c>PathGraph.CollectSpawnTips</c>) — used only for boss cadence
        /// (min(wave/10, tipCount)). Callers that don't care about boss cadence (e.g. waves
        /// before the first boss wave) may omit it.
        /// </summary>
        public void StartWave(int spawnTipCount = 1)
        {
            if (_nextWaveIndex >= _waves.Length)
                throw new InvalidOperationException("Campaign complete — no more waves.");

            _states.StartWave();

            _activeWave = _waves[_nextWaveIndex];
            CurrentWaveNumber = _nextWaveIndex + 1;
            CurrentBossCount = _bossEnemy != null
                ? BossCadence.BossCount(CurrentWaveNumber, spawnTipCount)
                : 0;
            BuildSpawnQueue(_activeWave, CurrentBossCount);
            _spawnIndex = 0;
            _spawnTimer = 0f;
            _waveCleared = false;
        }

        public void Tick(float dt, EnemySpawnerGate spawner)
        {
            if (spawner == null || _waveCleared || _states.Current != RunStateId.Combat)
                return;

            if (_spawnIndex < _spawnQueue.Count)
            {
                if (dt > 0f)
                    _spawnTimer -= dt;

                while (_spawnIndex < _spawnQueue.Count && _spawnTimer <= 0f)
                {
                    spawner.Spawn(_spawnQueue[_spawnIndex]);
                    _spawnIndex++;
                    _spawnTimer += _activeWave.SpawnInterval;
                }
            }

            if (_spawnIndex >= _spawnQueue.Count && spawner.LiveEnemyCount == 0)
            {
                _waveCleared = true;
                _nextWaveIndex++;
                _economy.GrantEndWaveGold(WaveScaling.ScaleEndWaveGold(_endWaveGold, CurrentWaveNumber));
                var offerDraft = _activeWave != null && _activeWave.OfferDraftAfterClear;
                var endsCampaign = _activeWave != null && _activeWave.EndsCampaign;
                _states.WaveCleared(offerDraft, endsCampaign);
            }
        }

        void BuildSpawnQueue(WaveDefinition wave, int bossCount)
        {
            _spawnQueue.Clear();
            var entries = wave.Entries;
            if (entries != null)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    if (entry.Enemy == null || entry.Count <= 0)
                        continue;

                    // Cadence owns all boss placement — authored boss entries are dropped
                    // even outside boss waves (see BossCadence / Task 6 brief).
                    if (entry.Enemy.IsBoss)
                        continue;

                    for (var c = 0; c < entry.Count; c++)
                        _spawnQueue.Add(entry.Enemy);
                }
            }

            // Bosses spawn after regulars — the wave's finale.
            for (var c = 0; c < bossCount; c++)
                _spawnQueue.Add(_bossEnemy);
        }
    }
}
