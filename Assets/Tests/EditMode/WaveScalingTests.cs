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
        public void HpScale_WaveTwo_AppliesEarlyBandOnce()
        {
            Assert.AreEqual(1.08f, WaveScaling.HpScale(2, 1f), 0.0001f);
        }

        [Test]
        public void HpScale_WaveFifteen_MatchesEarlyBandCompound()
        {
            var expected = 1f;
            for (var w = 2; w <= 15; w++)
                expected *= 1.08f;
            Assert.AreEqual(expected, WaveScaling.HpScale(15, 1f), 0.0001f);
            Assert.AreEqual(2.94f, WaveScaling.HpScale(15, 1f), 0.05f);
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
    }
}
