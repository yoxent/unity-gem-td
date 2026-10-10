using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

namespace GemTD.UI
{
    /// <summary>Generic hover-tooltip controller. Polls EventSystem.RaycastAll for ITooltipable and
    /// shows/repositions a single pre-placed panel. No tags, no per-icon handlers.</summary>
    public sealed class TooltipController : MonoBehaviour
    {
        [SerializeField] RectTransform tooltipPanel;
        [SerializeField] TMP_Text tooltipText;
        [SerializeField] Canvas rootCanvas;
        [SerializeField] float padding = 12f;
        [SerializeField] float yOffset = 16f;

        PointerEventData _eventData;
        readonly List<RaycastResult> _hits = new List<RaycastResult>(8);

        private void Start()
        {
            tooltipPanel.gameObject.SetActive(false);
        }

        void Update()
        {
            if (tooltipPanel == null || tooltipText == null)
                return;

            if (HoveredTooltipable(out var text, out var screenPos))
            {
                tooltipPanel.gameObject.SetActive(true);
                tooltipText.text = text;
                var rt = tooltipPanel;
                rt.position = RepositionAroundCursor(
                    screenPos, rt.rect.size, rt.pivot,
                    new Vector2(Screen.width, Screen.height), padding, yOffset);
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
            _eventData.position = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
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

        /// <summary>Places the tooltip above the cursor; flips below if the above rect would leave the screen. Testable.</summary>
        public static Vector2 RepositionAroundCursor(
            Vector2 cursorPos, Vector2 tooltipSize, Vector2 pivot, Vector2 screenDim, float pad, float yOff)
        {
            var maxY = screenDim.y - pad;
            var topIfAbove = cursorPos.y + yOff + tooltipSize.y;
            var y = topIfAbove > maxY
                ? cursorPos.y - yOff - tooltipSize.y * (1f - pivot.y)
                : cursorPos.y + yOff + tooltipSize.y * pivot.y;

            var x = cursorPos.x;
            var left = x - tooltipSize.x * pivot.x;
            if (left + tooltipSize.x > screenDim.x - pad)
                x = screenDim.x - pad - tooltipSize.x * (1f - pivot.x);
            if (x - tooltipSize.x * pivot.x < pad)
                x = pad + tooltipSize.x * pivot.x;

            var bottom = y - tooltipSize.y * pivot.y;
            if (bottom < pad)
                y = pad + tooltipSize.y * pivot.y;
            var top = y + tooltipSize.y * (1f - pivot.y);
            if (top > maxY)
                y = maxY - tooltipSize.y * (1f - pivot.y);

            return new Vector2(x, y);
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