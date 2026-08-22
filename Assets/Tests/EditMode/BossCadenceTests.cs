using NUnit.Framework;
using GemTD.Gameplay.Run;

namespace GemTD.Tests.EditMode
{
    public sealed class BossCadenceTests
    {
        [Test]
        public void IsBossWave_OnlyTrueOnMultiplesOfTenUpToFifty()
        {
            Assert.IsTrue(BossCadence.IsBossWave(10));
            Assert.IsTrue(BossCadence.IsBossWave(20));
            Assert.IsTrue(BossCadence.IsBossWave(30));
            Assert.IsTrue(BossCadence.IsBossWave(40));
            Assert.IsTrue(BossCadence.IsBossWave(50));

            Assert.IsFalse(BossCadence.IsBossWave(1));
            Assert.IsFalse(BossCadence.IsBossWave(9));
            Assert.IsFalse(BossCadence.IsBossWave(11));
            Assert.IsFalse(BossCadence.IsBossWave(15));
            Assert.IsFalse(BossCadence.IsBossWave(25));
        }

        [Test]
        public void IsBossWave_False_PastWaveFifty_WhenNotEndless()
        {
            Assert.IsFalse(BossCadence.IsBossWave(60));
            Assert.IsFalse(BossCadence.IsBossWave(51));
            Assert.IsFalse(BossCadence.IsBossWave(51, endless: false));
        }

        [Test]
        public void IsBossWave_True_PastWaveFifty_WhenEndless()
        {
            Assert.IsTrue(BossCadence.IsBossWave(51, endless: true));
            Assert.IsTrue(BossCadence.IsBossWave(60, endless: true));
            Assert.IsTrue(BossCadence.IsBossWave(50, endless: true)); // campaign boss wave still true
            Assert.IsTrue(BossCadence.IsBossWave(50));
        }

        [Test]
        public void BossCount_Endless_EqualsTipCountEveryWave()
        {
            Assert.AreEqual(1, BossCadence.BossCount(51, 1, endless: true));
            Assert.AreEqual(3, BossCadence.BossCount(52, 3, endless: true));
            Assert.AreEqual(2, BossCadence.BossCount(2, 2, endless: true));
            Assert.AreEqual(0, BossCadence.BossCount(51, 0, endless: true));
            // Campaign path unchanged when endless=false past 50
            Assert.AreEqual(0, BossCadence.BossCount(51, 3, endless: false));
        }

        [Test]
        public void BossCount_NonBossWave_IsZero()
        {
            Assert.AreEqual(0, BossCadence.BossCount(1, 4));
            Assert.AreEqual(0, BossCadence.BossCount(15, 4));
            Assert.AreEqual(0, BossCadence.BossCount(9, 4));
        }

        [Test]
        public void BossCount_WaveTen_IsOne()
        {
            Assert.AreEqual(1, BossCadence.BossCount(10, 4));
        }

        [Test]
        public void BossCount_ScalesWithWaveDividedByTen()
        {
            Assert.AreEqual(1, BossCadence.BossCount(10, 10));
            Assert.AreEqual(2, BossCadence.BossCount(20, 10));
            Assert.AreEqual(3, BossCadence.BossCount(30, 10));
            Assert.AreEqual(4, BossCadence.BossCount(40, 10));
            Assert.AreEqual(5, BossCadence.BossCount(50, 10));
        }

        [Test]
        public void BossCount_CappedAtTipCount()
        {
            // Wave 50 with only 1 tip (closing window collapsed to the last tip) → 1 boss.
            Assert.AreEqual(1, BossCadence.BossCount(50, 1));
            Assert.AreEqual(2, BossCadence.BossCount(50, 2));
            // Wave 30 wants 3 bosses but only 2 tips exist — capped at 2.
            Assert.AreEqual(2, BossCadence.BossCount(30, 2));
        }

        [Test]
        public void BossCount_ZeroTips_IsZero()
        {
            Assert.AreEqual(0, BossCadence.BossCount(10, 0));
        }
    }
}
