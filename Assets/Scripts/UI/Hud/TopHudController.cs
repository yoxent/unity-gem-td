using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GemTD.Core;
using GemTD.Gameplay;
using GemTD.Gameplay.Run;

namespace GemTD.UI
{
    /// <summary>Top HUD: lives/gold/wave/state/defeat text + Start Wave button + Esc layered close router.</summary>
    public sealed class TopHudController : MonoBehaviour
    {
        [SerializeField] TMP_Text livesText;
        [SerializeField] TMP_Text goldText;
        [SerializeField] TMP_Text waveText;
        [SerializeField] TMP_Text stateText;
        [SerializeField] TMP_Text defeatText;
        [SerializeField] Button startWaveButton;

        GameCompositionRoot _root;
        bool _bound;

        void OnEnable()
        {
            GameEvents.GoldChanged += OnGoldChanged;
            GameEvents.LivesChanged += OnLivesChanged;
            GameEvents.WaveChanged += OnWaveChanged;
            GameEvents.RequestCloseTopMost += OnRequestCloseTopMost;
        }

        void OnDisable()
        {
            GameEvents.GoldChanged -= OnGoldChanged;
            GameEvents.LivesChanged -= OnLivesChanged;
            GameEvents.WaveChanged -= OnWaveChanged;
            GameEvents.RequestCloseTopMost -= OnRequestCloseTopMost;
        }

        void Update()
        {
            if (_root == null) _root = GameCompositionRoot.Instance;
            if (_root == null || _bound) return;
            _bound = true;
            OnGoldChanged(_root.Economy != null ? _root.Economy.Gold : 0);
            OnLivesChanged(_root.Economy != null ? _root.Economy.Lives : 0);
            OnWaveChanged(_root.CurrentWaveNumber);
            if (startWaveButton != null)
                startWaveButton.onClick.AddListener(() => _root.RequestStartWave());
        }

        void OnGoldChanged(int gold) { if (goldText != null) goldText.text = $"Gold {gold}"; }
        void OnLivesChanged(int lives) { if (livesText != null) livesText.text = $"Lives {lives}"; }
        void OnWaveChanged(int wave) { if (waveText != null) waveText.text = wave > 0 ? $"Wave {wave}" : ""; }

        void LateUpdate()
        {
            if (_root == null) return;
            if (stateText != null && _root.States != null)
            {
                var planLocked = _root.States.Current == RunStateId.Plan && !_root.States.ExpandSatisfiedThisCycle;
                stateText.text = $"State: {_root.States.Current}" + (planLocked ? " (expand)" : "");
            }
            if (defeatText != null)
            {
                var defeat = _root.States != null && _root.States.Current == RunStateId.Defeat;
                var victory = _root.States != null && _root.States.Current == RunStateId.VictorySummary;
                defeatText.gameObject.SetActive(defeat || victory);
                if (defeat) defeatText.text = "DEFEAT";
                else if (victory) defeatText.text = "VICTORY";
            }
            if (startWaveButton != null)
            {
                var plan = _root.States != null && _root.States.Current == RunStateId.Plan;
                var draft = _root.States != null && _root.States.Current == RunStateId.Draft;
                startWaveButton.interactable = _root.CanStartWave && plan && !draft;
            }
        }

        // Layered Esc close: popup -> Codex -> cancel-place -> deselect tower.
        void OnRequestCloseTopMost()
        {
            if (_root == null) _root = GameCompositionRoot.Instance;
            if (_root == null) return;
            var popup = FindFirstObjectByType<PopupManager>(FindObjectsInactive.Include);
            if (popup != null && popup.IsOpen) { popup.Hide(); return; }
            if (_root.CodexPanelOpen) { _root.ToggleCodexPanel(); return; }
            if (_root.HasPlaceTowerSelected) { _root.ClearPlaceTower(); return; }
            if (_root.HasSelectedTower) { _root.ClearTowerSelection(); return; }
        }
    }
}