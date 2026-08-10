using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
        [SerializeField] Transform draftPicksParent;
        [SerializeField] List<DraftPick> picks = new List<DraftPick>();
        [SerializeField] Button skipButton;
        [SerializeField] TMP_Text replaceHintText;

        GameCompositionRoot _root;
        PopupManager _popup;

        bool _replacePopupShown;
        bool _buttonsBound = false;

        void Start()
        {
            picks.Clear();

            if (panel == null) panel = gameObject;

            for (var i = 0; i < draftPicksParent.childCount; i++)
            {
                DraftPick draftPick = null;
                draftPicksParent.GetChild(i).TryGetComponent(out draftPick);

                if (draftPick != null)
                {
                    var idx = i;
                    picks.Add(draftPick);
                    picks[i].GetButton().onClick.AddListener(() => _root?.RequestDraftPick(idx));
                }
            }

            if (skipButton != null) skipButton.onClick.AddListener(() => _root?.RequestDraftSkip());
            if (replaceHintText != null) replaceHintText.gameObject.SetActive(false);

            _buttonsBound = true;
        }

        void Update()
        {
            if (!_buttonsBound) return;

            if (_root == null) _root = GameCompositionRoot.Instance;
            if (_root == null) return;
            if (_popup == null) _popup = FindFirstObjectByType<PopupManager>(FindObjectsInactive.Include);

            var inDraft = _root.States != null && _root.States.Current == RunStateId.Draft;
            panel.SetActive(inDraft);

            if (!inDraft) { _replacePopupShown = false; return; }

            Refresh();
        }

        void Refresh()
        {
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

            // Replace hint + popup routing.
            var phase = draft.ReplacePhase;
            if (phase == DraftReplacePhase.AwaitingConfirm)
            {
                if (replaceHintText != null) { replaceHintText.text = ""; replaceHintText.gameObject.SetActive(false); }
                if (!_replacePopupShown && _popup != null)
                {
                    _replacePopupShown = true;
                    var name = draft.PendingReplaceGem != null ? draft.PendingReplaceGem.DisplayName : "gem";
                    _popup.ShowConfirm("DraftReplace", "Bag full — replace a gem?",
                        $"Take {name}? You'll destroy an inventory gem. Pick an inventory slot, or Cancel.",
                        onConfirm: () => _root.RequestDraftReplaceYes(),
                        onCancel: () => _root.RequestDraftReplaceCancel(),
                        pauseForFairness: true, yesText: "Pick slot", noText: "Cancel");
                }
            }
            else if (phase == DraftReplacePhase.AwaitingInventoryPick)
            {
                if (replaceHintText != null)
                {
                    replaceHintText.gameObject.SetActive(true);
                    replaceHintText.text = "Replace mode — click an inventory gem to destroy it, or Esc to cancel.";
                }
            }
            else
            {
                _replacePopupShown = false;
                if (replaceHintText != null) { replaceHintText.text = ""; replaceHintText.gameObject.SetActive(false); }
            }
        }
    }
}