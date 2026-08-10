using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GemTD.Core;

namespace GemTD.UI
{
    /// <summary>Generic popup/dialog owner. One pre-placed root panel; all confirms route through it.
    /// Don't-show-again is PlayerPrefs-backed; pause-for-fairness is opt-in via SpeedControl.</summary>
    public sealed class PopupManager : MonoBehaviour
    {
        public const string DefaultYesText = "Confirm";
        public const string DefaultNoText = "Cancel";

        [SerializeField] GameObject rootPanel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Toggle dontShowAgain;
        [SerializeField] TMP_Text dontShowAgainLabel;
        [SerializeField] Button yesButton;
        [SerializeField] TMP_Text yesLabel;
        [SerializeField] Button noButton;
        [SerializeField] TMP_Text noLabel;

        SpeedControl _speed;
        Action _onYes;
        Action _onNo;
        string _currentId;

        public bool IsOpen => rootPanel != null && rootPanel.activeSelf;

        public void Init(SpeedControl speed) => _speed = speed;

        void Awake()
        {
            if (yesButton != null) yesButton.onClick.AddListener(OnYesClicked);
            if (noButton != null) noButton.onClick.AddListener(OnNoClicked);
            if (rootPanel != null) rootPanel.SetActive(false);
        }

        public void ShowConfirm(string id, string title, string body, Action onConfirm,
            Action onCancel = null,
            bool pauseForFairness = false, string yesText = DefaultYesText, string noText = DefaultNoText,
            string dontShowAgainText = "Don't show again")
        {
            OpenInternal(id, title, body, onConfirm, onCancel, pauseForFairness, yesText, noText,
                showCheckbox: true, dontShowAgainText, singleButton: false);
        }

        public void ShowConfirmOnceSuppressed(string id, string title, string body, Action onConfirm,
            Action onCancel = null,
            bool pauseForFairness = false, string yesText = DefaultYesText, string noText = DefaultNoText,
            string dontShowAgainText = "Don't show again")
        {
            if (!ShouldShow(id))
            {
                onConfirm?.Invoke();
                return;
            }
            ShowConfirm(id, title, body, onConfirm, onCancel, pauseForFairness, yesText, noText, dontShowAgainText);
        }

        public void ShowInfo(string title, string body, string okText = "OK", Action onOk = null)
        {
            OpenInternal(id: "info", title, body, onOk, onCancel: null, pauseForFairness: false,
                yesText: okText, noText: okText, showCheckbox: false, dontShowAgainText: "", singleButton: true);
        }

        public void Hide()
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(false);
                rootPanel.transform.SetAsLastSibling();
            }
            if (_speed != null && !string.IsNullOrEmpty(_currentId))
                _speed.PopPause("popup-" + _currentId);
            Reset();
            _onYes = null;
            _onNo = null;
            _currentId = null;
        }

        // Esc dismiss = neutral close (no callback). No button calls this (fires onCancel).
        void Reset()
        {
            if (titleText != null) titleText.text = "";
            if (bodyText != null) bodyText.text = "";
            if (dontShowAgain != null) dontShowAgain.isOn = false;
            if (dontShowAgainLabel != null) dontShowAgainLabel.text = "";
            if (yesLabel != null) yesLabel.text = DefaultYesText;
            if (noLabel != null) noLabel.text = DefaultNoText;
            if (noButton != null) noButton.gameObject.SetActive(true);
        }

        void OpenInternal(string id, string title, string body, Action onYes, Action onCancel, bool pauseForFairness,
            string yesText, string noText, bool showCheckbox, string dontShowAgainText, bool singleButton)
        {
            _currentId = id;
            _onYes = onYes;
            _onNo = onCancel;
            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;
            if (yesLabel != null) yesLabel.text = yesText;
            if (noLabel != null) noLabel.text = noText;
            if (dontShowAgain != null) dontShowAgain.gameObject.SetActive(showCheckbox);
            if (dontShowAgainLabel != null) dontShowAgainLabel.text = showCheckbox ? dontShowAgainText : "";
            if (dontShowAgain != null) dontShowAgain.isOn = false;
            if (noButton != null) noButton.gameObject.SetActive(!singleButton);
            if (rootPanel != null)
            {
                rootPanel.transform.SetAsLastSibling();
                rootPanel.SetActive(true);
            }
            if (pauseForFairness && _speed != null)
                _speed.PushPause("popup-" + id);
        }

        // Called by yesButton.onClick (wired in Awake or via wire menu).
        public void OnYesClicked()
        {
            if (dontShowAgain != null && dontShowAgain.isOn && !string.IsNullOrEmpty(_currentId))
                Suppress(_currentId);
            var cb = _onYes;
            Reset();
            _onYes = null;
            _onNo = null;
            if (rootPanel != null) rootPanel.SetActive(false);
            if (_speed != null && !string.IsNullOrEmpty(_currentId))
                _speed.PopPause("popup-" + _currentId);
            _currentId = null;
            cb?.Invoke();
        }

        // Called by noButton.onClick. Fires onCancel (the "No" decision) then hides.
        public void OnNoClicked()
        {
            var cb = _onNo;
            Hide();
            cb?.Invoke();
        }

        // --- Pure helpers (testable without a live panel) ---
        public static string DontShowKey(string id) => "GemTD.Popup." + id + ".DontShow";
        public static bool ShouldShow(string id) => PlayerPrefs.GetInt(DontShowKey(id), 0) == 0;
        public static void Suppress(string id) => PlayerPrefs.SetInt(DontShowKey(id), 1);
    }
}