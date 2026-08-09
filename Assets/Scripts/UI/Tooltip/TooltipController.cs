using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GemTD.UI
{
    /// <summary>Generic hover-tooltip controller. Polls EventSystem.RaycastAll for ITooltipable and
    /// shows/repositions a single pre-placed panel. No tags, no per-icon handlers.</summary>
    public sealed class TooltipController : MonoBehaviour
    {
        [SerializeField] RectTransform tooltipPanel;
        [SerializeField] Text tooltipText;
        [SerializeField] Canvas rootCanvas;
        [SerializeField] float padding = 12f;

        PointerEventData _eventData;
        readonly System.Collections.Generic.List<RaycastResult> _hits = new System.Collections.Generic.List<RaycastResult>(8);

        void Update()
        {
            if (tooltipPanel == null || tooltipText == null)
                return;

            if (HoveredTooltipable(out var text, out var screenPos))
            {
                tooltipPanel.gameObject.SetActive(true);
                tooltipText.text = text;
                var rt = tooltipPanel;
                var size = rt.rect.size;
                var anchored = RepositionWithinScreen(screenPos, size, new Vector2(Screen.width, Screen.height), padding);
                rt.position = anchored;
            }
            else
            {
                tooltipPanel.gameObject.SetActive(false);
            }
        }

        bool HoveredTooltipable(out string text, out Vector2 screenPos)
        {
            text = null;
            screenPos = Vector2.zero;
            var es = EventSystem.current;
            if (es == null)
                return false;
            if (_eventData == null)
                _eventData = new PointerEventData(es);
            _eventData.Reset();
            _eventData.position = UnityEngine.Input.mousePosition;
            _hits.Clear();
            es.RaycastAll(_eventData, _hits);
            for (var i = 0; i < _hits.Count; i++)
            {
                var src = _hits[i].gameObject.GetComponent<ITooltipable>();
                if (src != null && !string.IsNullOrEmpty(src.TooltipText))
                {
                    text = src.TooltipText;
                    screenPos = _eventData.position;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Clamps a tooltip rect (pivot assumed top-left at anchor) within screen bounds. Testable.</summary>
        public static Vector2 RepositionWithinScreen(Vector2 anchorPos, Vector2 tooltipSize, Vector2 screenDim, float pad)
        {
            var x = anchorPos.x;
            var y = anchorPos.y;
            // Convention: anchor is bottom-left corner; tooltip extends right (+x) and up (+y) in screen coords.
            if (x + tooltipSize.x > screenDim.x - pad)
                x = screenDim.x - tooltipSize.x - pad;
            if (x < pad)
                x = pad;
            if (y + tooltipSize.y > screenDim.y - pad)
                y = screenDim.y - tooltipSize.y - pad;
            if (y < pad)
                y = pad;
            return new Vector2(x, y);
        }
    }
}