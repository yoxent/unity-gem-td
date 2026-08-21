using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    public class DraftPick : MonoBehaviour
    {
        [SerializeField] Button draftButton;
        [SerializeField] TMP_Text draftLabel;

        public void UpdateLabel(string label)
        {
            draftLabel.text = label;
        }

        public Button GetButton()
        {
            return draftButton;
        }
    }
}
