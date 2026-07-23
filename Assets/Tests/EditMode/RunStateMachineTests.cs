using NUnit.Framework;
using GemTD.Core;
using GemTD.Gameplay.Run;

namespace GemTD.Tests.EditMode
{
    public sealed class RunStateMachineTests
    {
        [Test]
        public void StartRun_EntersExpand()
        {
            var clock = new RunClock();
            var fsm = new RunStateMachine(clock);
            fsm.StartRun();
            Assert.AreEqual(RunStateId.Expand, fsm.Current);
        }

        [Test]
        public void Cycle_ExpandBuildCombatExpand()
        {
            var clock = new RunClock();
            var fsm = new RunStateMachine(clock);
            fsm.StartRun();
            fsm.ExpandConfirmed();
            Assert.AreEqual(RunStateId.Build, fsm.Current);
            fsm.StartWave();
            Assert.AreEqual(RunStateId.Combat, fsm.Current);
            fsm.WaveCleared(offerDraft: false);
            Assert.AreEqual(RunStateId.Expand, fsm.Current);
        }
    }
}
