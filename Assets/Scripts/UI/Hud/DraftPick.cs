using System.Collections.Generic;
using GemTD.Gameplay.Gems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    public class DraftPick : MonoBehaviour
    {
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

        readonly List<string> _tagNames = new List<string>(8);

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
