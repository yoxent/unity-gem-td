using UnityEngine;
using UnityEngine.InputSystem;
using GemTD.Core;
using GemTD.Gameplay.Run;

namespace GemTD.Gameplay
{
    /// <summary>
    /// Scene composition root. Owns service lifetimes for a Run.
    /// </summary>
    public sealed class GameCompositionRoot : MonoBehaviour
    {
        public static GameCompositionRoot Instance { get; private set; }

        public RunClock Clock { get; private set; }
        public RunStateMachine States { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Clock = new RunClock();
            States = new RunStateMachine(Clock);
            GameEvents.ClearAll();
        }

        void Start()
        {
            States.StartRun();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            GameEvents.ClearAll();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void Update()
        {
            // Temporary cycle for Phase 1 scaffold verification (remove when UI wired).
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.f5Key.wasPressedThisFrame)
                return;

            switch (States.Current)
            {
                case RunStateId.Expand:
                    States.ExpandConfirmed();
                    break;
                case RunStateId.Build:
                    States.StartWave();
                    break;
                case RunStateId.Combat:
                    States.WaveCleared(offerDraft: false);
                    break;
            }
        }
#endif
    }
}
