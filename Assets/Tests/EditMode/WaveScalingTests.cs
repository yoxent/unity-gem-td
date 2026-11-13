using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Run;

namespace GemTD.Tests.EditMode
{
    public sealed class WaveScalingTests
    {
        [Test]
        public void HpScale_WaveOne_IsModeMultiplierOnly()
        {
            Assert.AreEqual(1f, WaveScaling.HpScale(1, 1f), 0.0001f);
            Assert.AreEqual(1.1f, WaveScaling.HpScale(1, 1.1f), 0.0001f);
        }

        [Test]
        public void HpScale_WaveTwo_AppliesSmoothRateOnce()
        {
            Assert.AreEqual(1f + WaveScaling.HpRateSmooth, WaveScaling.HpScale(2, 1f), 0.0001f);
        }

        [Test]
        public void HpScale_BossWave_Jumps_ThenNonBossContinuesFromThatLevel()
        {
            var beforeBoss = WaveScaling.HpScale(9, 1f);
            var boss = WaveScaling.HpScale(10, 1f);
            var afterBoss = WaveScaling.HpScale(11, 1f);

            Assert.AreEqual(beforeBoss * (1f + WaveScaling.HpRateBoss), boss, 0.0001f);
            Assert.AreEqual(boss * (1f + WaveScaling.HpRateSmooth), afterBoss, 0.0001f);
            Assert.Greater(WaveScaling.HpRateBoss, WaveScaling.HpRateSmooth);
        }

        [Test]
        public void HpScale_WaveFifteen_KeepsBossJumpInsideSmoothCompound()
        {
            var expected = 1f;
            for (var w = 2; w <= 15; w++)
                expected *= 1f + WaveScaling.HpRateForWave(w);
            Assert.AreEqual(expected, WaveScaling.HpScale(15, 1f), 0.0001f);
            Assert.Greater(WaveScaling.HpScale(15, 1f), WaveScaling.HpScale(10, 1f));
        }

        [Test]
        public void HpScale_IsMonotonicThroughWaveFifty()
        {
            var prev = 0f;
            for (var w = 1; w <= 50; w++)
            {
                var s = WaveScaling.HpScale(w, 1f);
                Assert.GreaterOrEqual(s, prev);
                prev = s;
            }
        }

        [Test]
        public void HpScale_AppliesModeMultiplierOnTopOfCompound()
        {
            var oneLane = WaveScaling.HpScale(15, 1f);
            Assert.AreEqual(oneLane * 1.4f, WaveScaling.HpScale(15, 1.4f), 0.0001f);
        }

        [Test]
        public void ScaleEndWaveGold_WaveOneUnchanged_ThenEightPercent()
        {
            Assert.AreEqual(50, WaveScaling.ScaleEndWaveGold(50, 1));
            Assert.AreEqual(54, WaveScaling.ScaleEndWaveGold(50, 2));
        }

        [Test]
        public void ScaleBossBounty_WaveOneUnchanged_ThenTwelvePercent()
        {
            Assert.AreEqual(50, WaveScaling.ScaleBossBounty(50, 1));
            Assert.AreEqual(56, WaveScaling.ScaleBossBounty(50, 2));
        }

        [Test]
        public void GoldScales_AreMonotonicThroughWaveFifty()
        {
            var prevEnd = 0;
            var prevBoss = 0;
            for (var w = 1; w <= 50; w++)
            {
                var end = WaveScaling.ScaleEndWaveGold(50, w);
                var boss = WaveScaling.ScaleBossBounty(50, w);
                Assert.GreaterOrEqual(end, prevEnd);
                Assert.GreaterOrEqual(boss, prevBoss);
                prevEnd = end;
                prevBoss = boss;
            }
        }

        [Test]
        public void HpScale_WaveFifty_MatchesSmoothPlusBossJumps()
        {
            var expected = 1f;
            for (var w = 2; w <= 50; w++)
                expected *= 1f + WaveScaling.HpRateForWave(w);
            Assert.AreEqual(expected, WaveScaling.HpScale(50, 1f), 0.01f);
            Assert.AreEqual(62f, WaveScaling.HpScale(50, 1f), 1f);
        }

        [Test]
        public void HpScale_IsStrictlyIncreasingThroughWaveFifty()
        {
            var prev = WaveScaling.HpScale(1, 1f);
            for (var w = 2; w <= 50; w++)
            {
                var s = WaveScaling.HpScale(w, 1f);
                Assert.Greater(s, prev);
                prev = s;
            }
        }

        [Test]
        public void HpScale_Endless_JumpsHarderThanABossWave_EveryWave()
        {
            Assert.Greater(WaveScaling.HpRateEndless, WaveScaling.HpRateBoss);

            var at50 = WaveScaling.HpScale(50, 1f);
            var at51 = WaveScaling.HpScale(51, 1f, endless: true);
            var at52 = WaveScaling.HpScale(52, 1f, endless: true);
            Assert.AreEqual(at50 * (1f + WaveScaling.HpRateEndless), at51, 0.01f);
            Assert.AreEqual(at51 * (1f + WaveScaling.HpRateEndless), at52, 0.01f);
            Assert.Greater(at51 / at50, WaveScaling.HpScale(10, 1f) / WaveScaling.HpScale(9, 1f));
        }

