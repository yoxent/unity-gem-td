using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GemTD.Core;
using GemTD.Gameplay;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;
using System.Collections.Generic;

namespace GemTD.UI
{
    /// <summary>Lives on DraftPanel prefab. 4 DraftPick children + Skip + Replace popup routing.</summary>
    public sealed class DraftController : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] List<DraftPick> picks = new List<DraftPick>();
        [SerializeField] LayoutElement selectCardLayoutElement;
        [SerializeField] List<Button> selectButtons = new List<Button>();
        [SerializeField] Button skipButton;
        [SerializeField] TMP_Text skipText;
        [SerializeField] Button rerollButton;
        [SerializeField] TMP_Text rerollText;
        [SerializeField] Button banButton;
        [SerializeField] TMP_Text banText;
        [SerializeField] TMP_Text replaceHintText;

        GameCompositionRoot _root;
        PopupManager _popup;
        readonly List<CanvasGroup> _selectGroups = new List<CanvasGroup>(4);
        bool _replacePopupShown;
        bool _buttonsBound;
        DraftReplacePhase _lastReplacePhase = DraftReplacePhase.None;

        void OnEnable()
        {
            GameEvents.RunStateChanged += Refresh;
            GameEvents.DraftOfferChanged += Refresh;
            GameEvents.GoldChanged += OnGoldChanged;
        }

        void OnDisable()
        {
            GameEvents.RunStateChanged -= Refresh;
            GameEvents.DraftOfferChanged -= Refresh;
            GameEvents.GoldChanged -= OnGoldChanged;
        }

        void OnGoldChanged(int _) => Refresh();

        public void Bind(GameCompositionRoot root, PopupManager popup)
        {
            _root = root;
            _popup = popup;
            if (panel == null)
                Debug.LogError("DraftController: assign Panel on the prefab.", this);
            if (picks == null || picks.Count == 0)
                Debug.LogError("DraftController: assign DraftPick refs on the prefab.", this);
            else
            {
                for (var i = 0; i < picks.Count; i++)
                {
                    var idx = i;
                    if (picks[i] != null)
                        picks[i].GetButton().onClick.AddListener(() => _root?.RequestDraftSelect(idx));
                }
            }

            _selectGroups.Clear();
            if (selectButtons != null)
            {
                for (var i = 0; i < selectButtons.Count; i++)
                {
                    var idx = i;
                    var btn = selectButtons[i];
                    if (btn == null)
                    {
                        _selectGroups.Add(null);
                        continue;
                    }

                    var group = btn.GetComponent<CanvasGroup>();
                    if (group == null)
                        Debug.LogError("DraftController: SelectButton is missing a CanvasGroup.", btn);
                    _selectGroups.Add(group);
                    btn.onClick.AddListener(() => _root?.RequestDraftPick(idx));
                }
            }

            if (skipButton != null) skipButton.onClick.AddListener(() => _root?.RequestDraftSkip());
            if (rerollButton != null) rerollButton.onClick.AddListener(() => _root?.RequestDraftReroll());
            if (banButton != null) banButton.onClick.AddListener(() => _root?.RequestDraftBan());
            if (replaceHintText != null) replaceHintText.gameObject.SetActive(false);

            _buttonsBound = picks != null && picks.Count > 0;
            Refresh();
        }

        void Refresh()
        {
            if (!_buttonsBound || _root == null) return;

            var inDraft = _root.States != null && _root.States.Current == RunStateId.Draft;
            panel.SetActive(inDraft);

            if (!inDraft)
            {
                _replacePopupShown = false;
                _lastReplacePhase = DraftReplacePhase.None;
                return;
            }

            var draft = _root.Draft;
            if (draft == null) return;
            if (titleText != null) titleText.text = "Draft";

            for (var i = 0; i < picks.Count; i++)
            {
                if (picks[i] == null) continue;
                if (i < draft.CurrentOffer.Count && draft.CurrentOffer[i].IsFilled)
                {
                    var card = draft.CurrentOffer[i];
                    picks[i].UpdateLabel(TowerRoster.FormatOfferLabel(card, _root.Draft != null ? _root.Draft.Roster : null));
                    picks[i].SetSelected(i == draft.SelectedIndex);
                    var btn = picks[i].GetButton();
                    if (btn != null)
                    {
                        btn.interactable = draft.IsActive && draft.ReplacePhase == DraftReplacePhase.None;
                        btn.gameObject.SetActive(true);
                    }
                }
                else
                {
                    picks[i].SetSelected(false);
                    picks[i].GetButton()?.gameObject.SetActive(false);
                }
            }

            var idle = draft.IsActive && draft.ReplacePhase == DraftReplacePhase.None;
            var selected = draft.SelectedIndex;
            var hasSelection = selected >= 0;
            var gold = _root.Economy != null ? _root.Economy.Gold : 0;

            if (selectCardLayoutElement != null)
                selectCardLayoutElement.ignoreLayout = !hasSelection;

            var selectCount = selectButtons != null ? selectButtons.Count : 0;
            for (var i = 0; i < selectCount; i++)
            {
                var show = idle && i == selected;
                if (i < _selectGroups.Count && _selectGroups[i] != null)
                {
                    _selectGroups[i].alpha = show ? 1f : 0f;
                    _selectGroups[i].interactable = show;
                    _selectGroups[i].blocksRaycasts = show;
                }

                if (selectButtons[i] != null)
                    selectButtons[i].interactable = show;
            }

            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(draft.AllowSkip);
                skipButton.interactable = draft.AllowSkip && idle;
            }

            if (skipText != null && draft.AllowSkip)
            {
                var skipGold = _root.Economy != null ? _root.Economy.LastEndWaveGold : 0;
                skipText.text = "Skip +" + skipGold + "g";
            }

            if (rerollText != null)
                rerollText.text = "Reroll " + draft.NextRerollCost + "g";
            if (rerollButton != null)
            {
                rerollButton.gameObject.SetActive(true);
                rerollButton.interactable = idle && gold >= draft.NextRerollCost;
            }

            if (banText != null)
                banText.text = "Ban " + draft.NextBanCost + "g";
            if (banButton != null)
            {
                banButton.gameObject.SetActive(true);
                banButton.interactable = idle && _root.Economy != null && draft.CanBan(_root.Economy);
            }

            var phase = draft.ReplacePhase;
            if (phase == DraftReplacePhase.AwaitingConfirm
                && _lastReplacePhase != DraftReplacePhase.AwaitingConfirm
                && !_replacePopupShown
                && _popup != null)
            {
                _replacePopupShown = true;
                var name = !draft.PendingReplaceGem.IsEmpty ? draft.PendingReplaceGem.DisplayName : "gem";
                _popup.ShowConfirm("DraftReplace", "Bag full — replace a gem?",
                    $"Take {name}? You'll destroy an inventory gem. Pick an inventory slot, or Cancel.",
                    onConfirm: () => _root.RequestDraftReplaceYes(),
                    onCancel: () => _root.RequestDraftReplaceCancel(),
                    pauseForFairness: true, yesText: "Pick slot", noText: "Cancel");
            }

            if (phase == DraftReplacePhase.AwaitingInventoryPick)
            {
                if (replaceHintText != null)
                {
                    replaceHintText.gameObject.SetActive(true);
                    replaceHintText.text = "Replace mode — click an inventory gem to destroy it, or Esc to cancel.";
                }
            }
            else
            {
                if (phase == DraftReplacePhase.None)
                    _replacePopupShown = false;
                if (replaceHintText != null)
                {
                    replaceHintText.text = "";
                    replaceHintText.gameObject.SetActive(false);
                }
            }

            _lastReplacePhase = phase;
        }
    }
}
