using NUnit.Framework;
using GemTD.Core;

namespace GemTD.Tests.EditMode
{
    public sealed class SpeedControlTests
    {
        [Test]
        public void Defaults_Speed1_PausedFalse()
        {
            var sc = new SpeedControl(new RunClock());
            Assert.AreEqual(1f, sc.CurrentSpeed);
            Assert.IsFalse(sc.IsPaused);
        }

        [Test]
        public void SetSpeed_AcceptsValidSet()
        {
            var sc = new SpeedControl(new RunClock());
            sc.SetSpeed(2f);
            Assert.AreEqual(2f, sc.CurrentSpeed);
            sc.SetSpeed(4f);
            Assert.AreEqual(4f, sc.CurrentSpeed);
            sc.SetSpeed(1f);
            Assert.AreEqual(1f, sc.CurrentSpeed);
        }

        [Test]
        public void SetSpeed_RejectsInvalidValue()
        {
            var sc = new SpeedControl(new RunClock());
            Assert.Throws<System.ArgumentOutOfRangeException>(() => sc.SetSpeed(3f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => sc.SetSpeed(0f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => sc.SetSpeed(-1f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => sc.SetSpeed(2.5f));
        }

        [Test]
        public void TogglePause_UserPause_TogglesIsPaused()
        {
            var sc = new SpeedControl(new RunClock());
            sc.TogglePause();
            Assert.IsTrue(sc.IsPaused);
            sc.TogglePause();
            Assert.IsFalse(sc.IsPaused);
        }

        [Test]
        public void PushPause_IncrementsRef_PausedTrue()
        {
            var sc = new SpeedControl(new RunClock());
            sc.PushPause("draft");
            Assert.IsTrue(sc.IsPaused);
            sc.PushPause("popup");
            Assert.IsTrue(sc.IsPaused);
        }

        [Test]
        public void PopPause_AtZero_Resumes()
        {
            var sc = new SpeedControl(new RunClock());
            sc.PushPause("draft");
            sc.PushPause("popup");
            sc.PopPause("popup");
            Assert.IsTrue(sc.IsPaused);
            sc.PopPause("draft");
            Assert.IsFalse(sc.IsPaused);
        }

        [Test]
        public void PopPause_Unbalanced_NoOpBelowZero()
        {
            var sc = new SpeedControl(new RunClock());
            sc.PopPause("nothing");
            Assert.IsFalse(sc.IsPaused);
        }

        [Test]
        public void UserToggle_DoesNotUnpauseDraft_AfterDraftPops()
        {
            var sc = new SpeedControl(new RunClock());
            sc.PushPause("draft");
            sc.TogglePause();          // user also pauses
            Assert.IsTrue(sc.IsPaused);
            sc.PopPause("draft");       // draft ends; user-pause still active
            Assert.IsTrue(sc.IsPaused);
            sc.TogglePause();           // user resumes
            Assert.IsFalse(sc.IsPaused);
        }

        [Test]
        public void ResetSpeedForNewRun_SetsOne()
        {
            var sc = new SpeedControl(new RunClock());
            sc.SetSpeed(4f);
            sc.ResetSpeedForNewRun();
            Assert.AreEqual(1f, sc.CurrentSpeed);
        }
    }
}