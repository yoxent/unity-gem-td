namespace GemTD.Core
{
    public static class AudioMix
    {
        public const float PauseDuck = 0.5f;
        public const float DuckFadeSeconds = 0.2f;

        public static float BgmSourceVolume(float cueVolume, float bgmSlider, bool paused)
        {
            return cueVolume * bgmSlider * (paused ? PauseDuck : 1f);
        }

        public static float SfxSourceVolume(float cueVolume, float sfxSlider)
        {
            return cueVolume * sfxSlider;
        }
    }
}
