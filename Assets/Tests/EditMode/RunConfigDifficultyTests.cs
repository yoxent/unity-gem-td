using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Run;

namespace GemTD.Tests.EditMode
{
    public sealed class RunConfigDifficultyTests
    {
        [Test]
        public void LaneCount_ClampsOpenArmCountToOneThroughFour()
        {
            var config = ScriptableObject.CreateInstance<RunConfig>();

            config.OpenArmCount = 0;
            Assert.AreEqual(1, config.LaneCount);

            config.OpenArmCount = 5;
            Assert.AreEqual(4, config.LaneCount);

            config.OpenArmCount = 3;
            Assert.AreEqual(3, config.LaneCount);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void EndWave_DefaultIsFifty()
        {
            var config = ScriptableObject.CreateInstance<RunConfig>();
            Assert.AreEqual(50, config.EndWave);
            Object.DestroyImmediate(config);
        }

        [TestCase(1, 8, 25, 4, 0.30f, 1.0f)]
        [TestCase(2, 7, 22, 6, 0.32f, 1.1f)]
        [TestCase(3, 6, 18, 8, 0.34f, 1.25f)]
        [TestCase(4, 5, 15, 10, 0.36f, 1.4f)]
        public void DifficultyResolvers_MatchDesignDefaults(
            int lanes,
            int firstSplitWave,
            int crossUnlockWave,
            int tipCap,
            float splitP,
            float hpMultiplier)
        {
            var config = ScriptableObject.CreateInstance<RunConfig>();
            config.OpenArmCount = lanes;

            Assert.AreEqual(lanes, config.LaneCount);
            Assert.AreEqual(firstSplitWave, config.GetFirstSplitWave());
            Assert.AreEqual(crossUnlockWave, config.GetCrossUnlockWave());
            Assert.AreEqual(tipCap, config.GetTipCap());
            Assert.AreEqual(splitP, config.GetSplitP(), 0.0001f);
            Assert.AreEqual(hpMultiplier, config.GetHpMultiplier(), 0.0001f);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void DifficultyResolvers_OutOfRangeOpenArmCount_UsesClampedLaneRow()
        {
            var config = ScriptableObject.CreateInstance<RunConfig>();

            config.OpenArmCount = 0;
            Assert.AreEqual(8, config.GetFirstSplitWave());
            Assert.AreEqual(25, config.GetCrossUnlockWave());
            Assert.AreEqual(4, config.GetTipCap());
            Assert.AreEqual(0.30f, config.GetSplitP(), 0.0001f);
            Assert.AreEqual(1.0f, config.GetHpMultiplier(), 0.0001f);

            config.OpenArmCount = 99;
            Assert.AreEqual(5, config.GetFirstSplitWave());
            Assert.AreEqual(15, config.GetCrossUnlockWave());
            Assert.AreEqual(10, config.GetTipCap());
            Assert.AreEqual(0.36f, config.GetSplitP(), 0.0001f);
            Assert.AreEqual(1.4f, config.GetHpMultiplier(), 0.0001f);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void GetRosterCaps_Defaults_AreFiveTwoTwo_SumNine()
        {
            var config = ScriptableObject.CreateInstance<RunConfig>();
            var caps = config.GetRosterCaps();
            Assert.AreEqual(5, caps.MaxDamaging);
            Assert.AreEqual(2, caps.MaxCurse);
            Assert.AreEqual(2, caps.MaxAura);
            Assert.AreEqual(9, caps.MaxSlots);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void GetRosterCaps_DamagingFour_SumIsEight()
        {
            var config = ScriptableObject.CreateInstance<RunConfig>();
            config.MaxDamagingTowers = 4;
            Assert.AreEqual(8, config.GetRosterCaps().MaxSlots);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void GetRosterCaps_Negative_ClampsToZero()
        {
            var config = ScriptableObject.CreateInstance<RunConfig>();
            config.MaxDamagingTowers = -3;
            config.MaxCurseTowers = -1;
            config.MaxAuraTowers = 2;
            var caps = config.GetRosterCaps();
            Assert.AreEqual(0, caps.MaxDamaging);
            Assert.AreEqual(0, caps.MaxCurse);
            Assert.AreEqual(2, caps.MaxAura);
            Assert.AreEqual(2, caps.MaxSlots);
            Object.DestroyImmediate(config);
        }
    }
}
