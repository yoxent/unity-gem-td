using NUnit.Framework;
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
    }
}