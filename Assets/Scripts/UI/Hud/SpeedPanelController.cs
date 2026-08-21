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

        GameCompositionRoot _root;
        static readonly Color Dim = new Color(0.6f, 0.6f, 0.62f, 1f);
        static readonly Color Lit = new Color(0.88f, 0.71f, 0.29f, 1f);

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

            if (speed1Button != null) speed1Button.onClick.AddListener(() => _root.Speed?.SetSpeed(1f));
            if (speed2Button != null) speed2Button.onClick.AddListener(() => _root.Speed?.SetSpeed(2f));
            if (speed4Button != null) speed4Button.onClick.AddListener(() => _root.Speed?.SetSpeed(4f));
            if (pauseChip != null) pauseChip.SetActive(false);

            OnSpeedChanged(_root.Speed != null ? _root.Speed.CurrentSpeed : 1f);
        }

        void OnSpeedChanged(float scale)
        {
            if (speed1Label != null) speed1Label.color = Mathf.Approximately(scale, 1f) ? Lit : Dim;
            if (speed2Label != null) speed2Label.color = Mathf.Approximately(scale, 2f) ? Lit : Dim;
            if (speed4Label != null) speed4Label.color = Mathf.Approximately(scale, 4f) ? Lit : Dim;
        }

        void OnPauseChanged(bool paused)
        {
            if (pauseChip != null) pauseChip.SetActive(paused);
        }
    }
}
