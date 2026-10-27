using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Map;
using GemTD.Gameplay.Run;

namespace GemTD.Tests.EditMode
{
    public sealed class ExpandPickPolicyTests
    {
        RunConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<RunConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void AllowsTJunction_OneLane_BlockedBeforeWaveEight()
        {
            Assert.IsFalse(ExpandPickPolicy.AllowsTJunction(7, 1, _config));
            Assert.IsTrue(ExpandPickPolicy.AllowsTJunction(8, 1, _config));
        }

        [Test]
        public void AllowsCross_OneLane_BlockedBeforeWaveTwentyFive()
        {
            Assert.IsFalse(ExpandPickPolicy.AllowsCross(24, 1, _config));
            Assert.IsTrue(ExpandPickPolicy.AllowsCross(25, 1, _config));
        }

        [Test]
        public void AllowsTAndCross_AtTipCap_False()
        {
            var cap = ExpandPickPolicy.TipCap(_config);
            Assert.IsFalse(ExpandPickPolicy.AllowsTJunction(20, cap, _config));
            Assert.IsFalse(ExpandPickPolicy.AllowsCross(30, cap, _config));
        }

        [Test]
        public void AllowsCross_StopsAtWaveThirtyFive()
        {
            Assert.IsFalse(ExpandPickPolicy.AllowsCross(35, 1, _config));
            Assert.IsTrue(ExpandPickPolicy.AllowsCross(34, 1, _config));
        }

        [Test]
        public void AllowsTJunction_StopsAtWaveFortyFive()
        {
            Assert.IsFalse(ExpandPickPolicy.AllowsTJunction(45, 1, _config));
            Assert.IsTrue(ExpandPickPolicy.AllowsTJunction(44, 1, _config));
        }

        [Test]
        public void CrossRamp_UnlockPlusZeroIsZero_UnlockPlusTenIsOne()
        {
            var unlock = ExpandPickPolicy.CrossUnlockWave(_config);
            Assert.AreEqual(0f, ExpandPickPolicy.CrossRamp(unlock, _config), 0.0001f);
            Assert.AreEqual(1f, ExpandPickPolicy.CrossRamp(unlock + 10, _config), 0.0001f);
        }

        [Test]
        public void NullConfig_UsesOneLaneDefaults()
        {
            Assert.AreEqual(8, ExpandPickPolicy.FirstSplitWave(null));
            Assert.AreEqual(25, ExpandPickPolicy.CrossUnlockWave(null));
            Assert.AreEqual(4, ExpandPickPolicy.TipCap(null));
            Assert.AreEqual(0.30f, ExpandPickPolicy.SplitP(null), 0.0001f);
        }

        [Test]
        public void IsClosingWindow_MatchesRemainingTips()
        {
            Assert.IsTrue(ExpandPickPolicy.IsClosingWindow(47, 4, 50));
            Assert.IsFalse(ExpandPickPolicy.IsClosingWindow(47, 3, 50));
            Assert.IsTrue(ExpandPickPolicy.IsClosingWindow(49, 2, 50));
            Assert.IsFalse(ExpandPickPolicy.IsClosingWindow(49, 1, 50));
            Assert.IsFalse(ExpandPickPolicy.IsClosingWindow(1, 1, 50));
        }

        [Test]
        public void SkipExpand_TrueAtOrPastEndWave()
        {
            Assert.IsFalse(ExpandPickPolicy.SkipExpand(49, 50));
            Assert.IsTrue(ExpandPickPolicy.SkipExpand(50, 50));
            Assert.IsTrue(ExpandPickPolicy.SkipExpand(51, 50));
            Assert.IsFalse(ExpandPickPolicy.SkipExpand(50, 0));
        }

        [Test]
        public void ExpansionTurnsRemaining_ExcludesFinalWave()
        {
            Assert.AreEqual(3, ExpandPickPolicy.ExpansionTurnsRemaining(47, 50));
            Assert.AreEqual(1, ExpandPickPolicy.ExpansionTurnsRemaining(49, 50));
            Assert.AreEqual(0, ExpandPickPolicy.ExpansionTurnsRemaining(50, 50));
        }

        [Test]
        public void ShouldExpand_EveryPreFinalWave_MatchesSkipExpand()
        {
            Assert.IsTrue(ExpandPickPolicy.ShouldExpand(1, 50));
            Assert.IsTrue(ExpandPickPolicy.ShouldExpand(49, 50));
            Assert.IsFalse(ExpandPickPolicy.ShouldExpand(50, 50));
            Assert.IsTrue(ExpandPickPolicy.SkipExpand(50, 50));
        }

        [Test]
        public void ShouldExpand_OddUpcomingWaves_SkipsEvenWaves()
        {
            const ExpansionCadence cadence = ExpansionCadence.OddUpcomingWaves;
            Assert.IsTrue(ExpandPickPolicy.ShouldExpand(1, 30, cadence));
            Assert.IsFalse(ExpandPickPolicy.ShouldExpand(2, 30, cadence));
            Assert.IsTrue(ExpandPickPolicy.ShouldExpand(29, 30, cadence));
            Assert.IsFalse(ExpandPickPolicy.ShouldExpand(30, 30, cadence));
            Assert.IsTrue(ExpandPickPolicy.SkipExpand(28, 30, cadence));
        }

        [Test]
        public void ExpansionTurnsRemaining_OddUpcomingWaves_CountsOddSlots()
        {
            const ExpansionCadence cadence = ExpansionCadence.OddUpcomingWaves;
            Assert.AreEqual(15, ExpandPickPolicy.ExpansionTurnsRemaining(1, 30, cadence));
            Assert.AreEqual(14, ExpandPickPolicy.ExpansionTurnsRemaining(2, 30, cadence));
            Assert.AreEqual(1, ExpandPickPolicy.ExpansionTurnsRemaining(29, 30, cadence));
            Assert.AreEqual(0, ExpandPickPolicy.ExpansionTurnsRemaining(30, 30, cadence));
        }
    }
}
