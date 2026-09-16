using NUnit.Framework;
using UnityEngine;
using GemTD.Core;

namespace GemTD.Tests.EditMode
{
    public sealed class AudioPitchTests
    {
        [Test]
        public void Resolve_Fixed_UsesPitch()
        {
            var data = new SfxData { randomPitch = false, pitch = 1.25f, pitchMin = 0.5f, pitchMax = 2f };
            Assert.AreEqual(1.25f, AudioPitch.Resolve(data, 0f), 0.0001f);
            Assert.AreEqual(1.25f, AudioPitch.Resolve(data, 1f), 0.0001f);
        }

        [Test]
        public void Resolve_Random_LerpsUnit01()
        {
            var data = new SfxData { randomPitch = true, pitch = 1f, pitchMin = 0.9f, pitchMax = 1.1f };
            Assert.AreEqual(0.9f, AudioPitch.Resolve(data, 0f), 0.0001f);
            Assert.AreEqual(1.1f, AudioPitch.Resolve(data, 1f), 0.0001f);
            Assert.AreEqual(1.0f, AudioPitch.Resolve(data, 0.5f), 0.0001f);
        }

        [Test]
        public void Resolve_Random_SwapsIfMinGreaterThanMax()
        {
            var data = new SfxData { randomPitch = true, pitchMin = 1.2f, pitchMax = 0.8f };
            Assert.AreEqual(0.8f, AudioPitch.Resolve(data, 0f), 0.0001f);
            Assert.AreEqual(1.2f, AudioPitch.Resolve(data, 1f), 0.0001f);
        }

        [Test]
        public void Resolve_ClampsUnit01()
        {
            var data = new SfxData { randomPitch = true, pitchMin = 0f, pitchMax = 10f };
            Assert.AreEqual(0f, AudioPitch.Resolve(data, -1f), 0.0001f);
            Assert.AreEqual(10f, AudioPitch.Resolve(data, 2f), 0.0001f);
        }

        [Test]
        public void BgmSourceVolume_IsCueTimesSlider()
        {
            Assert.AreEqual(0.8f, AudioMix.BgmSourceVolume(1f, 0.8f), 0.0001f);
            Assert.AreEqual(0.4f, AudioMix.BgmSourceVolume(0.5f, 0.8f), 0.0001f);
        }

        [Test]
        public void SfxSourceVolume_IsCueTimesSlider()
        {
            Assert.AreEqual(0.42f, AudioMix.SfxSourceVolume(0.6f, 0.7f), 0.0001f);
        }

        [Test]
        public void SfxDataDefault_HasExpectedPitch()
        {
            var d = SfxData.Default;
            Assert.IsFalse(d.randomPitch);
            Assert.AreEqual(1f, d.pitch, 0.0001f);
            Assert.AreEqual(0.9f, d.pitchMin, 0.0001f);
            Assert.AreEqual(1.1f, d.pitchMax, 0.0001f);
            Assert.IsNull(d.clip);
        }
    }
}
