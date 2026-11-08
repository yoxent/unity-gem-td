using System;
using System.Collections.Generic;

namespace GemTD.Core
{
    /// <summary>
    /// Per-key minimum-interval gate for SFX raised at high frequency.
    /// Tower fire and projectile impact events can exceed what the eight
    /// <see cref="AudioPlayer"/> sources can voice, and source stealing turns a
    /// dense volley into a stutter. Pure C# with an injected clock so the gate is
    /// testable without a scene or a real audio device.
    /// </summary>
    public sealed class SfxRateLimiter
    {
        readonly Dictionary<string, float> _lastPlayedSeconds =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true when <paramref name="eventKey"/> is allowed to play at
        /// <paramref name="nowSeconds"/>. A non-positive
        /// <paramref name="minIntervalSeconds"/> always allows playback.
        /// An allowed call consumes the budget; a rejected one does not.
        /// </summary>
        public bool TryPlay(string eventKey, float minIntervalSeconds, float nowSeconds)
        {
            if (string.IsNullOrEmpty(eventKey))
                return true;

            if (minIntervalSeconds <= 0f)
            {
                _lastPlayedSeconds[eventKey] = nowSeconds;
                return true;
            }

            if (_lastPlayedSeconds.TryGetValue(eventKey, out var lastPlayed))
            {
                var elapsed = nowSeconds - lastPlayed;
                // elapsed < 0 means the clock moved backwards; allow rather than
                // silence the key until it catches up.
                if (elapsed >= 0f && elapsed < minIntervalSeconds)
                    return false;
            }

            _lastPlayedSeconds[eventKey] = nowSeconds;
            return true;
        }

        public void Reset()
        {
            _lastPlayedSeconds.Clear();
        }
    }
}
