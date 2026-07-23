using System;
using GemTD.Core;

namespace GemTD.Gameplay.Run
{
    public sealed class WaveController
    {
        readonly WaveDefinition[] _waves;
        readonly RunStateMachine _states;
        readonly RunEconomy _economy;
        readonly int _endWaveGold;

        int _nextWaveIndex;
        int _remainingSpawns;
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
            _states.StartWave();

            var defIndex = _nextWaveIndex;
            if (defIndex >= _waves.Length)
                defIndex = _waves.Length - 1;

            _activeWave = _waves[defIndex];
            CurrentWaveNumber = _nextWaveIndex + 1;
            _remainingSpawns = _activeWave.Count;
            _spawnTimer = 0f;
            _waveCleared = false;
        }

        public void Tick(float dt, EnemySpawnerGate spawner)
        {
            if (spawner == null || _waveCleared || _states.Current != RunStateId.Combat)
                return;

            if (_remainingSpawns > 0)
            {
                if (dt > 0f)
                    _spawnTimer -= dt;

                while (_remainingSpawns > 0 && _spawnTimer <= 0f)
                {
                    spawner.Spawn(_activeWave.Enemy);
                    _remainingSpawns--;
                    _spawnTimer += _activeWave.SpawnInterval;
                }
            }

            if (_remainingSpawns <= 0 && spawner.LiveEnemyCount == 0)
            {
                _waveCleared = true;
                _nextWaveIndex++;
                _economy.GrantEndWaveGold(_endWaveGold);
                _states.WaveCleared(offerDraft: false);
            }
        }
    }
}
