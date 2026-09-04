namespace GemTD.Core
{
    public static class AudioMix
    {
        public static float BgmSourceVolume(float cueVolume, float bgmSlider)
        {
            return cueVolume * bgmSlider;
        }

        public static float SfxSourceVolume(float cueVolume, float sfxSlider)
        {
            return cueVolume * sfxSlider;
        }
    }
}
