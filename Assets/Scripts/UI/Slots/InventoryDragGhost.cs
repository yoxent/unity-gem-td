using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    /// <summary>
    /// Authored view for the gem drag ghost. A slot supplies only the
    /// current icon, label, font, and size; the hierarchy and visual defaults
    /// live on the prefab.
    /// </summary>
    public sealed class InventoryDragGhost : MonoBehaviour
    {
        [SerializeField] RectTransform ghostRect;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text label;

        public RectTransform RectTransform => ghostRect;

        public void Bind(
            Vector2 size,
            Sprite iconSprite,
            string text,
            TMP_FontAsset font,
            float fontSize)
        {
            if (ghostRect != null)
                ghostRect.sizeDelta = size;

            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.gameObject.SetActive(iconSprite != null);
                icon.preserveAspect = true;
            }

            if (label == null)
                return;

            label.text = text ?? string.Empty;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            if (font != null)
                label.font = font;
        }
    }
}
