using TMPro;
using UnityEngine;

namespace GemTD.UI
{
    public class DraftTagLabel : MonoBehaviour
    {
        [SerializeField] TMP_Text draftTagText;

        public void Bind(string tagName)
        {
            if (draftTagText != null)
                draftTagText.text = tagName ?? "";
            else
                Debug.LogError("DraftTagLabel: assign draftTagText on the prefab.", this);
        }
    }
}
