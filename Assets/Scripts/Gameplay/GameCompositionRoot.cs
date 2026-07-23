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

        InputAction _advancePhase;
        InputActionMap _debugMap;

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _debugMap = new InputActionMap("RunDebug");
            _advancePhase = _debugMap.AddAction("AdvancePhase", InputActionType.Button);
            _advancePhase.AddBinding("<Keyboard>/space");
            _advancePhase.AddBinding("<Keyboard>/f5");
            _debugMap.Enable();
#endif
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
            _debugMap?.Dispose();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void Update()
        {
            // Temporary cycle for Phase 1 scaffold verification (remove when UI wired).
            if (_advancePhase == null || !_advancePhase.WasPressedThisFrame())
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