        [Test]
        public void ApplyCountScale_SameWave_LeavesCounts()
        {
            var counts = new[] { 4, 3, 1 };
            WaveScaling.ApplyCountScale(counts, counts.Length, templateWave: 15, wave: 15);
            Assert.AreEqual(4, counts[0]);
            Assert.AreEqual(3, counts[1]);
            Assert.AreEqual(1, counts[2]);
        }

        [Test]
        public void ApplyCountScale_GrowsThroughWaveFifty_ThenHoldsShares()
        {
            var at16 = new[] { 16, 12, 8, 4 };
            var at50 = new[] { 16, 12, 8, 4 };
            var at60 = new[] { 16, 12, 8, 4 };
            WaveScaling.ApplyCountScale(at16, at16.Length, 15, 16);
            WaveScaling.ApplyCountScale(at50, at50.Length, 15, 50);
            WaveScaling.ApplyCountScale(at60, at60.Length, 15, 60);

            var mult = 1f;
            for (var step = 0; step < 35; step++)
                mult *= 1f + WaveScaling.CountRateCampaign;
            var atWave50 = Mathf.RoundToInt(40f * mult);

            var sum16 = at16[0] + at16[1] + at16[2] + at16[3];
            var sum50 = at50[0] + at50[1] + at50[2] + at50[3];
            var sum60 = at60[0] + at60[1] + at60[2] + at60[3];
            Assert.AreEqual(42, sum16);
            Assert.AreEqual(atWave50, sum50);
            Assert.AreEqual(sum50, sum60);
            Assert.AreEqual(17, at16[0]);
            Assert.AreEqual(13, at16[1]);
            Assert.AreEqual(8, at16[2]);
            Assert.AreEqual(4, at16[3]);
        }

        [Test]
        public void ArmorSpeedAndResist_BossJumpThenSmooth_EndlessJumpsHarder()
        {
            var armor9 = WaveScaling.ArmorScale(9);
            var armor10 = WaveScaling.ArmorScale(10);
            var armor11 = WaveScaling.ArmorScale(11);
            Assert.AreEqual(armor9 * (1f + WaveScaling.ArmorRateBoss), armor10, 0.0001f);
            Assert.AreEqual(armor10 * (1f + WaveScaling.ArmorRateSmooth), armor11, 0.0001f);

            var speed9 = WaveScaling.SpeedScale(9);
            var speed10 = WaveScaling.SpeedScale(10);
            var speed11 = WaveScaling.SpeedScale(11);
            Assert.AreEqual(speed9 * (1f + WaveScaling.SpeedRateBoss), speed10, 0.0001f);
            Assert.AreEqual(speed10 * (1f + WaveScaling.SpeedRateSmooth), speed11, 0.0001f);

            Assert.AreEqual(0, WaveScaling.ResistBonus(1));
            Assert.AreEqual(WaveScaling.ResistStepSmooth, WaveScaling.ResistBonus(2));
            Assert.AreEqual(
                WaveScaling.ResistBonus(9) + WaveScaling.ResistStepBoss,
                WaveScaling.ResistBonus(10));
            Assert.AreEqual(
                WaveScaling.ResistBonus(10) + WaveScaling.ResistStepSmooth,
                WaveScaling.ResistBonus(11));

            var endlessArmor = WaveScaling.ArmorScale(51, endless: true) / WaveScaling.ArmorScale(50);
            var endlessSpeed = WaveScaling.SpeedScale(51, endless: true) / WaveScaling.SpeedScale(50);
            Assert.AreEqual(1f + WaveScaling.ArmorRateEndless, endlessArmor, 0.0001f);
            Assert.AreEqual(1f + WaveScaling.SpeedRateEndless, endlessSpeed, 0.0001f);
            Assert.Greater(WaveScaling.ArmorRateEndless, WaveScaling.ArmorRateBoss);
            Assert.Greater(WaveScaling.SpeedRateEndless, WaveScaling.SpeedRateBoss);
            Assert.AreEqual(
                WaveScaling.ResistBonus(50) + WaveScaling.ResistStepEndless,
                WaveScaling.ResistBonus(51, endless: true));
            Assert.Greater(WaveScaling.ResistStepEndless, WaveScaling.ResistStepBoss);
        }

        [Test]
        public void ApplyCountScale_BossWave_KeepsTheSameShares()
        {
            var smooth = new[] { 8, 2 };
            var boss = new[] { 8, 2 };
            WaveScaling.ApplyCountScale(smooth, 2, templateWave: 15, wave: 16);
            WaveScaling.ApplyCountScale(boss, 2, templateWave: 15, wave: 20);

            var smoothTotal = smooth[0] + smooth[1];
            var bossTotal = boss[0] + boss[1];
            Assert.Greater(bossTotal, smoothTotal);
            Assert.AreEqual(smooth[0] / (float)smoothTotal, boss[0] / (float)bossTotal, 0.02f);
        }

        [Test]
        public void GoldScales_Endless_HalvesAfterCampaignFormula()
        {
            var end = WaveScaling.ScaleEndWaveGold(50, 51);
            var boss = WaveScaling.ScaleBossBounty(50, 51);
            Assert.AreEqual(Mathf.RoundToInt(end * 0.5f), WaveScaling.ScaleEndWaveGold(50, 51, endless: true));
            Assert.AreEqual(Mathf.RoundToInt(boss * 0.5f), WaveScaling.ScaleBossBounty(50, 51, endless: true));
            Assert.AreEqual(25, WaveScaling.ApplyEndlessGold(50, true));
            Assert.AreEqual(50, WaveScaling.ApplyEndlessGold(50, false));
        }
    }
}
