using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GemTD.Gameplay.Gems;

namespace GemTD.UI
{
    /// <summary>
    /// Fixed-position tooltip for gems in the inventory. It is kept separate
    /// from TowerGemTooltip so its content can evolve independently later.
    /// </summary>
    public sealed class InventoryGemTooltip : MonoBehaviour
    {
        [SerializeField] GameObject infoPanel;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text gemNameText;
        [SerializeField] TMP_Text rarityText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] TMP_Text tagsText;

        void Awake()
        {
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

            SetText(gemNameText, gem.DisplayName);
            SetText(rarityText, FormatRarity(gem.Rarity));
            SetText(descriptionText, definition.Description);
            SetText(tagsText, GemTags.Format(definition.Tags));

            if (infoPanel != null)
                infoPanel.SetActive(true);
        }

        public void Hide()
        {
            if (infoPanel != null)
                infoPanel.SetActive(false);
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

        static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }
    }
}
