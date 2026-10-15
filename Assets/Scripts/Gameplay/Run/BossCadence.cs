namespace GemTD.Gameplay.Run
{
    /// <summary>
    /// Boss wave cadence (Phase 3 design §8-9 / §11): campaign bosses on waves 10/20/30/40/50
    /// (min(wave/10, tipCount)); Endless (51+) is 1 boss per tip every wave.
    /// </summary>
    public static class BossCadence
    {
        public const int Interval = 10;
        public const int LastWave = 50;

        public static bool IsBossWave(int wave, bool endless = false)
        {
            if (endless)
                return wave > 0;
            return wave > 0 && wave <= LastWave && wave % Interval == 0;
        }

        /// <summary>
        /// Campaign: min(wave / 10, tipCount) on boss waves; 0 otherwise.
        /// Endless: tipCount every wave.
        /// </summary>
        public static int BossCount(int wave, int tipCount, bool endless = false)
        {
            if (tipCount <= 0 || wave <= 0)
                return 0;

            if (endless)
                return tipCount;

            if (!IsBossWave(wave))
                return 0;

            var count = wave / Interval;
            return count < tipCount ? count : tipCount;
        }
    }
}
