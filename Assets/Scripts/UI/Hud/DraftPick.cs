using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    public class DraftPick : MonoBehaviour
    {
        [SerializeField] Button draftButton;
        [SerializeField] TMP_Text draftLabel;

        Color _normalColor = Color.white;
        bool _haveNormalColor;

        public void UpdateLabel(string label)
        {
            draftLabel.text = label;
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
