namespace GemTD.Gameplay.Run
{
    /// <summary>
    /// Boss wave cadence (Phase 3 design §8-9): bosses spawn on waves 10/20/30/40/50,
    /// one per furthest tip (by hop BFS from home, coord tiebreak), capped at tip count.
    /// Endless (51+, Task 8) is out of scope — <see cref="IsBossWave"/> is false past wave 50.
    /// </summary>
    public static class BossCadence
    {
        public const int Interval = 10;
        public const int LastWave = 50;

        public static bool IsBossWave(int wave) =>
            wave > 0 && wave <= LastWave && wave % Interval == 0;

        /// <summary>min(wave / 10, tipCount) on boss waves; 0 otherwise (integer division).</summary>
        public static int BossCount(int wave, int tipCount)
        {
            if (!IsBossWave(wave) || tipCount <= 0)
                return 0;

            var count = wave / Interval;
            return count < tipCount ? count : tipCount;
        }
    }
}
