using System;
using UnityEngine;

namespace GemTD.Core
{
    [Serializable]
    public struct SfxData
    {
        public AudioClip clip;
        public bool randomPitch;
        public float pitch;
        public float pitchMin;
        public float pitchMax;

        public static SfxData Default => new SfxData
        {
            clip = null,
            randomPitch = false,
            pitch = 1f,
            pitchMin = 0.9f,
            pitchMax = 1.1f,
        };
    }
}
