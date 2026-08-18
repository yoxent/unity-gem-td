using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GemTD.Core;
using GemTD.Gameplay;
using GemTD.Gameplay.Run;
using System.Collections.Generic;

namespace GemTD.UI
{
    /// <summary>Lives on DraftPanel prefab. 3 DraftPick children + Skip + Replace popup routing.</summary>
    public sealed class DraftController : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] List<DraftPick> picks = new List<DraftPick>();
        [SerializeField] Button skipButton;
        [SerializeField] TMP_Text replaceHintText;

        GameCompositionRoot _root;
        PopupManager _popup;
        bool _replacePopupShown;
        bool _buttonsBound;
        DraftReplacePhase _lastReplacePhase = DraftReplacePhase.None;

        void OnEnable()
        {
            GameEvents.RunStateChanged += Refresh;
            GameEvents.DraftOfferChanged += Refresh;
        }

        void OnDisable()
        {
            GameEvents.RunStateChanged -= Refresh;
            GameEvents.DraftOfferChanged -= Refresh;
        }

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
                        picks[i].GetButton().onClick.AddListener(() => _root?.RequestDraftPick(idx));
                }
            }

            if (skipButton != null) skipButton.onClick.AddListener(() => _root?.RequestDraftSkip());
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
            if (titleText != null) titleText.text = "Draft — pick a gem";

            for (var i = 0; i < picks.Count; i++)
            {
                if (picks[i] == null) continue;
                if (i < draft.CurrentOffer.Count && draft.CurrentOffer[i] != null)
                {
                    var gem = draft.CurrentOffer[i];
                    picks[i].UpdateLabel(gem.DisplayName);
                    var btn = picks[i].GetButton();
                    if (btn != null)
                    {
                        btn.interactable = draft.IsActive && draft.ReplacePhase == DraftReplacePhase.None;
                        btn.gameObject.SetActive(true);
                    }
                }
                else
                {
                    picks[i].GetButton()?.gameObject.SetActive(false);
                }
            }

            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(draft.AllowSkip);
                skipButton.interactable = draft.AllowSkip && draft.ReplacePhase == DraftReplacePhase.None;
            }

            var phase = draft.ReplacePhase;
            if (phase == DraftReplacePhase.AwaitingConfirm
                && _lastReplacePhase != DraftReplacePhase.AwaitingConfirm
                && !_replacePopupShown
                && _popup != null)
            {
                _replacePopupShown = true;
                var name = draft.PendingReplaceGem != null ? draft.PendingReplaceGem.DisplayName : "gem";
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
