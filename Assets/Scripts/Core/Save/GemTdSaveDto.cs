using System;

namespace GemTD.Core
{
    [Serializable]
    public sealed class GemTdSaveDto
    {
        public float MasterVolume = 1f;
        public float BgmVolume = 1f;
        public float SfxVolume = 1f;
        /// <summary>JsonUtility-safe: missing field is false, which keeps shake on.</summary>
        public bool CameraShakeDisabled;
        public int HighestWaveCleared;
    }
}
