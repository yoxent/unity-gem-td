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
        readonly List<EnemyDefinition> _spawnQueue = new List<EnemyDefinition>();

        int _nextWaveIndex;
        int _spawnIndex;
        float _spawnTimer;
        WaveDefinition _activeWave;
        bool _waveCleared;

        public int CurrentWaveNumber { get; private set; }

        public WaveController(
            WaveDefinition[] waves,
            RunStateMachine states,
            RunEconomy economy,
            int endWaveGold)
        {
            _waves = waves ?? throw new ArgumentNullException(nameof(waves));
            if (_waves.Length == 0)
                throw new ArgumentException("At least one wave definition is required.", nameof(waves));

            _states = states ?? throw new ArgumentNullException(nameof(states));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _endWaveGold = endWaveGold;
        }

        public void StartWave()
        {
            if (_nextWaveIndex >= _waves.Length)
                throw new InvalidOperationException("Campaign complete — no more waves.");

            _states.StartWave();

            _activeWave = _waves[_nextWaveIndex];
            CurrentWaveNumber = _nextWaveIndex + 1;
            BuildSpawnQueue(_activeWave);
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
                _economy.GrantEndWaveGold(_endWaveGold);
                var offerDraft = _activeWave != null && _activeWave.OfferDraftAfterClear;
                var endsCampaign = _activeWave != null && _activeWave.EndsCampaign;
                _states.WaveCleared(offerDraft, endsCampaign);
            }
        }

        void BuildSpawnQueue(WaveDefinition wave)
        {
            _spawnQueue.Clear();
            var entries = wave.Entries;
            if (entries == null)
                return;

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry.Enemy == null || entry.Count <= 0)
                    continue;

                for (var c = 0; c < entry.Count; c++)
                    _spawnQueue.Add(entry.Enemy);
            }
        }
    }
}
