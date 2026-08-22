using System;

namespace GemTD.Core
{
    [Serializable]
    public sealed class GemTdSaveDto
    {
        public float MasterVolume = 1f;
        public float BgmVolume = 1f;
        public float SfxVolume = 1f;
        public int HighestWaveCleared;
    }
}
