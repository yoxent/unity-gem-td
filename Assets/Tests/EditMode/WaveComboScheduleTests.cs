using NUnit.Framework;
using GemTD.Gameplay.Run;

namespace GemTD.Tests.EditMode
{
    public sealed class WaveComboScheduleTests
    {
        [Test]
        public void ForWave_CampaignBands_IncludeTheMatchingBufferCombo()
        {
            Assert.AreEqual(WaveComboId.Rush, WaveComboSchedule.ForWave(16, false));
            Assert.AreEqual(WaveComboId.Rush, WaveComboSchedule.ForWave(19, false));
            Assert.AreEqual(WaveComboId.Bulwark, WaveComboSchedule.ForWave(20, false));
            Assert.AreEqual(WaveComboId.Ward, WaveComboSchedule.ForWave(21, false));
            Assert.AreEqual(WaveComboId.Ward, WaveComboSchedule.ForWave(24, false));
            Assert.AreEqual(WaveComboId.Rush, WaveComboSchedule.ForWave(25, false));
            Assert.AreEqual(WaveComboId.FullMix, WaveComboSchedule.ForWave(30, false));
            Assert.AreEqual(WaveComboId.Bulwark, WaveComboSchedule.ForWave(31, false));
            Assert.AreEqual(WaveComboId.Ward, WaveComboSchedule.ForWave(35, false));
            Assert.AreEqual(WaveComboId.FullMix, WaveComboSchedule.ForWave(40, false));
            Assert.AreEqual(WaveComboId.Rush, WaveComboSchedule.ForWave(41, false));
            Assert.AreEqual(WaveComboId.Bulwark, WaveComboSchedule.ForWave(45, false));
            Assert.AreEqual(WaveComboId.FullMix, WaveComboSchedule.ForWave(50, false));
        }

        [Test]
        public void ForWave_Endless_RotatesOneComboPerWave()
        {
            Assert.AreEqual(WaveComboId.Rush, WaveComboSchedule.ForWave(51, true));
            Assert.AreEqual(WaveComboId.Bulwark, WaveComboSchedule.ForWave(52, true));
            Assert.AreEqual(WaveComboId.Ward, WaveComboSchedule.ForWave(53, true));
            Assert.AreEqual(WaveComboId.FullMix, WaveComboSchedule.ForWave(54, true));
            Assert.AreEqual(WaveComboId.Rush, WaveComboSchedule.ForWave(55, true));
        }
    }
}
