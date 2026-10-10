using UnityEngine;

namespace GemTD.Gameplay.Run
{
    /// <summary>
    /// Three toughness steps. Non-boss campaign waves compound
    /// <see cref="HpRateSmooth"/>. Boss waves (10/20/30/40/50) take
    /// <see cref="HpRateBoss"/>, and later non-boss waves keep compounding
    /// from that new level. Endless (51+) takes <see cref="HpRateEndless"/>
    /// every wave, a larger step than a campaign boss. Difficulty mode
    /// multiplies the result. Headcount on a reused template grows 4%/wave
    /// through wave 50, every type keeping its share, then holds.
    /// </summary>
    public static class WaveScaling
    {
        public const float HpRateSmooth = 0.08f;
        public const float HpRateBoss = 0.16f;
        public const float HpRateEndless = 0.24f;
        public const float ArmorRateSmooth = 0.04f;
        public const float ArmorRateBoss = 0.10f;
        public const float ArmorRateEndless = 0.16f;
        public const float SpeedRateSmooth = 0.005f;
        public const float SpeedRateBoss = 0.02f;
        public const float SpeedRateEndless = 0.03f;
        public const float SpeedScaleCap = 1.65f;
        public const int ResistStepSmooth = 1;
        public const int ResistStepBoss = 3;
        public const int ResistStepEndless = 5;
        public const int ResistCap = 75;
        public const float CountRateCampaign = 0.04f;
        public const float EndWaveGoldRate = 0.08f;
        public const float BossBountyRate = 0.12f;
        public const float EndlessGoldMultiplier = 0.5f;
        public const int CampaignEndWave = 50;

        public static float HpRateForWave(int wave, bool endless = false)
        {
            if (endless && wave > CampaignEndWave)
                return HpRateEndless;
            if (BossCadence.IsBossWave(wave))
                return HpRateBoss;
            return HpRateSmooth;
        }

        public static float ArmorRateForWave(int wave, bool endless = false)
        {
            if (endless && wave > CampaignEndWave)
                return ArmorRateEndless;
            if (BossCadence.IsBossWave(wave))
                return ArmorRateBoss;
            return ArmorRateSmooth;
        }

        public static float SpeedRateForWave(int wave, bool endless = false)
        {
            if (endless && wave > CampaignEndWave)
                return SpeedRateEndless;
            if (BossCadence.IsBossWave(wave))
                return SpeedRateBoss;
            return SpeedRateSmooth;
        }

        public static int ResistStepForWave(int wave, bool endless = false)
        {
            if (endless && wave > CampaignEndWave)
                return ResistStepEndless;
            if (BossCadence.IsBossWave(wave))
                return ResistStepBoss;
            return ResistStepSmooth;
        }

        public static float HpScale(int wave, float modeHpMultiplier, bool endless = false)
        {
            return Compound(wave, w => HpRateForWave(w, endless)) * modeHpMultiplier;
        }

        public static float ArmorScale(int wave, bool endless = false) =>
            Compound(wave, w => ArmorRateForWave(w, endless));

        public static float SpeedScale(int wave, bool endless = false)
        {
            var scale = Compound(wave, w => SpeedRateForWave(w, endless));
            return scale > SpeedScaleCap ? SpeedScaleCap : scale;
        }

        /// <summary>
        /// Flat points added only to resistances an enemy already has.
        /// </summary>
        public static int ResistBonus(int wave, bool endless = false)
        {
            if (wave < 2)
                return 0;
            var bonus = 0;
            for (var w = 2; w <= wave; w++)
                bonus += ResistStepForWave(w, endless);
            return bonus;
        }

        /// <summary>
        /// Grows <paramref name="counts"/> (length <paramref name="length"/>) when
        /// <paramref name="wave"/> is past the template's authored wave. Every type
        /// keeps its share. Growth stops at wave 50. The total rises by at least
        /// one spawn per counted wave, and by the compound count rate when that is larger.
        /// </summary>
        public static void ApplyCountScale(
            int[] counts,
            int length,
            int templateWave,
            int wave)
        {
            if (counts == null || length <= 0)
                return;

            var end = wave > CampaignEndWave ? CampaignEndWave : wave;
            if (end <= templateWave)
                return;

            var steps = end - templateWave;
            var mult = 1f;
            for (var step = 1; step <= steps; step++)
                mult *= 1f + CountRateCampaign;

            var baseTotal = 0;
            for (var i = 0; i < length; i++)
            {
                if (counts[i] > 0)
                    baseTotal += counts[i];
            }

            if (baseTotal <= 0)
                return;

            var grown = Mathf.RoundToInt(baseTotal * mult);
            var atLeast = baseTotal + steps;
            var target = grown > atLeast ? grown : atLeast;
            var exactScale = target / (float)baseTotal;
            var fractions = new float[length];
            var assigned = 0;
            for (var i = 0; i < length; i++)
            {
                if (counts[i] <= 0)
                {
                    counts[i] = 0;
                    continue;
                }

                var exact = counts[i] * exactScale;
                var floor = Mathf.FloorToInt(exact);
                counts[i] = floor;
                fractions[i] = exact - floor;
                assigned += floor;
            }

            var left = target - assigned;
            while (left > 0)
            {
                var best = -1;
                var bestFrac = -1f;
                for (var i = 0; i < length; i++)
                {
                    if (fractions[i] <= 0f && counts[i] <= 0)
                        continue;
                    if (best >= 0 && fractions[i] <= bestFrac)
                        continue;
                    best = i;
                    bestFrac = fractions[i];
                }

                if (best < 0)
                    break;

                counts[best]++;
                fractions[best] -= 1f;
                left--;
            }
        }

        public static int ScaleEndWaveGold(int baseGold, int wave, bool endless = false)
        {
            var amount = ScaleInt(baseGold, wave, w => EndWaveGoldRate);
            return ApplyEndlessGold(amount, endless);
        }

        public static int ScaleBossBounty(int baseGold, int wave, bool endless = false)
        {
            var amount = ScaleInt(baseGold, wave, w => BossBountyRate);
            return ApplyEndlessGold(amount, endless);
        }

        public static int ApplyEndlessGold(int amount, bool endless) =>
            endless ? Mathf.RoundToInt(amount * EndlessGoldMultiplier) : amount;

        static float Compound(int wave, System.Func<int, float> rateForWave)
        {
            if (wave < 1)
                wave = 1;
            var s = 1f;
            for (var w = 2; w <= wave; w++)
                s *= 1f + rateForWave(w);
            return s;
        }

        static int ScaleInt(int baseAmount, int wave, System.Func<int, float> rateForWave)
        {
            return Mathf.RoundToInt(baseAmount * Compound(wave, rateForWave));
        }
    }
}
