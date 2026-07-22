using System;
using UnityEngine;
using GemTD.Core;

namespace GemTD.Gameplay.Run
{
    /// <summary>
    /// Explicit run phase machine. Expand → Build → Combat → (Draft?) → Expand …
    /// </summary>
    public sealed class RunStateMachine
    {
        public RunStateId Current { get; private set; } = RunStateId.Boot;

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

        public void StartRun()
        {
            ForceState(RunStateId.Expand);
        }

        public void ExpandConfirmed()
        {
            Ensure(RunStateId.Expand);
            ForceState(RunStateId.Build);
        }

        public void StartWave()
        {
            Ensure(RunStateId.Build);
            ForceState(RunStateId.Combat);
        }

        public void WaveCleared(bool offerDraft)
        {
            Ensure(RunStateId.Combat, RunStateId.Boss);
            ForceState(offerDraft ? RunStateId.Draft : RunStateId.Expand);
        }

        public void DraftResolved()
        {
            Ensure(RunStateId.Draft);
            ForceState(RunStateId.Expand);
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
