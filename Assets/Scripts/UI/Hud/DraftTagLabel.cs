using GemTD.Gameplay.Gems;
using TMPro;
using UnityEngine;

namespace GemTD.UI
{
    public class TagLabel : MonoBehaviour
    {
        [SerializeField] TMP_Text tagText;
        [SerializeField] DraftElementColors elementColors;
        [SerializeField] ElementColorTargets colorTargets;

        /// <summary>Binds one chip flag. The flag picks the row, so the text never round-trips a name.</summary>
        public void Bind(GemTag chip)
        {
            if (!TryBindText(GemTags.ChipLabel(chip)))
                return;
            if (!colorTargets.Apply(elementColors.ForTagLabel(chip)))
                Debug.LogError("TagLabel: assign colorTargets on the prefab.", this);
        }

        /// <summary>
        /// Binds a gem category badge. Uses the same pill/text/border targets as a tag chip,
        /// so a category reads as one more chip in the same row and needs no extra prefab.
        /// </summary>
        public void BindCategory(GemCategory category)
        {
            if (!TryBindText(GemCategories.Label(category)))
                return;
            if (!colorTargets.Apply(elementColors.ForCategory(category)))
                Debug.LogError("TagLabel: assign colorTargets on the prefab.", this);
        }

        bool TryBindText(string label)
        {
            if (tagText == null)
            {
                Debug.LogError("TagLabel: assign tagText on the prefab.", this);
                return false;
            }

            tagText.text = label ?? "";

            if (elementColors == null)
            {
                Debug.LogError("TagLabel: assign elementColors on the prefab.", this);
                return false;
            }

            return true;
        }
    }
}
