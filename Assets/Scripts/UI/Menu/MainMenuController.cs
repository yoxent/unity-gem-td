using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using GemTD.Core;

namespace GemTD.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] Button playButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button skillLabButton;
        [SerializeField] Button quitButton;
        [SerializeField] SettingsController settings;
        [SerializeField] string clickSfxKey = "Click";

        InputAction _escape;

        void Awake()
        {
            if (playButton == null) Debug.LogError("MainMenuController: playButton is not assigned.", this);
            if (settingsButton == null) Debug.LogError("MainMenuController: settingsButton is not assigned.", this);
            if (skillLabButton == null) Debug.LogError("MainMenuController: skillLabButton is not assigned.", this);
            if (quitButton == null) Debug.LogError("MainMenuController: quitButton is not assigned.", this);
            if (settings == null) Debug.LogError("MainMenuController: settings is not assigned.", this);

            if (playButton != null) playButton.onClick.AddListener(OnPlay);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
            if (skillLabButton != null) skillLabButton.onClick.AddListener(OnSkillLab);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);

            PlayerProfile.Load();
            AudioPlayer.EnsureExists();
            GameSettings.ApplyAudio();
            if (settings != null) settings.Close();
        }

        void OnEnable()
        {
            _escape = new InputAction("Escape", InputActionType.Button, "<Keyboard>/escape");
            _escape.Enable();
        }

        void OnDisable()
        {
            _escape?.Disable();
            _escape?.Dispose();
            _escape = null;
        }

        void Update()
        {
            if (_escape != null && _escape.WasPressedThisFrame() && settings != null && settings.IsOpen)
                settings.Close();
        }

        void OnPlay()
        {
            PlayClickSfx();
            SceneManager.LoadScene(SceneNames.Run);
        }

        void OnSkillLab()
        {
            PlayClickSfx();
            SceneManager.LoadScene(SceneNames.SkillLab);
        }

        void OnSettings()
        {
            PlayClickSfx();
            if (settings != null) settings.Open();
        }

        void OnQuit()
        {
            PlayClickSfx();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void PlayClickSfx()
        {
            GameEvents.RaisePlaySfx(clickSfxKey);
        }
    }
}
