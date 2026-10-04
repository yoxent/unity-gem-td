using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GemTD.Core;
using GemTD.Gameplay;

namespace GemTD.UI
{
    /// <summary>Lives on SpeedPanel prefab. Speed buttons + pause chip. Listens to GameEvents.</summary>
    public sealed class SpeedPanelController : MonoBehaviour
    {
        [SerializeField] Button speed1Button;
        [SerializeField] Button speed2Button;
        [SerializeField] Button speed4Button;
        [SerializeField] TMP_Text speed1Label;
        [SerializeField] TMP_Text speed2Label;
        [SerializeField] TMP_Text speed4Label;
        [SerializeField] GameObject pauseChip;

        [SerializeField] Sprite activeSprite;
        [SerializeField] Sprite inactiveSprite;
        [SerializeField] Color activeTextColor = Color.white;
        [SerializeField] Color inactiveTextColor = Color.white;

        GameCompositionRoot _root;

        void OnEnable()
        {
            GameEvents.SpeedChanged += OnSpeedChanged;
            GameEvents.PauseChanged += OnPauseChanged;
        }

        void OnDisable()
        {
            GameEvents.SpeedChanged -= OnSpeedChanged;
            GameEvents.PauseChanged -= OnPauseChanged;
        }

        public void Bind(GameCompositionRoot root)
        {
            _root = root;
            if (_root == null) return;

            if (speed1Button != null) speed1Button.onClick.AddListener(() =>
            {
                UiSfx.Click();
                _root.Speed?.SetSpeed(1f);
            });
            if (speed2Button != null) speed2Button.onClick.AddListener(() =>
            {
                UiSfx.Click();
                _root.Speed?.SetSpeed(2f);
            });
            if (speed4Button != null) speed4Button.onClick.AddListener(() =>
            {
                UiSfx.Click();
                _root.Speed?.SetSpeed(4f);
            });
            if (pauseChip != null) pauseChip.SetActive(false);

            OnSpeedChanged(_root.Speed != null ? _root.Speed.CurrentSpeed : 1f);
        }

        void OnSpeedChanged(float scale)
        {
            ApplySpeedButton(speed1Button, speed1Label, Mathf.Approximately(scale, 1f));
            ApplySpeedButton(speed2Button, speed2Label, Mathf.Approximately(scale, 2f));
            ApplySpeedButton(speed4Button, speed4Label, Mathf.Approximately(scale, 4f));
        }

        void ApplySpeedButton(Button button, TMP_Text label, bool active)
        {
            if (button != null && button.image != null)
                button.image.sprite = active ? activeSprite : inactiveSprite;
            if (label != null)
                label.color = active ? activeTextColor : inactiveTextColor;
        }

        void OnPauseChanged(bool paused)
        {
            if (pauseChip != null) pauseChip.SetActive(paused);
        }
    }
}
