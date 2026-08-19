using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GemTD.Core;

namespace GemTD.UI
{
    public sealed class SettingsController : MonoBehaviour
    {
        public const string QuitToMenuPopupId = "QuitToMenu";

        [SerializeField] GameObject rootPanel;
        [SerializeField] SliderHandler masterSlider;
        [SerializeField] SliderHandler bgmSlider;
        [SerializeField] SliderHandler sfxSlider;
        [SerializeField] Button closeButton;
        [SerializeField] Button quitToMenuButton;
        [SerializeField] bool showQuitToMenu;
        [SerializeField] PopupManager popup;

        SpeedControl _speed;

        public bool IsOpen => rootPanel != null && rootPanel.activeSelf;

        public void BindSpeed(SpeedControl speed) => _speed = speed;

        void Awake()
        {
            if (rootPanel == null) Debug.LogError("SettingsController: rootPanel is not assigned.", this);
            if (masterSlider == null) Debug.LogError("SettingsController: masterSlider is not assigned.", this);
            if (bgmSlider == null) Debug.LogError("SettingsController: bgmSlider is not assigned.", this);
            if (sfxSlider == null) Debug.LogError("SettingsController: sfxSlider is not assigned.", this);
            if (closeButton == null) Debug.LogError("SettingsController: closeButton is not assigned.", this);
            if (showQuitToMenu && popup == null)
                Debug.LogError("SettingsController: popup is required when showQuitToMenu is set.", this);

            if (closeButton != null) closeButton.onClick.AddListener(Close);

            BindSlider(masterSlider, "Master Volume", OnMasterVolumeChanged);
            BindSlider(bgmSlider, "BGM Volume", OnBgmVolumeChanged);
            BindSlider(sfxSlider, "SFX Volume", OnSfxVolumeChanged);

            if (quitToMenuButton != null)
            {
                quitToMenuButton.onClick.AddListener(OnQuitToMenuClicked);
                quitToMenuButton.gameObject.SetActive(showQuitToMenu);
            }

            if (rootPanel != null) rootPanel.SetActive(false);
            GameSettings.IsPanelOpen = false;
        }

        void OnDisable()
        {
            if (IsOpen) Close();
            GameSettings.IsPanelOpen = false;
        }

        public void Open()
        {
            if (rootPanel == null || IsOpen) return;

            SyncSlidersFromStore();
            rootPanel.SetActive(true);
            GameSettings.IsPanelOpen = true;
            _speed?.PushPause("settings");
        }

        public void Close()
        {
            if (rootPanel == null) return;
            if (!IsOpen)
            {
                GameSettings.IsPanelOpen = false;
                return;
            }

            rootPanel.SetActive(false);
            GameSettings.IsPanelOpen = false;
            _speed?.PopPause("settings");
        }

        void SyncSlidersFromStore()
        {
            masterSlider?.SetValue01(GameSettings.GetMasterVolume());
            bgmSlider?.SetValue01(GameSettings.GetBgmVolume());
            sfxSlider?.SetValue01(GameSettings.GetSfxVolume());
        }

        static void BindSlider(SliderHandler handler, string label, System.Action<float> onChange)
        {
            if (handler == null) return;
            handler.SetLabel(label);
            handler.BindOnValueChanged(onChange);
        }

        static void OnMasterVolumeChanged(float v) => GameSettings.SetMasterVolume(v);
        static void OnBgmVolumeChanged(float v) => GameSettings.SetBgmVolume(v);
        static void OnSfxVolumeChanged(float v) => GameSettings.SetSfxVolume(v);

        void OnQuitToMenuClicked()
        {
            if (popup == null) return;
            popup.ShowConfirm(
                QuitToMenuPopupId,
                "Quit to Main Menu?",
                "This run will be lost.",
                onConfirm: LoadMainMenu,
                onCancel: null,
                pauseForFairness: false,
                yesText: "Quit",
                noText: "Cancel");
        }

        static void LoadMainMenu() => SceneManager.LoadScene(SceneNames.MainMenu);
    }
}
