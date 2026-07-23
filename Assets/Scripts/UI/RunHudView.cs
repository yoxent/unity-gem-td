using UnityEngine;
using UnityEngine.UI;
using GemTD.Core;
using GemTD.Gameplay;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;

namespace GemTD.UI
{
    /// <summary>Greybox run HUD: lives/gold/wave/state, Start Wave, Sell, gem sockets.</summary>
    public sealed class RunHudView : MonoBehaviour
    {
        const string SuppressSellGemWarningKey = "GemTD.SuppressSellGemWarning";

        [SerializeField] Text livesText;
        [SerializeField] Text goldText;
        [SerializeField] Text waveText;
        [SerializeField] Text stateText;
        [SerializeField] Text defeatText;
        [SerializeField] Button startWaveButton;
        [SerializeField] Button sellButton;
        [SerializeField] Button socketLmpButton;
        [SerializeField] Button socketChainButton;

        [Header("Sell confirm (socketed gems)")]
        [SerializeField] GameObject sellConfirmPanel;
        [SerializeField] Text sellConfirmMessage;
        [SerializeField] Toggle sellConfirmDontShowAgain;
        [SerializeField] Button sellConfirmYesButton;
        [SerializeField] Button sellConfirmNoButton;

        GameCompositionRoot _root;
        bool _buttonsBound;

        void OnEnable()
        {
            BindGameEvents();
            BindButtons();
        }

        void OnDisable()
        {
            UnbindGameEvents();
            UnbindButtons();
        }

        void Start()
        {
            // Re-bind after composition root Awake may have called GameEvents.ClearAll().
            BindGameEvents();
            _root = GameCompositionRoot.Instance;
            HideSellConfirm();
            RefreshAll();
        }

        void Update()
        {
            if (_root == null)
                _root = GameCompositionRoot.Instance;
            if (_root == null)
                return;

            // Source of truth for economy/wave — survives ClearAll race with static events.
            if (_root.Economy != null)
            {
                OnGoldChanged(_root.Economy.Gold);
                OnLivesChanged(_root.Economy.Lives);
            }
            OnWaveChanged(_root.CurrentWaveNumber);

            if (stateText != null && _root.States != null)
                stateText.text = $"State: {_root.States.Current}";

            if (defeatText != null)
            {
                var defeated = _root.States != null && _root.States.Current == RunStateId.Defeat;
                defeatText.gameObject.SetActive(defeated);
                if (defeated)
                    defeatText.text = "Defeat — restart Play Mode";
            }

            var build = _root.States != null && _root.States.Current == RunStateId.Build;
            var confirmOpen = sellConfirmPanel != null && sellConfirmPanel.activeSelf;
            if (startWaveButton != null)
                startWaveButton.interactable = build && !confirmOpen;
            if (sellButton != null)
                sellButton.interactable = build && _root.HasSelectedTower && !confirmOpen;
            if (socketLmpButton != null)
                socketLmpButton.interactable = build && _root.HasSelectedTower && !confirmOpen;
            if (socketChainButton != null)
                socketChainButton.interactable = build && _root.HasSelectedTower && !confirmOpen;
        }

        void BindGameEvents()
        {
            UnbindGameEvents();
            GameEvents.GoldChanged += OnGoldChanged;
            GameEvents.LivesChanged += OnLivesChanged;
            GameEvents.WaveChanged += OnWaveChanged;
        }

        void UnbindGameEvents()
        {
            GameEvents.GoldChanged -= OnGoldChanged;
            GameEvents.LivesChanged -= OnLivesChanged;
            GameEvents.WaveChanged -= OnWaveChanged;
        }

        void BindButtons()
        {
            if (_buttonsBound)
                return;
            if (startWaveButton != null)
                startWaveButton.onClick.AddListener(OnStartWave);
            if (sellButton != null)
                sellButton.onClick.AddListener(OnSell);
            if (socketLmpButton != null)
                socketLmpButton.onClick.AddListener(OnSocketLmp);
            if (socketChainButton != null)
                socketChainButton.onClick.AddListener(OnSocketChain);
            if (sellConfirmYesButton != null)
                sellConfirmYesButton.onClick.AddListener(OnSellConfirmYes);
            if (sellConfirmNoButton != null)
                sellConfirmNoButton.onClick.AddListener(OnSellConfirmNo);
            _buttonsBound = true;
        }

        void UnbindButtons()
        {
            if (!_buttonsBound)
                return;
            if (startWaveButton != null)
                startWaveButton.onClick.RemoveListener(OnStartWave);
            if (sellButton != null)
                sellButton.onClick.RemoveListener(OnSell);
            if (socketLmpButton != null)
                socketLmpButton.onClick.RemoveListener(OnSocketLmp);
            if (socketChainButton != null)
                socketChainButton.onClick.RemoveListener(OnSocketChain);
            if (sellConfirmYesButton != null)
                sellConfirmYesButton.onClick.RemoveListener(OnSellConfirmYes);
            if (sellConfirmNoButton != null)
                sellConfirmNoButton.onClick.RemoveListener(OnSellConfirmNo);
            _buttonsBound = false;
        }

        void RefreshAll()
        {
            if (_root == null)
                return;
            if (_root.Economy != null)
            {
                OnGoldChanged(_root.Economy.Gold);
                OnLivesChanged(_root.Economy.Lives);
            }
            OnWaveChanged(_root.CurrentWaveNumber);
        }

        void OnGoldChanged(int gold)
        {
            if (goldText != null)
                goldText.text = $"Gold: {gold}";
        }

        void OnLivesChanged(int lives)
        {
            if (livesText != null)
                livesText.text = $"Lives: {lives}";
        }

        void OnWaveChanged(int wave)
        {
            if (waveText != null)
                waveText.text = $"Wave: {wave}";
        }

        void OnStartWave() => _root?.RequestStartWave();

        void OnSell()
        {
            if (_root == null || !_root.HasSelectedTower)
                return;

            if (_root.SelectedHasSocketedGems && !IsSellGemWarningSuppressed())
            {
                ShowSellConfirm();
                return;
            }

            _root.RequestSellSelected();
        }

        void OnSellConfirmYes()
        {
            if (sellConfirmDontShowAgain != null && sellConfirmDontShowAgain.isOn)
                PlayerPrefs.SetInt(SuppressSellGemWarningKey, 1);

            HideSellConfirm();
            _root?.RequestSellSelected();
        }

        void OnSellConfirmNo() => HideSellConfirm();

        void ShowSellConfirm()
        {
            if (sellConfirmPanel == null)
            {
                // Fallback if panel not wired: sell immediately (gems still return to inventory).
                _root?.RequestSellSelected();
                return;
            }

            if (sellConfirmMessage != null)
            {
                sellConfirmMessage.text =
                    "This tower has a socketed gem.\nSelling will unsocket the gem back to your inventory.";
            }

            if (sellConfirmDontShowAgain != null)
                sellConfirmDontShowAgain.isOn = false;

            sellConfirmPanel.SetActive(true);
        }

        void HideSellConfirm()
        {
            if (sellConfirmPanel != null)
                sellConfirmPanel.SetActive(false);
        }

        static bool IsSellGemWarningSuppressed() =>
            PlayerPrefs.GetInt(SuppressSellGemWarningKey, 0) != 0;

        void OnSocketLmp() => _root?.RequestSocket(GemId.Lmp);
        void OnSocketChain() => _root?.RequestSocket(GemId.Chain);
    }
}
