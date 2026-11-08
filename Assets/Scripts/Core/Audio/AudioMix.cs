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

        /// <summary>
        /// Effective length in seconds of a clip played at <paramref name="pitch"/>.
        /// Pitching up shortens a clip, so the cap must account for it.
        /// </summary>
        public static float AudibleLengthSeconds(float clipLengthSeconds, float pitch)
        {
            if (clipLengthSeconds <= 0f)
                return 0f;
            return clipLengthSeconds / (pitch > 0.01f ? pitch : 0.01f);
        }

        /// <summary>
        /// Scheduled end offset in seconds for one-shot playback, or 0 when the clip
        /// needs no cap. A cap of 0 or less means "play the whole clip".
        /// </summary>
        public static float ResolvePlaybackCapSeconds(
            float clipLengthSeconds,
            float pitch,
            float capSeconds)
        {
            if (capSeconds <= 0f)
                return 0f;

            var audible = AudibleLengthSeconds(clipLengthSeconds, pitch);
            return audible > capSeconds ? capSeconds : 0f;
        }
    }
}
