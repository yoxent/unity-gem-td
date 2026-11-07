using NUnit.Framework;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class TowerAttackPlaybackTests
    {
        [Test]
        public void TryStart_NoFireSteps_ReturnsFalse()
        {
            var playback = new TowerAttackPlayback();

            Assert.IsFalse(playback.TryStart(0, out var playIndex));
            Assert.AreEqual(-1, playIndex);
            Assert.IsFalse(playback.IsPlaying);
        }

        [Test]
        public void TryStart_OneStep_PlaysIndexZero()
        {
            var playback = new TowerAttackPlayback();

            Assert.IsTrue(playback.TryStart(1, out var playIndex));
            Assert.AreEqual(0, playIndex);
            Assert.IsTrue(playback.IsPlaying);
            Assert.AreEqual(0, playback.StepIndex);
        }

        [Test]
        public void TryAdvance_AfterSingleStep_ReturnsToIdle()
        {
            var playback = new TowerAttackPlayback();
            playback.TryStart(1, out _);

            Assert.IsFalse(playback.TryAdvance(1, out var playIndex));
            Assert.AreEqual(-1, playIndex);
            Assert.IsFalse(playback.IsPlaying);
        }

        [Test]
        public void TryAdvance_TwoSteps_PlaysSecondThenIdle()
        {
            var playback = new TowerAttackPlayback();
            playback.TryStart(2, out _);

            Assert.IsTrue(playback.TryAdvance(2, out var second));
            Assert.AreEqual(1, second);
            Assert.IsTrue(playback.IsPlaying);

            Assert.IsFalse(playback.TryAdvance(2, out var after));
            Assert.AreEqual(-1, after);
            Assert.IsFalse(playback.IsPlaying);
        }

        [Test]
        public void TryStart_WhilePlaying_RestartsAtFirstStep()
        {
            var playback = new TowerAttackPlayback();
            playback.TryStart(2, out _);
            playback.TryAdvance(2, out _);

            Assert.IsTrue(playback.TryStart(2, out var playIndex));
            Assert.AreEqual(0, playIndex);
            Assert.AreEqual(0, playback.StepIndex);
        }

        [Test]
        public void ClipSpeed_MatchingLengthAndWindow_IsOne()
        {
            Assert.AreEqual(1f, TowerAttackPlayback.ClipSpeed(1f, 1f), 1e-4f);
        }

        [Test]
        public void ClipSpeed_ShorterWindow_PlaysFaster()
        {
            Assert.AreEqual(1.25f, TowerAttackPlayback.ClipSpeed(1f, 0.8f), 1e-4f);
        }

        [Test]
        public void ClipSpeed_LongerWindow_SlowsClip()
        {
            Assert.AreEqual(0.5f, TowerAttackPlayback.ClipSpeed(0.5f, 1f), 1e-4f);
        }

        [Test]
        public void SimAnimatorSpeed_MultipliesClipBySimSpeed()
        {
            Assert.AreEqual(2.5f, TowerAttackPlayback.SimAnimatorSpeed(1.25f, 2f), 1e-4f);
            Assert.AreEqual(5f, TowerAttackPlayback.SimAnimatorSpeed(1.25f, 4f), 1e-4f);
        }

        [Test]
        public void SimAnimatorSpeed_PauseOrZero_IsZero()
        {
            Assert.AreEqual(0f, TowerAttackPlayback.SimAnimatorSpeed(1.25f, 0f), 1e-4f);
            Assert.AreEqual(0f, TowerAttackPlayback.SimAnimatorSpeed(1.25f, -1f), 1e-4f);
        }

        [Test]
        public void ClipSpeed_NearZero_IsOne()
        {
            Assert.AreEqual(1f, TowerAttackPlayback.ClipSpeed(0f, 1f), 1e-4f);
            Assert.AreEqual(1f, TowerAttackPlayback.ClipSpeed(1f, 0f), 1e-4f);
        }

        [Test]
        public void ContactDelay_UsesStrikeNormalized()
        {
            Assert.AreEqual(0.5f, TowerAttackPlayback.ContactDelay(1f, 0.5f), 1e-4f);
            Assert.AreEqual(1f, TowerAttackPlayback.ContactDelay(1f, 1f), 1e-4f);
            Assert.AreEqual(0f, TowerAttackPlayback.ContactDelay(1f, 0f), 1e-4f);
            Assert.AreEqual(1f, TowerAttackPlayback.ContactDelay(1f, 2f), 1e-4f);
        }

        [Test]
        public void ClipWindow_SingleClip_FillsWholeInterval()
        {
            Assert.AreEqual(1f, TowerAttackPlayback.ClipWindow(1f, 0.6f, 0, 1), 1e-4f);
        }

        [Test]
        public void ClipWindow_TwoClips_DrawThenRelease()
        {
            Assert.AreEqual(0.8f, TowerAttackPlayback.ClipWindow(1f, 0.8f, 0, 2), 1e-4f);
            Assert.AreEqual(0.2f, TowerAttackPlayback.ClipWindow(1f, 0.8f, 1, 2), 1e-4f);
        }

        [Test]
        public void TryTickWindup_ZeroWindup_DoesNotAdvance()
        {
            var playback = new TowerAttackPlayback();
            playback.TryStart(2, out _);

            Assert.IsFalse(playback.TryTickWindup(1f, 0f, 2, out var playIndex));
            Assert.AreEqual(-1, playIndex);
            Assert.AreEqual(0, playback.StepIndex);
            Assert.IsTrue(playback.IsPlaying);
        }

        [Test]
        public void TryTickWindup_BeforeThreshold_DoesNotAdvance()
        {
            var playback = new TowerAttackPlayback();
            playback.TryStart(2, out _);

            Assert.IsFalse(playback.TryTickWindup(0.2f, 0.4f, 2, out _));
            Assert.IsFalse(playback.TryTickWindup(0.19f, 0.4f, 2, out var playIndex));
            Assert.AreEqual(-1, playIndex);
            Assert.AreEqual(0, playback.StepIndex);
        }

        [Test]
        public void TryTickWindup_SingleFireStep_DoesNotAdvance()
        {
            var playback = new TowerAttackPlayback();
            playback.TryStart(1, out _);

            Assert.IsFalse(playback.TryTickWindup(0.15f, 0.15f, 1, out var playIndex));
            Assert.AreEqual(-1, playIndex);
            Assert.AreEqual(0, playback.StepIndex);
            Assert.IsTrue(playback.IsPlaying);
        }

        [Test]
        public void TryTickWindup_ReachingThreshold_AdvancesToSecondStep()
        {
            var playback = new TowerAttackPlayback();
            playback.TryStart(2, out _);

            Assert.IsTrue(playback.TryTickWindup(0.4f, 0.4f, 2, out var playIndex));
            Assert.AreEqual(1, playIndex);
            Assert.AreEqual(1, playback.StepIndex);
            Assert.IsTrue(playback.IsPlaying);
        }

        [Test]
        public void TryTickWindup_AfterAdvance_DoesNotAdvanceAgain()
        {
            var playback = new TowerAttackPlayback();
            playback.TryStart(2, out _);
            playback.TryTickWindup(0.4f, 0.4f, 2, out _);

            Assert.IsFalse(playback.TryTickWindup(1f, 0.4f, 2, out var playIndex));
            Assert.AreEqual(-1, playIndex);
            Assert.AreEqual(1, playback.StepIndex);
        }

        [Test]
        public void TryStart_ResetsWindupElapsed()
        {
            var playback = new TowerAttackPlayback();
            playback.TryStart(2, out _);
            playback.TryTickWindup(0.3f, 0.4f, 2, out _);
            playback.TryStart(2, out _);

            Assert.IsFalse(playback.TryTickWindup(0.3f, 0.4f, 2, out _));
            Assert.AreEqual(0, playback.StepIndex);
        }
    }
}
