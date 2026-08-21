using UnityEngine;
using GemTD.Gameplay.Run;

namespace GemTD.Gameplay.Map
{
    public static class ExpandPickPolicy
    {
        public const int CrossStopWave = 35;
        public const int TJunctionStopWave = 45;

        public const int DefaultFirstSplitWave = 8;
        public const int DefaultCrossUnlockWave = 25;
        public const int DefaultTipCap = 4;
        public const float DefaultSplitP = 0.30f;
        public const int DefaultEndWave = 50;

        public static int FirstSplitWave(RunConfig config) =>
            config != null ? config.GetFirstSplitWave() : DefaultFirstSplitWave;

        public static int CrossUnlockWave(RunConfig config) =>
            config != null ? config.GetCrossUnlockWave() : DefaultCrossUnlockWave;

        public static int TipCap(RunConfig config) =>
            config != null ? config.GetTipCap() : DefaultTipCap;

        public static float SplitP(RunConfig config) =>
            config != null ? config.GetSplitP() : DefaultSplitP;

        public static int EndWave(RunConfig config)
        {
            if (config == null || config.EndWave <= 0)
                return DefaultEndWave;
            return config.EndWave;
        }

        public static bool AllowsTJunction(int upcomingWave, int tipCount, RunConfig config)
        {
            if (upcomingWave >= TJunctionStopWave)
                return false;
            if (upcomingWave < FirstSplitWave(config))
                return false;
            if (tipCount >= TipCap(config))
                return false;
            return true;
        }

        public static bool AllowsCross(int upcomingWave, int tipCount, RunConfig config)
        {
            if (upcomingWave >= CrossStopWave)
                return false;
            if (upcomingWave < CrossUnlockWave(config))
                return false;
            if (upcomingWave < FirstSplitWave(config))
                return false;
            if (tipCount >= TipCap(config))
                return false;
            return true;
        }

        public static float CrossRamp(int upcomingWave, RunConfig config)
        {
            var t = (upcomingWave - CrossUnlockWave(config)) / 10f;
            return Mathf.Clamp(t, 0f, 1f);
        }

        public static bool IsClosingWindow(int upcomingWave, int tipCount, int endWave)
        {
            if (tipCount <= 1)
                return false;
            return tipCount >= (endWave - upcomingWave + 1);
        }
    }
}
