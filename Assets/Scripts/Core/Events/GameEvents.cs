using System;

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

        public static void RaiseGoldChanged(int gold) => GoldChanged?.Invoke(gold);
        public static void RaiseLivesChanged(int lives) => LivesChanged?.Invoke(lives);
        public static void RaiseWaveChanged(int wave) => WaveChanged?.Invoke(wave);
        public static void RaiseEvolutionUnlocked() => EvolutionUnlocked?.Invoke();

        public static void ClearAll()
        {
            GoldChanged = null;
            LivesChanged = null;
            WaveChanged = null;
            EvolutionUnlocked = null;
        }
    }
}
