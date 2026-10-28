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

        public void Bind(string tagName)
        {
            if (tagText != null)
                tagText.text = tagName ?? "";
            else
                Debug.LogError("TagLabel: assign tagText on the prefab.", this);

            if (elementColors == null)
            {
                Debug.LogError("TagLabel: assign elementColors on the prefab.", this);
                return;
            }

            if (!colorTargets.Apply(elementColors.Resolve(GemTags.FromPoe(tagName))))
                Debug.LogError("TagLabel: assign colorTargets on the prefab.", this);
        }
    }
}
