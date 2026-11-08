using System.Collections.Generic;
using GemTD.Gameplay.Gems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    /// <summary>Fixed-position tooltip for gems in the inventory.</summary>
    public sealed class InventoryGemTooltip : MonoBehaviour
    {
        [SerializeField] GameObject infoPanel;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text gemNameText;
        [SerializeField] TMP_Text rarityText;
        [SerializeField] Transform tagsParent;
        [SerializeField] Transform tagSubParentTemplate;
        [SerializeField] TagLabel tagLabelPrefab;
        [SerializeField] List<TagLabel> tagLabels = new List<TagLabel>();
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] TMP_Text effectsText;
        [SerializeField] TMP_Text combineText;

        readonly List<GemTag> _tagChips = new List<GemTag>(8);

        void Awake()
        {
            if (tagSubParentTemplate != null)
                tagSubParentTemplate.gameObject.SetActive(false);
            Hide();
        }

        void OnDisable()
        {
            Hide();
        }

        public void Show(GemInstance gem)
        {
            if (gem.IsEmpty)
            {
                Hide();
                return;
            }

            var definition = gem.Def;
            if (icon != null)
            {
                icon.sprite = definition.Icon;
                icon.gameObject.SetActive(definition.Icon != null);
                icon.preserveAspect = true;
            }

            SetText(gemNameText, definition.DisplayName);
            SetRarity(gem.Rarity);
            SetTags(GemTags.EffectiveGemTags(definition), definition.Category);
            SetText(descriptionText, definition.Description);
            SetText(effectsText, TowerGemTooltip.FormatEffects(gem));
            if (effectsText != null)
                effectsText.gameObject.SetActive(!string.IsNullOrEmpty(effectsText.text));
            SetText(combineText, FormatCombine(gem.Rarity));

            if (infoPanel != null)
                infoPanel.SetActive(true);
        }

        public void Hide()
        {
            if (infoPanel != null)
                infoPanel.SetActive(false);
        }

        void SetRarity(GemRarity rarity)
        {
            if (rarityText == null)
                return;

            var normalized = GemRarityUtility.Normalize(rarity);
            rarityText.text = FormatRarity(normalized);
            rarityText.color = RarityColor(normalized);
        }

        void SetTags(GemTag tags, GemCategory category)
        {
            if (tagsParent == null || tagSubParentTemplate == null)
                return;

            tagSubParentTemplate.gameObject.SetActive(false);
            GemTags.CollectChipFlags(tags, _tagChips);

            var slot = 0;
            if (GemCategories.Label(category).Length > 0)
            {
                var categoryLabel = GetOrCreateTag(slot);
                if (categoryLabel != null)
                {
                    if (categoryLabel.transform.parent != tagsParent)
                        categoryLabel.transform.SetParent(tagsParent, false);
                    categoryLabel.transform.SetSiblingIndex(slot);
                    categoryLabel.gameObject.SetActive(true);
                    categoryLabel.BindCategory(category);
                    slot++;
                }
            }

            for (var i = 0; i < _tagChips.Count; i++)
            {
                var label = GetOrCreateTag(slot);
                if (label == null)
                    continue;

                if (label.transform.parent != tagsParent)
                    label.transform.SetParent(tagsParent, false);
                label.transform.SetSiblingIndex(slot);
                label.gameObject.SetActive(true);
                label.Bind(_tagChips[i]);
                slot++;
            }

            for (var i = slot; i < tagLabels.Count; i++)
            {
                if (tagLabels[i] != null)
                    tagLabels[i].gameObject.SetActive(false);
            }

            tagsParent.gameObject.SetActive(slot > 0);
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)tagsParent);
        }

        TagLabel GetOrCreateTag(int index)
        {
            while (tagLabels.Count <= index)
            {
                if (tagLabelPrefab == null)
                {
                    Debug.LogError("InventoryGemTooltip: assign tagLabelPrefab on the prefab.", this);
                    return null;
                }

                var label = Instantiate(tagLabelPrefab, tagsParent);
                label.gameObject.SetActive(false);
                tagLabels.Add(label);
            }

            return tagLabels[index];
        }

        static string FormatCombine(GemRarity rarity)
        {
            var normalized = GemRarityUtility.Normalize(rarity);
            if (!GemRarityUtility.TryNext(normalized, out var next))
                return "Maximum rarity";

            return $"3 {FormatRarity(normalized)} copies → 1 {FormatRarity(next)} copy";
        }

        static string FormatRarity(GemRarity rarity)
        {
            switch (GemRarityUtility.Normalize(rarity))
            {
                case GemRarity.Lesser:
                    return "Lesser";
                case GemRarity.Greater:
                    return "Greater";
                default:
                    return "Normal";
            }
        }

        static Color RarityColor(GemRarity rarity)
        {
            switch (GemRarityUtility.Normalize(rarity))
            {
                case GemRarity.Lesser:
                    return new Color32(154, 165, 177, 255);
                case GemRarity.Greater:
                    return new Color32(199, 125, 255, 255);
                default:
                    return new Color32(91, 141, 239, 255);
            }
        }

        static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }
    }
}
