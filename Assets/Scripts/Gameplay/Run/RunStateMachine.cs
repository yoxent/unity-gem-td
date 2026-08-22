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

        /// <summary>Set by <see cref="EnterEndless"/> after Victory — skip expand; combat+draft loop.</summary>
        public bool IsEndless { get; private set; }

        public event Action<RunStateId, RunStateId> StateChanged;

        readonly RunClock _clock;
        readonly SpeedControl _speed;

        public RunStateMachine(SpeedControl speed, RunClock clock)
        {
            _speed = speed ?? throw new ArgumentNullException(nameof(speed));
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

        public void StartRun()
        {
            _speed.ResetSpeedForNewRun();
            ForceState(RunStateId.Draft);
        }

        public void DraftResolved()
        {
            Ensure(RunStateId.Draft);
            // Endless: no expand — enter Plan already waived so combat can start immediately.
            ExpandSatisfiedThisCycle = IsEndless;
            ForceState(RunStateId.Plan);
        }

        /// <summary>Victory → Endless: leave summary, enter Plan with expand waived.</summary>
        public void EnterEndless()
        {
            Ensure(RunStateId.VictorySummary);
            IsEndless = true;
            ExpandSatisfiedThisCycle = true;
            ForceState(RunStateId.Plan);
        }

        public void NotifyExpandDone()
        {
            Ensure(RunStateId.Plan);
            ExpandSatisfiedThisCycle = true;
        }

        public void WaiveExpandRequirement() => NotifyExpandDone();

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

            // Endless: no Plan/expand — stay ready to start the next combat immediately.
            ExpandSatisfiedThisCycle = IsEndless;
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
                    _speed.PopPause("draft");
                    break;
                case RunStateId.Draft:
                case RunStateId.Defeat:
                case RunStateId.VictorySummary:
                    _speed.PushPause("draft");
                    break;
                default:
                    _speed.PushPause("draft"); // Plan stays paused
                    break;
            }
        }
    }
}
