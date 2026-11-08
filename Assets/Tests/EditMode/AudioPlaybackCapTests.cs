using NUnit.Framework;
using GemTD.Core;

namespace GemTD.Tests.EditMode
{
    public sealed class AudioPlaybackCapTests
    {
        [Test]
        public void AudibleLength_UnityPitch_IsClipLength()
        {
            Assert.AreEqual(1.109f, AudioMix.AudibleLengthSeconds(1.109f, 1f), 0.0001f);
        }

        [Test]
        public void AudibleLength_HigherPitch_Shortens()
        {
            // 1.1x pitch plays the same samples faster.
            Assert.AreEqual(1f, AudioMix.AudibleLengthSeconds(1.109f, 1.109f), 0.0001f);
        }

        [Test]
        public void AudibleLength_LowerPitch_Lengthens()
        {
            Assert.AreEqual(2f, AudioMix.AudibleLengthSeconds(1f, 0.5f), 0.0001f);
        }

        [Test]
        public void AudibleLength_ZeroPitch_FallsBackToGuard()
        {
            // Must not divide by zero; guard floor is 0.01.
            Assert.AreEqual(100f, AudioMix.AudibleLengthSeconds(1f, 0f), 0.0001f);
        }

        [Test]
        public void AudibleLength_ZeroClip_IsZero()
        {
            Assert.AreEqual(0f, AudioMix.AudibleLengthSeconds(0f, 1f), 0.0001f);
        }

        [Test]
        public void Cap_DisabledWhenZero()
        {
            Assert.AreEqual(0f, AudioMix.ResolvePlaybackCapSeconds(1.109f, 1f, 0f), 0.0001f);
        }

        [Test]
        public void Cap_DisabledWhenNegative()
        {
            Assert.AreEqual(0f, AudioMix.ResolvePlaybackCapSeconds(1.109f, 1f, -1f), 0.0001f);
        }

        [Test]
        public void Cap_AppliesWhenClipLongerThanCap()
        {
            // The tower-shoot case: 1.109s clip, 0.2s cap.
            Assert.AreEqual(0.2f, AudioMix.ResolvePlaybackCapSeconds(1.109f, 1f, 0.2f), 0.0001f);
        }

        [Test]
        public void Cap_NotAppliedWhenClipAlreadyShorter()
        {
            // A properly trimmed 0.18s clip needs no schedule.
            Assert.AreEqual(0f, AudioMix.ResolvePlaybackCapSeconds(0.18f, 1f, 0.2f), 0.0001f);
        }

        [Test]
        public void Cap_NotAppliedAtExactLength()
        {
            Assert.AreEqual(0f, AudioMix.ResolvePlaybackCapSeconds(0.2f, 1f, 0.2f), 0.0001f);
        }

        [Test]
        public void Cap_UsesAudibleLengthNotRawLength()
        {
            // 0.3s clip pitched down to 0.5x lasts 0.6s, so a 0.2s cap applies.
            Assert.AreEqual(0.2f, AudioMix.ResolvePlaybackCapSeconds(0.3f, 0.5f, 0.2f), 0.0001f);
        }

        [Test]
        public void Cap_SwappingInLongerClipNeedsNoReTrim()
        {
            // The reason maxPlaybackSeconds exists: any candidate length works.
            foreach (var length in new[] { 0.2f, 1.109f, 1.147f, 3f })
            {
                var cap = AudioMix.ResolvePlaybackCapSeconds(length, 1f, 0.2f);
                var effective = cap > 0f ? cap : length;
                Assert.LessOrEqual(effective, 0.2001f, "length " + length);
            }
        }

        [Test]
        public void SfxDataDefault_HasNoTimingLimits()
        {
            var d = SfxData.Default;
            Assert.AreEqual(0f, d.minIntervalSeconds, 0.0001f);
            Assert.AreEqual(0f, d.maxPlaybackSeconds, 0.0001f);
        }
    }
}
