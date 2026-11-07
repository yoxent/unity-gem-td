using UnityEngine;
using GemTD.Gameplay.Run;

namespace GemTD.Gameplay.Map
{
    /// <summary>
    /// When the run offers a chunk expand relative to the upcoming wave number.
    /// Default preserves today's one-expand-per-pre-final-wave schedule.
    /// </summary>
    public enum ExpansionCadence
    {
        EveryPreFinalWave = 0,
        OddUpcomingWaves = 1
    }

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

        /// <summary>
        /// True when the upcoming wave should offer a chunk expand under
        /// <paramref name="cadence"/>. The end wave never expands.
        /// </summary>
        public static bool ShouldExpand(
            int upcomingWave,
            int endWave,
            ExpansionCadence cadence = ExpansionCadence.EveryPreFinalWave)
        {
            if (endWave <= 0 || upcomingWave <= 0 || upcomingWave >= endWave)
                return false;
            if (cadence == ExpansionCadence.OddUpcomingWaves)
                return (upcomingWave & 1) == 1;
            return true;
        }

        /// <summary>
        /// Counts expansion turns from <paramref name="upcomingWave"/> through the
        /// wave before <paramref name="endWave"/>, respecting <paramref name="cadence"/>.
        /// The end wave has no expand phase. Default cadence remains one expansion
        /// per pre-final wave so topology budgeting stays independent of callers.
        /// </summary>
        public static int ExpansionTurnsRemaining(
            int upcomingWave,
            int endWave,
            ExpansionCadence cadence = ExpansionCadence.EveryPreFinalWave)
        {
            if (endWave <= 0 || upcomingWave <= 0 || upcomingWave >= endWave)
                return 0;
            if (cadence == ExpansionCadence.EveryPreFinalWave)
                return endWave - upcomingWave;

            var count = 0;
            for (var wave = upcomingWave; wave < endWave; wave++)
            {
                if (ShouldExpand(wave, endWave, cadence))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// True when the upcoming wave should skip expand (end wave, or cadence gap).
        /// An unset <paramref name="endWave"/> never skips (matches historical callers).
        /// Default cadence only skips at/after a configured end wave.
        /// </summary>
        public static bool SkipExpand(
            int upcomingWave,
            int endWave,
            ExpansionCadence cadence = ExpansionCadence.EveryPreFinalWave)
        {
            if (endWave <= 0)
                return false;
            return !ShouldExpand(upcomingWave, endWave, cadence);
        }
    }
}
