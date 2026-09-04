using UnityEngine;

namespace GemTD.Gameplay.Run
{
    /// <summary>
    /// Phase 3 campaign scaling. Wave 1 is 1× authored stats; each later wave
    /// compounds the band rate (8% / 12% / 15% HP, 10% end-gold, 12% boss bounty),
    /// then HP is multiplied by the difficulty mode factor.
    /// </summary>
    public static class WaveScaling
    {
        public const float HpRateEarly = 0.08f;
        public const float HpRateMid = 0.12f;
        public const float HpRateLate = 0.15f;
        public const float EndWaveGoldRate = 0.08f;
        public const float BossBountyRate = 0.12f;
        public const float EndlessHpMultiplier = 1.2f;
        public const float EndlessGoldMultiplier = 0.5f;
        public const int EarlyBandEnd = 15;
        public const int MidBandEnd = 30;

        public static float HpRateForWave(int wave)
        {
            if (wave <= EarlyBandEnd)
                return HpRateEarly;
            if (wave <= MidBandEnd)
                return HpRateMid;
            return HpRateLate;
        }

        public static float HpScale(int wave, float modeHpMultiplier, bool endless = false)
        {
            var s = Compound(wave, HpRateForWave) * modeHpMultiplier;
            return endless ? s * EndlessHpMultiplier : s;
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
