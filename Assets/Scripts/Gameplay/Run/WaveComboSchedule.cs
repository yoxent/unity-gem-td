namespace GemTD.Gameplay.Run
{
    public enum WaveComboId
    {
        Rush = 0,
        Bulwark = 1,
        Ward = 2,
        FullMix = 3
    }

    /// <summary>
    /// Fixed combo calendar from wave 16. Waves 1–15 stay authored.
    /// Endless rotates Rush, Bulwark, Ward, Full mix, one per wave.
    /// </summary>
    public static class WaveComboSchedule
    {
        public const int AuthoredWaveCount = 15;

        public static WaveComboId ForWave(int wave, bool endless)
        {
            if (endless && wave > WaveScaling.CampaignEndWave)
            {
                var step = wave - (WaveScaling.CampaignEndWave + 1);
                var cycle = step % 4;
                if (cycle < 0)
                    cycle += 4;
                return (WaveComboId)cycle;
            }

            if (wave >= 50)
                return WaveComboId.FullMix;
            if (wave >= 45)
                return WaveComboId.Bulwark;
            if (wave >= 41)
                return WaveComboId.Rush;
            if (wave >= 40)
                return WaveComboId.FullMix;
            if (wave >= 35)
                return WaveComboId.Ward;
            if (wave >= 31)
                return WaveComboId.Bulwark;
            if (wave >= 30)
                return WaveComboId.FullMix;
            if (wave >= 25)
                return WaveComboId.Rush;
            if (wave >= 21)
                return WaveComboId.Ward;
            if (wave >= 20)
                return WaveComboId.Bulwark;
            return WaveComboId.Rush;
        }
    }
}
