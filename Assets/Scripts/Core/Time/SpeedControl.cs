using System;
using System.Collections.Generic;

namespace GemTD.Core
{
    /// <summary>
    /// Single owner of pause + speed. Pause is reference-counted so multiple systems
    /// (Draft, popups, user) can request pause without fighting. Wraps <see cref="RunClock"/>.
    /// </summary>
    public sealed class SpeedControl
    {
        readonly RunClock _clock;
        readonly HashSet<string> _pauseReasons = new HashSet<string>(StringComparer.Ordinal);
        bool _userPaused;

        public SpeedControl(RunClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public float CurrentSpeed => _clock.TimeScale;

        public bool IsPaused => _pauseReasons.Count > 0 || _userPaused;

        public void SetSpeed(float scale)
        {
            if (scale != 1f && scale != 2f && scale != 4f)
                throw new ArgumentOutOfRangeException(nameof(scale), "Speed must be 1, 2, or 4.");
            _clock.SetTimeScale(scale);
            GameEvents.RaiseSpeedChanged(scale);
        }

        public void TogglePause()
        {
            _userPaused = !_userPaused;
            ApplyPauseToClock();
            GameEvents.RaisePauseChanged(IsPaused);
        }

        public void PushPause(string reason)
        {
            if (string.IsNullOrEmpty(reason))
                throw new ArgumentNullException(nameof(reason));
            var wasPaused = IsPaused;
            _pauseReasons.Add(reason);
            if (!wasPaused)
            {
                ApplyPauseToClock();
                GameEvents.RaisePauseChanged(true);
            }
        }

        public void PopPause(string reason)
        {
            if (string.IsNullOrEmpty(reason))
                throw new ArgumentNullException(nameof(reason));
            if (_pauseReasons.Count == 0)
                return; // unbalanced pop: no-op, never go below zero
            var wasPaused = IsPaused;
            _pauseReasons.Remove(reason);
            if (wasPaused && !IsPaused)
            {
                ApplyPauseToClock();
                GameEvents.RaisePauseChanged(false);
            }
        }

        public void ResetSpeedForNewRun()
        {
            _clock.SetTimeScale(1f);
            GameEvents.RaiseSpeedChanged(1f);
        }

        void ApplyPauseToClock()
        {
            _clock.SetPaused(IsPaused);
        }
    }
}