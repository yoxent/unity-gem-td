using System;
using NUnit.Framework;
using GemTD.Core;
using GemTD.Gameplay.Run;

namespace GemTD.Tests.EditMode
{
    public sealed class RunStateMachineTests
    {
        static RunStateMachine ReadyCombat(bool expandSatisfied = true)
        {
            var fsm = new RunStateMachine(new RunClock());
            fsm.StartRun();
            fsm.DraftResolved();
            if (expandSatisfied)
                fsm.NotifyExpandDone();
            fsm.StartWave();
            return fsm;
        }

        [Test]
        public void StartRun_EntersDraft()
        {
            var fsm = new RunStateMachine(new RunClock());
            fsm.StartRun();
            Assert.AreEqual(RunStateId.Draft, fsm.Current);
        }

        [Test]
        public void DraftResolved_EntersPlan_ExpandUnsatisfied()
        {
            var fsm = new RunStateMachine(new RunClock());
            fsm.StartRun();
            fsm.DraftResolved();
            Assert.AreEqual(RunStateId.Plan, fsm.Current);
            Assert.IsFalse(fsm.ExpandSatisfiedThisCycle);
        }

        [Test]
        public void StartWave_RequiresExpandSatisfied()
        {
            var fsm = new RunStateMachine(new RunClock());
            fsm.StartRun();
            fsm.DraftResolved();
            Assert.Throws<InvalidOperationException>(() => fsm.StartWave());
            fsm.NotifyExpandDone();
            fsm.StartWave();
            Assert.AreEqual(RunStateId.Combat, fsm.Current);
        }

        [Test]
        public void WaveCleared_OfferDraft_GoesDraft()
        {
            var fsm = ReadyCombat();
            fsm.WaveCleared(offerDraft: true);
            Assert.AreEqual(RunStateId.Draft, fsm.Current);
        }

        [Test]
        public void WaveCleared_EndsCampaign_GoesVictory()
        {
            var fsm = ReadyCombat();
            fsm.WaveCleared(offerDraft: false, endsCampaign: true);
            Assert.AreEqual(RunStateId.VictorySummary, fsm.Current);
        }

        [Test]
        public void WaveCleared_NoDraft_GoesPlan_ResetsExpandGate()
        {
            var fsm = ReadyCombat();
            fsm.WaveCleared(offerDraft: false);
            Assert.AreEqual(RunStateId.Plan, fsm.Current);
            Assert.IsFalse(fsm.ExpandSatisfiedThisCycle);
        }
    }
}
