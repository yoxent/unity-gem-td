using System;
using System.Collections.Generic;
using GemTD.Core;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Map;

namespace GemTD.Gameplay.Run
{
    public sealed class WaveController
    {
        readonly WaveDefinition[] _waves;
        readonly RunStateMachine _states;
        readonly RunEconomy _economy;
        readonly int _endWaveGold;
        readonly EnemyDefinition _bossEnemy;
        readonly int _endWave;
        readonly Action _beforeCampaignVictory;
        readonly WaveDefinition[] _combos;
        readonly List<EnemyDefinition> _spawnQueue = new List<EnemyDefinition>();
        int[] _countScratch;

        public const float ClearHoldSeconds = 2f;

        int _nextWaveIndex;
        int _spawnIndex;
        float _spawnTimer;
        WaveDefinition _activeWave;
        bool _waveCleared;
        bool _clearHoldActive;
        float _clearHoldRemaining;

        public int CurrentWaveNumber { get; private set; }

        public int NextWaveNumber => _nextWaveIndex + 1;

        /// <summary>Bosses injected into the current wave's spawn queue by cadence (Task 6 / 8).</summary>
        public int CurrentBossCount { get; private set; }

        /// <summary>True after Victory → Endless; allows waves past EndWave and applies Endless modifiers.</summary>
        public bool IsEndless { get; private set; }

        public WaveController(
            WaveDefinition[] waves,
            RunStateMachine states,
            RunEconomy economy,
            int endWaveGold,
            EnemyDefinition bossEnemy = null,
            int endWave = 0,
            Action beforeCampaignVictory = null,
            WaveDefinition[] combos = null)
        {
            _waves = waves ?? throw new ArgumentNullException(nameof(waves));
            if (_waves.Length == 0)
                throw new ArgumentException("At least one wave definition is required.", nameof(waves));

            _states = states ?? throw new ArgumentNullException(nameof(states));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _endWaveGold = endWaveGold;
            _bossEnemy = bossEnemy;
            _endWave = endWave > 0 ? endWave : ExpandPickPolicy.DefaultEndWave;
            _beforeCampaignVictory = beforeCampaignVictory;
            _combos = combos;
        }

        public void BeginEndless()
        {
            IsEndless = true;
        }

        /// <summary>
        /// <paramref name="spawnTipCount"/> is the live tip count for the combat about to
        /// start (from <c>PathGraph.CollectSpawnTips</c>) — used for boss cadence.
        /// </summary>
        public void StartWave(int spawnTipCount = 1)
        {
            var waveNumber = _nextWaveIndex + 1;
            if (waveNumber > _endWave && !IsEndless)
                throw new InvalidOperationException("Campaign complete — no more waves.");

            _states.StartWave();

            _activeWave = ResolveWaveTemplate(_nextWaveIndex);
            CurrentWaveNumber = waveNumber;
            CurrentBossCount = _bossEnemy != null
                ? BossCadence.BossCount(CurrentWaveNumber, spawnTipCount, IsEndless)
                : 0;
            BuildSpawnQueue(_activeWave, CurrentBossCount);
            _spawnIndex = 0;
            _spawnTimer = 0f;
            _waveCleared = false;
            _clearHoldActive = false;
            _clearHoldRemaining = 0f;
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
                if (!_clearHoldActive)
                {
                    _clearHoldActive = true;
                    _clearHoldRemaining = ClearHoldSeconds;
                    GameEvents.RaiseWaveClearHoldChanged(true);
                }

                _clearHoldRemaining -= dt;
                if (_clearHoldRemaining > 0f)
                    return;

                _clearHoldActive = false;
                GameEvents.RaiseWaveClearHoldChanged(false);
                _waveCleared = true;
                _nextWaveIndex++;
                _economy.GrantEndWaveGold(
                    WaveScaling.ScaleEndWaveGold(_endWaveGold, CurrentWaveNumber, IsEndless));

                var endsCampaign = !IsEndless
                    && (CurrentWaveNumber >= _endWave
                        || (_activeWave != null && _activeWave.EndsCampaign));
                var offerDraft = ShouldOfferDraft(CurrentWaveNumber, _endWave, IsEndless);

                if (endsCampaign)
                    _beforeCampaignVictory?.Invoke();
                else if (IsEndless)
                    PlayerProfile.TryUpdateHighestWave(CurrentWaveNumber);

                _states.WaveCleared(offerDraft, endsCampaign);
            }
            else if (_clearHoldActive)
            {
                _clearHoldActive = false;
                GameEvents.RaiseWaveClearHoldChanged(false);
            }
        }

