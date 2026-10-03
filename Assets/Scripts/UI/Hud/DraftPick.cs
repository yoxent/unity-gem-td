using System.Collections.Generic;
using GemTD.Gameplay.Gems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    public class DraftPick : MonoBehaviour
    {
        const int TagsPerRow = 3;

        [SerializeField] Button draftButton;
        [SerializeField] TMP_Text draftLabel;
        [SerializeField] Transform tagsParent;
        [SerializeField] Transform tagSubParentTemplate;
        [SerializeField] DraftTagLabel tagLabelPrefab;
        [SerializeField] List<DraftTagLabel> tagLabels = new List<DraftTagLabel>();
        [SerializeField] TMP_Text draftDescription;
        [SerializeField] CanvasGroup draftStatusGroup;
        [SerializeField] TMP_Text draftStatus;
        [SerializeField] TMP_Text levelLabel;

        readonly List<string> _tagNames = new List<string>(8);
        readonly List<Transform> _tagRows = new List<Transform>(4);

        void Awake()
        {
            if (tagSubParentTemplate != null)
                tagSubParentTemplate.gameObject.SetActive(false);
        }

        Color _normalColor = Color.white;
        bool _haveNormalColor;

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

            var rowsNeeded = _tagNames.Count == 0
                ? 0
                : (_tagNames.Count + TagsPerRow - 1) / TagsPerRow;
            for (var row = 0; row < rowsNeeded; row++)
            {
                var rowTransform = GetOrCreateRow(row);
                if (rowTransform != null)
                    rowTransform.gameObject.SetActive(true);
            }

            for (var row = rowsNeeded; row < _tagRows.Count; row++)
            {
                if (_tagRows[row] != null)
                    _tagRows[row].gameObject.SetActive(false);
            }

            for (var i = 0; i < _tagNames.Count; i++)
            {
                var label = GetOrCreateTag(i);
                if (label == null)
                    continue;
                var rowTransform = _tagRows[i / TagsPerRow];
                if (label.transform.parent != rowTransform)
                    label.transform.SetParent(rowTransform, false);
                label.gameObject.SetActive(true);
                label.Bind(_tagNames[i]);
            }

            for (var i = _tagNames.Count; i < tagLabels.Count; i++)
            {
                if (tagLabels[i] != null)
                    tagLabels[i].gameObject.SetActive(false);
            }
        }

        Transform GetOrCreateRow(int index)
        {
            while (_tagRows.Count <= index)
            {
                var row = Instantiate(tagSubParentTemplate, tagsParent);
                row.gameObject.SetActive(false);
                row.SetAsLastSibling();
                _tagRows.Add(row);
            }

            return _tagRows[index];
        }

        DraftTagLabel GetOrCreateTag(int index)
        {
            while (tagLabels.Count <= index)
            {
                if (tagLabelPrefab == null)
                {
                    Debug.LogError("DraftPick: assign tagLabelPrefab (DraftTagLabel) on the prefab.", this);
                    return null;
                }

                var row = GetOrCreateRow(index / TagsPerRow);
                var label = Instantiate(tagLabelPrefab, row);
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
            if (draftButton == null || draftButton.targetGraphic == null)
                return;
            if (!_haveNormalColor)
            {
                _normalColor = draftButton.targetGraphic.color;
                _haveNormalColor = true;
            }

            draftButton.targetGraphic.color = selected
                ? new Color(1f, 0.82f, 0.35f, 1f)
                : _normalColor;
        }
    }
}
