using TMPro;
using UnityEngine;

namespace GemTD.UI
{
    public class TagLabel : MonoBehaviour
    {
        [SerializeField] TMP_Text tagText;

        public void Bind(string tagName)
        {
            if (tagText != null)
                tagText.text = tagName ?? "";
            else
                Debug.LogError("TagLabel: assign tagText on the prefab.", this);
        }
    }
}
