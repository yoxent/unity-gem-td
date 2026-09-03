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
            AudioCue received = null;
            var cue = ScriptableObject.CreateInstance<AudioCue>();
            GameEvents.PlaySfx += c => received = c;
            GameEvents.RaisePlaySfx(cue);
            Assert.AreSame(cue, received);
            GameEvents.ClearAll();
            GameEvents.RaisePlaySfx(cue);
            Assert.AreSame(cue, received);
            Object.DestroyImmediate(cue);
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
    }
}