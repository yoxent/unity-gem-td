using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    public class DraftPick : MonoBehaviour
    {
        [SerializeField] Button draftButton;
        [SerializeField] TMP_Text draftLabel;
        [SerializeField] TMP_Text draftDescription;
        [SerializeField] CanvasGroup draftStatusGroup;
        [SerializeField] TMP_Text draftStatus;

        Color _normalColor = Color.white;
        bool _haveNormalColor;

        public void UpdateLabel(string label, string description, string status)
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
