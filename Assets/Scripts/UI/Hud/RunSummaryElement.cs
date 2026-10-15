using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    public sealed class RunSummaryElement : MonoBehaviour
    {
        [SerializeField] TMP_Text summaryLabel;
        [SerializeField] Image summaryBar;
        [SerializeField] TMP_Text summaryValue;
        [SerializeField] LayoutElement summaryValueLayoutElement;

        public void Bind(string label, float value, float percent, Color barColor)
        {
            if (summaryLabel != null)
                summaryLabel.text = label;

            var percentClamped = Mathf.Clamp01(percent);
            var percentLabel = Mathf.RoundToInt(percentClamped * 100f);

            if (summaryValue != null)
                summaryValue.text = $"{Mathf.RoundToInt(value).ToString("N0", CultureInfo.InvariantCulture)} ({percentLabel}%)";

            if (summaryBar == null)
                return;

            summaryBar.type = Image.Type.Filled;
            summaryBar.fillMethod = Image.FillMethod.Horizontal;
            summaryBar.fillOrigin = (int)Image.OriginHorizontal.Left;
            summaryBar.fillAmount = percentClamped;
            summaryBar.color = barColor;
        }

        public float SummaryValueWidth
        {
            get
            {
                if (summaryValue == null)
                    return 0f;

                summaryValue.ForceMeshUpdate();
                return summaryValue.preferredWidth;
            }
        }

        public void SetSummaryValueWidth(float width)
        {
            if (summaryValueLayoutElement == null)
            {
                Debug.LogError("RunSummaryElement: summaryValueLayoutElement is not assigned.", this);
                return;
            }

            summaryValueLayoutElement.preferredWidth = width;
        }
    }
}
