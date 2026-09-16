using NUnit.Framework;
using UnityEngine;
using GemTD.Core;

namespace GemTD.Tests.EditMode
{
    public sealed class GameEventsTests
    {
        [TearDown]
        public void TearDown() => GameEvents.ClearAll();

        [Test]
        public void SpeedChanged_RaisesAndClears()
        {
            float received = -1f;
            GameEvents.SpeedChanged += s => received = s;
            GameEvents.RaiseSpeedChanged(2f);
            Assert.AreEqual(2f, received);
            GameEvents.ClearAll();
            GameEvents.RaiseSpeedChanged(4f); // no subscriber after clear
            Assert.AreEqual(2f, received);
        }

        [Test]
        public void PauseChanged_RaisesAndClears()
        {
            bool received = false;
            GameEvents.PauseChanged += p => received = p;
            GameEvents.RaisePauseChanged(true);
            Assert.IsTrue(received);
            GameEvents.ClearAll();
            GameEvents.RaisePauseChanged(false);
            Assert.IsTrue(received); // unchanged after clear
        }

        [Test]
        public void EvolutionUnlocked_RaisesOnce()
        {
            int count = 0;
            GameEvents.EvolutionUnlocked += () => count++;
            GameEvents.RaiseEvolutionUnlocked();
            Assert.AreEqual(1, count);
        }

        [Test]
        public void RequestTargetingAllConfirm_RaisesAndClears()
        {
            var count = 0;
            GameEvents.RequestTargetingAllConfirm += () => count++;
            GameEvents.RaiseRequestTargetingAllConfirm();
            Assert.AreEqual(1, count);
            GameEvents.ClearAll();
            GameEvents.RaiseRequestTargetingAllConfirm();
            Assert.AreEqual(1, count);
        }

        [Test]
        public void PlaySfx_RaisesAndClears()
        {
            string received = null;
            GameEvents.PlaySfx += key => received = key;
            GameEvents.RaisePlaySfx("Click");
            Assert.AreEqual("Click", received);
            GameEvents.ClearAll();
            GameEvents.RaisePlaySfx("Drop");
            Assert.AreEqual("Click", received);
        }

        [Test]
        public void PlayBgm_RaisesAndClears()
        {
            AudioCue received = null;
            var cue = ScriptableObject.CreateInstance<AudioCue>();
            GameEvents.PlayBgm += c => received = c;
            GameEvents.RaisePlayBgm(cue);
            Assert.AreSame(cue, received);
            GameEvents.ClearAll();
            received = null;
            GameEvents.RaisePlayBgm(cue);
            Assert.IsNull(received);
            Object.DestroyImmediate(cue);
        }

        [Test]
        public void StopBgm_RaisesAndClears()
        {
            var count = 0;
            GameEvents.StopBgm += () => count++;
            GameEvents.RaiseStopBgm();
            Assert.AreEqual(1, count);
            GameEvents.ClearAll();
            GameEvents.RaiseStopBgm();
            Assert.AreEqual(1, count);
        }

        [Test]
        public void CameraShake_RaisesAndClears()
        {
            CameraShakeRequest received = default;
            var count = 0;
            GameEvents.CameraShake += r =>
            {
                received = r;
                count++;
            };
            GameEvents.RaiseCameraShake(0.04f);
            Assert.AreEqual(1, count);
            Assert.AreEqual(0.04f, received.Intensity, 1e-4f);
            GameEvents.ClearAll();
            GameEvents.RaiseCameraShake(0.08f);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void CameraShake_NoArg_UsesDefaultRequest()
        {
            CameraShakeRequest received = default;
            GameEvents.CameraShake += r => received = r;
            GameEvents.RaiseCameraShake();
            Assert.AreEqual(-1f, received.Intensity, 1e-4f);
            Assert.AreEqual(CameraShakeKind.Burst, received.Kind);
        }

        [Test]
        public void CameraShakeFor_RaisesDurationKind()
        {
            CameraShakeRequest received = default;
            GameEvents.CameraShake += r => received = r;
            GameEvents.RaiseCameraShakeFor(0.5f, 0.04f);
            Assert.AreEqual(CameraShakeKind.Duration, received.Kind);
            Assert.AreEqual(0.5f, received.Duration, 1e-4f);
            Assert.AreEqual(0.04f, received.Intensity, 1e-4f);
        }

        [Test]
        public void CameraShakeContinuous_RaisesContinuousKind()
        {
            CameraShakeRequest received = default;
            GameEvents.CameraShake += r => received = r;
            GameEvents.RaiseCameraShakeContinuous(0.03f);
            Assert.AreEqual(CameraShakeKind.Continuous, received.Kind);
            Assert.AreEqual(0.03f, received.Intensity, 1e-4f);
        }
    }
}