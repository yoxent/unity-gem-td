using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GemTD.UI
{
    /// <summary>
    /// Shared pool-of-one for the authored gem drag ghost. Slot views provide
    /// their prefab and current gem presentation; this helper owns its lifetime
    /// and screen-canvas positioning.
    /// </summary>
    public static class GemDragGhostPool
    {
        static InventoryDragGhost s_ghost;
        static Canvas s_ghostCanvas;

        public static void Show(
            InventoryDragGhost prefab,
            Component owner,
            Vector2 size,
            Sprite icon,
            string text,
            TMP_FontAsset font,
            float fontSize,
            PointerEventData eventData)
        {
            Release();

            if (owner == null)
                return;

            var canvas = owner.GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            var rootCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            if (prefab == null)
            {
                Debug.LogError(
                    "GemDragGhostPool: assign Drag Ghost Prefab on the slot prefab.",
                    owner);
                return;
            }

            if (s_ghost == null || s_ghostCanvas != rootCanvas)
            {
                if (s_ghost != null)
                    Object.Destroy(s_ghost.gameObject);

                s_ghostCanvas = rootCanvas;
                s_ghost = Object.Instantiate(prefab, s_ghostCanvas.transform);
            }

            s_ghost.gameObject.SetActive(true);
            s_ghost.transform.SetAsLastSibling();
            s_ghost.Bind(size, icon, text, font, fontSize);
            Move(eventData);
        }

        public static void Move(PointerEventData eventData)
        {
            if (eventData == null
                || s_ghost == null
                || s_ghostCanvas == null
                || s_ghost.RectTransform == null)
                return;

            var canvasRect = (RectTransform)s_ghostCanvas.transform;
            Camera camera = null;
            if (s_ghostCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                camera = eventData.pressEventCamera != null
                    ? eventData.pressEventCamera
                    : s_ghostCanvas.worldCamera;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, eventData.position, camera, out var local))
                return;

            s_ghost.RectTransform.anchoredPosition = local;
        }

        public static void Release()
        {
            if (s_ghost == null)
            {
                s_ghostCanvas = null;
                return;
            }

            s_ghost.gameObject.SetActive(false);
        }
    }
}
