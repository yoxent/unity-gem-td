using System;
using UnityEngine;

namespace GemTD.Core
{
    /// <summary>
    /// UI-facing signals only. Prefer direct calls between gameplay services.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<int> GoldChanged;
        public static event Action<int> LivesChanged;
        public static event Action<int> WaveChanged;
        public static event Action EvolutionUnlocked;
        public static event Action<float> SpeedChanged;
        public static event Action<bool> PauseChanged;
        public static event Action RequestCloseTopMost;
        public static event Action CodexToggled;
        public static event Action RequestTargetingAllConfirm;
        public static event Action<Vector2Int> ChunkPlaced;

        public static event Action RunStateChanged;
        public static event Action TowerSelectionChanged;
        public static event Action InventoryChanged;
        public static event Action TargetingChanged;
        public static event Action PlaceModeChanged;
        public static event Action DraftOfferChanged;
        public static event Action TowerRosterChanged;

        public static void RaiseGoldChanged(int gold) => GoldChanged?.Invoke(gold);
        public static void RaiseLivesChanged(int lives) => LivesChanged?.Invoke(lives);
        public static void RaiseWaveChanged(int wave) => WaveChanged?.Invoke(wave);
        public static void RaiseEvolutionUnlocked() => EvolutionUnlocked?.Invoke();

        public static void RaiseSpeedChanged(float scale) => SpeedChanged?.Invoke(scale);
        public static void RaisePauseChanged(bool paused) => PauseChanged?.Invoke(paused);
        public static void RaiseRequestCloseTopMost() => RequestCloseTopMost?.Invoke();
        public static void RaiseCodexToggled() => CodexToggled?.Invoke();
        public static void RaiseRequestTargetingAllConfirm() => RequestTargetingAllConfirm?.Invoke();
        public static void RaiseChunkPlaced(Vector2Int coord) => ChunkPlaced?.Invoke(coord);

        public static void RaiseRunStateChanged() => RunStateChanged?.Invoke();
        public static void RaiseTowerSelectionChanged() => TowerSelectionChanged?.Invoke();
        public static void RaiseInventoryChanged() => InventoryChanged?.Invoke();
        public static void RaiseTargetingChanged() => TargetingChanged?.Invoke();
        public static void RaisePlaceModeChanged() => PlaceModeChanged?.Invoke();
        public static void RaiseDraftOfferChanged() => DraftOfferChanged?.Invoke();
        public static void RaiseTowerRosterChanged() => TowerRosterChanged?.Invoke();

        public static void ClearAll()
        {
            GoldChanged = null;
            LivesChanged = null;
            WaveChanged = null;
            EvolutionUnlocked = null;
            SpeedChanged = null;
            PauseChanged = null;
            RequestCloseTopMost = null;
            CodexToggled = null;
            RequestTargetingAllConfirm = null;
            ChunkPlaced = null;
            RunStateChanged = null;
            TowerSelectionChanged = null;
            InventoryChanged = null;
            TargetingChanged = null;
            PlaceModeChanged = null;
            DraftOfferChanged = null;
            TowerRosterChanged = null;
        }
    }
}
