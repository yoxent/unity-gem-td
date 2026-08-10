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
        public static event Action<float> SpeedChanged;
        public static event Action<bool> PauseChanged;
        public static event Action RequestCloseTopMost;
        public static event Action CodexToggled;

        public static void RaiseGoldChanged(int gold) => GoldChanged?.Invoke(gold);
        public static void RaiseLivesChanged(int lives) => LivesChanged?.Invoke(lives);
        public static void RaiseWaveChanged(int wave) => WaveChanged?.Invoke(wave);
        public static void RaiseEvolutionUnlocked() => EvolutionUnlocked?.Invoke();

        public static void RaiseSpeedChanged(float scale) => SpeedChanged?.Invoke(scale);
        public static void RaisePauseChanged(bool paused) => PauseChanged?.Invoke(paused);
        public static void RaiseRequestCloseTopMost() => RequestCloseTopMost?.Invoke();
        public static void RaiseCodexToggled() => CodexToggled?.Invoke();

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
        }
    }
}
