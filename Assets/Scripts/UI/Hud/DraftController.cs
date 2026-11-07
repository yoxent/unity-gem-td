using System.Collections.Generic;
using GemTD.Core;
using GemTD.Gameplay;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    /// <summary>Lives on DraftPanel prefab. 4 DraftPick children + Skip + Replace popup routing.</summary>
    public sealed class DraftController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text subTitleText;
        [SerializeField] List<DraftPick> picks = new List<DraftPick>();

        [Header("Actions")]
        [SerializeField] Button skipButton;
        [SerializeField] TMP_Text skipText;
        [SerializeField] Button rerollButton;
        [SerializeField] Image rerollButtonBG;
        [SerializeField] TMP_Text rerollText;
        [SerializeField] Button banButton;
        [SerializeField] Image banButtonBG;
        [SerializeField] TMP_Text banText;
        [SerializeField] Button lockInButton;
        [SerializeField] TMP_Text replaceHintText;

        GameCompositionRoot _root;
        PopupManager _popup;
        bool _buttonsBound;
        bool _replacePopupShown;
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
                        picks[i].GetButton().onClick.AddListener(() =>
                        {
                            UiSfx.Click();
                            _root?.RequestDraftSelect(idx);
                        });
                }
            }

            if (skipButton != null) skipButton.onClick.AddListener(() =>
            {
                UiSfx.Click();
                _root?.RequestDraftSkip();
            });
            if (rerollButton != null) rerollButton.onClick.AddListener(() =>
            {
                UiSfx.Click();
                _root?.RequestDraftReroll();
            });
            if (banButton != null) banButton.onClick.AddListener(() =>
            {
                UiSfx.Click();
                _root?.RequestDraftBan();
            });
            if (lockInButton != null) lockInButton.onClick.AddListener(() =>
            {
                GameEvents.RaisePlaySfx(SfxKeys.DraftPick);
                if (_root != null && _root.Draft != null)
                    _root.RequestDraftPick(_root.Draft.SelectedIndex);
            });
            else
                Debug.LogError("DraftController: assign lockInButton on the prefab.", this);
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
                if (lockInButton != null)
                    lockInButton.gameObject.SetActive(false);
                return;
            }

            var draft = _root.Draft;
            if (draft == null) return;
            if (titleText != null) titleText.text = "Draft Phase";
            if (subTitleText != null)
                subTitleText.text = FormatHint(draft);

            for (var i = 0; i < picks.Count; i++)
            {
                if (picks[i] == null) continue;
                if (i < draft.CurrentOffer.Count && draft.CurrentOffer[i].IsFilled)
                {
                    var card = draft.CurrentOffer[i];
                    var roster = draft.Roster;
                    picks[i].UpdateLabel(
                        TowerRoster.FormatOfferLabel(card, roster),
                        card.Description,
                        TowerRoster.FormatOfferStatus(card, roster),
                        TowerRoster.FormatOfferLevel(card, roster));
                    picks[i].SetTags(card.Tags);
                    picks[i].ApplyElements(card.Tags);
                    picks[i].SetSelected(i == draft.SelectedIndex);
                    picks[i].gameObject.SetActive(true);
                    var btn = picks[i].GetButton();
                    if (btn != null)
                        btn.interactable = draft.IsActive && draft.ReplacePhase == DraftReplacePhase.None;
                    else
                        Debug.LogError("DraftController: assign draftButton on the DraftPick prefab.", picks[i]);
                }
                else
                {
                    picks[i].SetSelected(false);
                    picks[i].SetTags(GemTag.None);
                    picks[i].gameObject.SetActive(false);
                }
            }

            var idle = draft.IsActive && draft.ReplacePhase == DraftReplacePhase.None;
            var gold = _root.Economy != null ? _root.Economy.Gold : 0;

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
                var canReroll = idle && gold >= draft.NextRerollCost;
                rerollButton.interactable = canReroll;
                if (rerollButtonBG != null)
                    rerollButtonBG.gameObject.SetActive(canReroll);
                else
                    Debug.LogError("DraftController: assign rerollButtonBG on the prefab.", this);
            }

            if (banText != null)
                banText.text = "Ban " + draft.NextBanCost + "g";
            if (banButton != null)
            {
                banButton.gameObject.SetActive(true);
                var canBan = idle && _root.Economy != null && draft.CanBan(_root.Economy);
                banButton.interactable = canBan;
                if (banButtonBG != null)
                    banButtonBG.gameObject.SetActive(canBan);
                else
                    Debug.LogError("DraftController: assign banButtonBG on the prefab.", this);
            }

            if (lockInButton != null)
            {
                var canLockIn = idle && draft.SelectedIndex >= 0;
                lockInButton.gameObject.SetActive(canLockIn);
                lockInButton.interactable = canLockIn;
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
                    replaceHintText.text = "Replace mode — Click an inventory gem to destroy it, or Esc to cancel.";
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

        static string FormatHint(DraftService draft)
        {
            if (draft.ReplacePhase == DraftReplacePhase.AwaitingInventoryPick)
                return "Click an inventory gem to destroy it, or Esc to cancel.";
            if (draft.ReplacePhase == DraftReplacePhase.AwaitingConfirm)
                return "Bag is full. Replace a gem to take this card.";
            if (!draft.AllowSkip)
                return "Choose a tower, then lock it in.";
            return "Choose a card, then lock it in, or skip for gold.";
        }
    }
}
