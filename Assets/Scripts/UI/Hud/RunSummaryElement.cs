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

        public void Bind(string label, float value, float percent, Color barColor)
        {
            if (summaryLabel != null)
                summaryLabel.text = label;

            var percentClamped = Mathf.Clamp01(percent);
            var percentLabel = Mathf.RoundToInt(percentClamped * 100f);

            if (summaryValue != null)
                summaryValue.text = $"{Mathf.RoundToInt(value)} ({percentLabel}%)";

            if (summaryBar == null)
                return;

            summaryBar.type = Image.Type.Filled;
            summaryBar.fillMethod = Image.FillMethod.Horizontal;
            summaryBar.fillOrigin = (int)Image.OriginHorizontal.Left;
            summaryBar.fillAmount = percentClamped;
            summaryBar.color = barColor;
        }
    }
}