        /// <summary>
        /// Campaign: draft after every clear except the EndWave clear (victory).
        /// Endless: never draft.
        /// </summary>
        public static bool ShouldOfferDraft(int clearedWave, int endWave, bool isEndless)
        {
            if (isEndless)
                return false;
            return clearedWave > 0 && clearedWave < endWave;
        }

        WaveDefinition ResolveWaveTemplate(int waveIndex)
        {
            var waveNumber = waveIndex + 1;
            if (waveNumber > WaveComboSchedule.AuthoredWaveCount)
            {
                var combo = ComboFor(waveNumber);
                if (combo != null)
                    return combo;
            }

            if (waveIndex < _waves.Length)
                return _waves[waveIndex];
            return _waves[_waves.Length - 1];
        }

        WaveDefinition ComboFor(int waveNumber)
        {
            if (_combos == null || _combos.Length == 0)
                return null;

            var index = (int)WaveComboSchedule.ForWave(waveNumber, IsEndless);
            if (index < 0 || index >= _combos.Length)
                return null;
            return _combos[index];
        }

        void BuildSpawnQueue(WaveDefinition wave, int bossCount)
        {
            _spawnQueue.Clear();
            var entries = wave.Entries;
            if (entries != null)
            {
                var regular = 0;
                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    if (entry.Enemy == null || entry.Count <= 0 || entry.Enemy.IsBoss)
                        continue;
                    regular++;
                }

                if (regular > 0)
                {
                    if (_countScratch == null || _countScratch.Length < regular)
                        _countScratch = new int[regular];

                    var n = 0;
                    for (var i = 0; i < entries.Length; i++)
                    {
                        var entry = entries[i];
                        if (entry.Enemy == null || entry.Count <= 0 || entry.Enemy.IsBoss)
                            continue;
                        _countScratch[n] = entry.Count;
                        n++;
                    }

                    WaveScaling.ApplyCountScale(
                        _countScratch,
                        regular,
                        wave.WaveNumber,
                        CurrentWaveNumber);

                    n = 0;
                    for (var i = 0; i < entries.Length; i++)
                    {
                        var entry = entries[i];
                        // Cadence owns all boss placement — authored boss entries are dropped
                        // even outside boss waves (see BossCadence / Task 6 brief).
                        if (entry.Enemy == null || entry.Count <= 0 || entry.Enemy.IsBoss)
                            continue;

                        var count = _countScratch[n++];
                        for (var c = 0; c < count; c++)
                            _spawnQueue.Add(entry.Enemy);
                    }
                }
            }

            InsertBosses(bossCount);
        }

        void InsertBosses(int bossCount)
        {
            if (bossCount <= 0 || _bossEnemy == null)
                return;

            if (_spawnQueue.Count == 0)
            {
                for (var c = 0; c < bossCount; c++)
                    _spawnQueue.Add(_bossEnemy);
                return;
            }

            var index = _spawnQueue.Count / 4;
            for (var c = 0; c < bossCount; c++)
            {
                if (index > _spawnQueue.Count)
                    index = _spawnQueue.Count;
                _spawnQueue.Insert(index, _bossEnemy);
                index += 7;
            }
        }
    }
}
