using System;

using UnityEngine;

namespace GemTD.Core
{    [Serializable]
    public struct SfxData
    {
        public AudioClip clip;
        public bool randomPitch;
        public float pitch;
        public float pitchMin;
        public float pitchMax;

        [Tooltip("Minimum seconds between plays of this cue's event key. 0 disables throttling. " +
                 "Set on high-frequency cues (tower fire, impacts) so dense combat does not steal sources.")]
        public float minIntervalSeconds;

        public static SfxData Default => new SfxData
        {
            clip = null,
            randomPitch = false,
            pitch = 1f,
            pitchMin = 0.9f,
            pitchMax = 1.1f,
            minIntervalSeconds = 0f,
        };
    }
}
