using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GemTD.Core;
using GemTD.Gameplay;
using GemTD.Gameplay.Run;

namespace GemTD.UI
{
    /// <summary>Top HUD: lives/gold/wave/state/defeat text + Esc layered close router.</summary>
    public sealed class TopHudController : MonoBehaviour
    {
        [SerializeField] TMP_Text livesText;
        [SerializeField] TMP_Text goldText;
        [SerializeField] TMP_Text waveText;
        [SerializeField] TMP_Text stateText;
        [SerializeField] TMP_Text defeatText;

        GameCompositionRoot _root;
        PopupManager _popup;
        SettingsController _settings;

        void OnEnable()
        {
            GameEvents.GoldChanged += OnGoldChanged;
            GameEvents.LivesChanged += OnLivesChanged;
            GameEvents.WaveChanged += OnWaveChanged;
            GameEvents.RunStateChanged += RefreshRunState;
            GameEvents.RequestCloseTopMost += OnRequestCloseTopMost;
        }

        void OnDisable()
        {
            GameEvents.GoldChanged -= OnGoldChanged;
            GameEvents.LivesChanged -= OnLivesChanged;
            GameEvents.WaveChanged -= OnWaveChanged;
            GameEvents.RunStateChanged -= RefreshRunState;
            GameEvents.RequestCloseTopMost -= OnRequestCloseTopMost;
        }

        public void Bind(GameCompositionRoot root, PopupManager popup)
        {
            _root = root;
            _popup = popup;
            if (_root == null) return;

            OnGoldChanged(_root.Economy != null ? _root.Economy.Gold : 0);
            OnLivesChanged(_root.Economy != null ? _root.Economy.Lives : 0);
            OnWaveChanged(_root.CurrentWaveNumber);
            RefreshRunState();
        }

        void OnGoldChanged(int gold) { if (goldText != null) goldText.text = $"Gold {gold}"; }
        void OnLivesChanged(int lives) { if (livesText != null) livesText.text = $"Lives {lives}"; }
        void OnWaveChanged(int wave) { if (waveText != null) waveText.text = wave > 0 ? $"Wave {wave}" : ""; }

        void RefreshRunState()
        {
            if (_root == null) return;
            if (stateText != null && _root.States != null)
            {
                var planLocked = _root.States.Current == RunStateId.Plan && !_root.States.ExpandSatisfiedThisCycle;
                stateText.text = $"State: {_root.States.Current}" + (planLocked ? " (expand)" : "");
            }
            if (defeatText != null)
                defeatText.gameObject.SetActive(false);
        }

        public void BindSettings(SettingsController settingsController)
        {
            _settings = settingsController;
        }

        void OnRequestCloseTopMost()
        {
            if (_root == null) return;
            if (_popup != null && _popup.IsOpen) { _popup.Hide(); return; }
            if (_settings != null && _settings.IsOpen) { _settings.Close(); return; }
            if (_root.CodexPanelOpen) { _root.ToggleCodexPanel(); return; }
            if (_root.HasPlaceTowerSelected) { _root.ClearPlaceTower(); return; }
            if (_root.HasSelectedTower) { _root.ClearTowerSelection(); return; }
            if (_settings != null) _settings.Open();
            else Debug.LogError("TopHudController: settings is not assigned.", this);
        }
    }
}
