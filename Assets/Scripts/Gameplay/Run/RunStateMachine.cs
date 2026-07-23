using System;
using UnityEngine;
using GemTD.Core;

namespace GemTD.Gameplay.Run
{
    /// <summary>
    /// Explicit run phase machine. Draft → Plan → Combat → (Draft?) → Plan …
    /// </summary>
    public sealed class RunStateMachine
    {
        public RunStateId Current { get; private set; } = RunStateId.Boot;

        public bool ExpandSatisfiedThisCycle { get; private set; }

        public event Action<RunStateId, RunStateId> StateChanged;

        readonly RunClock _clock;

        public RunStateMachine(RunClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public void ForceState(RunStateId next)
        {
            if (next == Current) return;
            var prev = Current;
            Current = next;
            ApplyClockForState(next);
            StateChanged?.Invoke(prev, next);
            Debug.Log($"[RunState] {prev} → {next}");
        }

        public void StartRun() => ForceState(RunStateId.Draft);

        public void DraftResolved()
        {
            Ensure(RunStateId.Draft);
            ExpandSatisfiedThisCycle = false;
            ForceState(RunStateId.Plan);
        }

        public void NotifyExpandDone()
        {
            Ensure(RunStateId.Plan);
            ExpandSatisfiedThisCycle = true;
        }

        public void WaiveExpandRequirement() => NotifyExpandDone();

        /// <summary>
        /// Obsolete PR4 migration shim until Task 10 rewires CompositionRoot / tests.
        /// Old Expand→Build becomes Plan + expand satisfied (and Draft→Plan if still in starter Draft).
        /// </summary>
        public void ExpandConfirmed()
        {
            if (Current == RunStateId.Draft)
                DraftResolved();

            if (Current == RunStateId.Plan)
            {
                NotifyExpandDone();
                return;
            }

            throw new InvalidOperationException($"Invalid transition from {Current}");
        }

        public void StartWave()
        {
            Ensure(RunStateId.Plan);
            if (!ExpandSatisfiedThisCycle)
                throw new InvalidOperationException("Expand required before Start Wave");
            ForceState(RunStateId.Combat);
        }

        public void WaveCleared(bool offerDraft, bool endsCampaign = false)
        {
            Ensure(RunStateId.Combat, RunStateId.Boss);
            if (endsCampaign)
            {
                ForceState(RunStateId.VictorySummary);
                return;
            }

            if (offerDraft)
            {
                ForceState(RunStateId.Draft);
                return;
            }

            ExpandSatisfiedThisCycle = false;
            ForceState(RunStateId.Plan);
        }

        public void TriggerDefeat() => ForceState(RunStateId.Defeat);

        void Ensure(params RunStateId[] allowed)
        {
            for (var i = 0; i < allowed.Length; i++)
            {
                if (Current == allowed[i]) return;
            }

            throw new InvalidOperationException($"Invalid transition from {Current}");
        }

        void ApplyClockForState(RunStateId state)
        {
            switch (state)
            {
                case RunStateId.Combat:
                case RunStateId.Boss:
                case RunStateId.Endless:
                    _clock.SetPaused(false);
                    break;
                case RunStateId.Draft:
                case RunStateId.Defeat:
                case RunStateId.VictorySummary:
                    _clock.SetPaused(true);
                    break;
                default:
                    _clock.SetPaused(false);
                    break;
            }
        }
    }
}
