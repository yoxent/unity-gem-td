using System;

namespace GemTD.Core
{
    /// <summary>
    /// Simulation clock independent of UI. Pause freezes sim while menus stay interactive.
    /// </summary>
    public sealed class RunClock
    {
        public bool IsPaused { get; private set; }
        public float TimeScale { get; private set; } = 1f;

        public float DeltaTime => IsPaused ? 0f : UnityEngine.Time.unscaledDeltaTime * TimeScale;

        public void SetPaused(bool paused) => IsPaused = paused;

        public void TogglePause() => IsPaused = !IsPaused;

        public void SetTimeScale(float scale)
        {
            if (scale <= 0f) throw new ArgumentOutOfRangeException(nameof(scale));
            TimeScale = scale;
        }

        public void CycleSpeed()
        {
            TimeScale = TimeScale switch
            {
                <= 1.01f => 2f,
                <= 2.01f => 4f,
                _ => 1f
            };
        }
    }
}
