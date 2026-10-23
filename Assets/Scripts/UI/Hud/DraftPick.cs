using System.Collections.Generic;
using GemTD.Gameplay.Gems;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    public class DraftPick : MonoBehaviour
    {
        [SerializeField] DraftElementColors elementColors;
        [SerializeField] ElementColorTargets colorTargets;
        [SerializeField] Button draftButton;
        [SerializeField] TMP_Text draftLabel;
        [SerializeField] Transform tagsParent;
        [SerializeField] Transform tagSubParentTemplate;
        [SerializeField] TagLabel tagLabelPrefab;
        [SerializeField] List<TagLabel> tagLabels = new List<TagLabel>();
        [SerializeField] TMP_Text draftDescription;
        [SerializeField] CanvasGroup draftStatusGroup;
        [SerializeField] TMP_Text draftStatus;
        [SerializeField] TMP_Text levelLabel;
        [SerializeField] Transform cardPanel;
        [SerializeField] float selectMoveSeconds = 0.2f;
        [SerializeField] float selectedCardY = 14f;
        [SerializeField] GameObject selectedGroupPanel;
        [SerializeField] Color selectedHighlightColor;

        readonly List<string> _tagNames = new List<string>(8);
        MotionHandle _cardMove;
        DraftElementColors.Palette _palette;
        bool _havePalette;
        bool _hasSelection;
        bool _selected;

        void Awake()
        {
            if (tagSubParentTemplate != null)
                tagSubParentTemplate.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            if (_cardMove.IsActive())
                _cardMove.Complete();
        }

        public void UpdateLabel(string label, string description, string status, string level)
        {
            if (draftLabel != null)
                draftLabel.text = label ?? "";
            else
                Debug.LogError("DraftPick: assign draftLabel on the prefab.", this);

            if (draftDescription != null)
                draftDescription.text = description ?? "";
            else
                Debug.LogError("DraftPick: assign draftDescription on the prefab.", this);

            if (draftStatus != null)
            {
                var text = status ?? "";
                draftStatus.text = text;
                draftStatusGroup.alpha = text.Length > 0 ? 1f : 0f;
                draftStatus.gameObject.SetActive(text.Length > 0);
            }
            else
                Debug.LogError("DraftPick: assign draftStatus on the prefab.", this);

            if (levelLabel != null)
            {
                var text = level ?? "";
                levelLabel.text = text;
                var badge = levelLabel.transform.parent;
                if (badge != null)
                    badge.gameObject.SetActive(text.Length > 0);
            }
            else
                Debug.LogError("DraftPick: assign levelLabel on the prefab.", this);
        }

        public void ApplyElements(GemTag tags)
        {
            if (elementColors == null)
            {
                Debug.LogError("DraftPick: assign elementColors on the prefab.", this);
                return;
            }

            _palette = elementColors.Resolve(tags);
            _havePalette = true;
            if (!colorTargets.Apply(_palette))
                Debug.LogError("DraftPick: assign colorTargets on the prefab.", this);
            if (_selected)
                PaintSelectionHighlight(true);
        }

        public void SetTags(GemTag tags)
        {
            if (tagsParent == null)
            {
                Debug.LogError("DraftPick: assign tagsParent on the prefab.", this);
                return;
            }

            if (tagSubParentTemplate == null)
            {
                Debug.LogError("DraftPick: assign tagSubParentTemplate on the prefab.", this);
                return;
            }

            tagSubParentTemplate.gameObject.SetActive(false);
            GemTags.CollectNames(tags, _tagNames);

            for (var i = 0; i < _tagNames.Count; i++)
            {
                var label = GetOrCreateTag(i);
                if (label == null)
                    continue;
                if (label.transform.parent != tagsParent)
                    label.transform.SetParent(tagsParent, false);
                label.transform.SetSiblingIndex(i);
                label.gameObject.SetActive(true);
                label.Bind(_tagNames[i]);
            }

            for (var i = _tagNames.Count; i < tagLabels.Count; i++)
            {
                if (tagLabels[i] != null)
                    tagLabels[i].gameObject.SetActive(false);
            }

            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)tagsParent);
        }

        TagLabel GetOrCreateTag(int index)
        {
            while (tagLabels.Count <= index)
            {
                if (tagLabelPrefab == null)
                {
                    Debug.LogError("DraftPick: assign tagLabelPrefab (DraftTagLabel) on the prefab.", this);
                    return null;
                }

                var label = Instantiate(tagLabelPrefab, tagsParent);
                label.gameObject.SetActive(false);
                tagLabels.Add(label);
            }

            return tagLabels[index];
        }

        public Button GetButton()
        {
            return draftButton;
        }

        public void SetSelected(bool selected)
        {
            if (selectedGroupPanel != null)
                selectedGroupPanel.SetActive(selected);
            else
                Debug.LogError("DraftPick: assign selectedGroupPanel on the prefab.", this);

            var changed = !_hasSelection || selected != _selected;
            _selected = selected;
            _hasSelection = true;
            PaintSelectionHighlight(selected);
            if (changed)
                MoveCard(selected ? selectedCardY : -selectedCardY);
        }

        void PaintSelectionHighlight(bool selected)
        {
            var graphic = colorTargets.selectedHighlight;
            if (graphic == null)
            {
                Debug.LogError("DraftPick: assign colorTargets.selectedHighlight on the prefab.", this);
                return;
            }

            if (selected)
                graphic.color = selectedHighlightColor;
            else if (_havePalette)
                graphic.color = _palette.normal;
        }

        void MoveCard(float targetY)
        {
            if (cardPanel is not RectTransform rect)
            {
                Debug.LogError("DraftPick: assign cardPanel on the prefab.", this);
                return;
            }

            if (_cardMove.IsActive())
                _cardMove.Cancel();

            var pos = rect.anchoredPosition;
            if (selectMoveSeconds <= 0f || Mathf.Approximately(pos.y, targetY))
            {
                pos.y = targetY;
                rect.anchoredPosition = pos;
                return;
            }

            _cardMove = LMotion.Create(pos.y, targetY, selectMoveSeconds)
                .WithEase(Ease.OutQuad)
                .Bind(rect, static (y, rt) =>
                {
                    if (rt == null)
                        return;
                    var p = rt.anchoredPosition;
                    p.y = y;
                    rt.anchoredPosition = p;
                });
        }
    }
}
