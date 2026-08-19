using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using GemTD.Gameplay;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;

namespace GemTD.UI
{
    /// <summary>Inventory bar slot. Pointer/drag lives on child Slot via <see cref="SlotEventHandler"/>.</summary>
    public sealed class InventoryGemSlot : MonoBehaviour
    {
        static readonly Color FilledColor = new Color(0.28f, 0.42f, 0.32f, 1f);
        static readonly Color EmptyColor = new Color(0.16f, 0.17f, 0.2f, 1f);

        [SerializeField] Image icon;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] Button xButton;
        [SerializeField] SlotEventHandler slotEvents;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] HoverPointerRelay xHover;

        static InventoryGemSlot s_dragSource;
        static RectTransform s_ghost;
        static Canvas s_ghostCanvas;

        public int SlotIndex => _slotIndex;

        GameCompositionRoot _root;
        PopupManager _popup;
        int _slotIndex = -1;
        GemDefinition _gem;
        bool _pointerOverSlot;
        bool _pointerOverX;

        public void Configure(GameCompositionRoot root, PopupManager popup, int slotIndex, GemDefinition gem)
        {
            _root = root;
            _popup = popup;
            _slotIndex = slotIndex;
            _gem = gem;
            if (icon != null) icon.color = gem != null ? Color.white : new Color(0.18f, 0.18f, 0.22f, 1f);
            if (nameLabel != null) nameLabel.text = gem != null ? gem.DisplayName : "—";
            if (slotEvents != null)
                slotEvents.SetBaseColor(gem != null ? FilledColor : EmptyColor);
            RefreshXVisible();
        }

        public void SetPointerInteractable(bool interactable)
        {
            if (slotEvents != null)
                slotEvents.SetInteractable(interactable);
        }

        void Awake()
        {
            if (slotEvents == null || canvasGroup == null)
            {
                Debug.LogError(
                    "InventoryGemSlot: assign Slot Events and Canvas Group on the prefab.",
                    this);
                return;
            }

            if (xButton != null)
                xButton.onClick.AddListener(OnXClicked);

            slotEvents.CanBeginDrag = CanStartDrag;
            slotEvents.Clicked = OnSlotClicked;
            slotEvents.RightClicked = OnSlotRightClicked;
            slotEvents.BeginDrag = OnBeginDrag;
            slotEvents.Drag = OnDrag;
            slotEvents.EndDrag = OnEndDrag;
            slotEvents.Drop = OnDrop;
            slotEvents.HoverChanged = OnHoverChanged;

            if (xHover != null)
            {
                xHover.OnEnter = () => { _pointerOverX = true; RefreshXVisible(); };
                xHover.OnExit = () => { _pointerOverX = false; RefreshXVisible(); };
            }
            else if (xButton != null)
            {
                Debug.LogError(
                    "InventoryGemSlot: assign X Hover (HoverPointerRelay on XButton) on the prefab.",
                    this);
            }

            if (xButton != null)
                xButton.gameObject.SetActive(false);
        }

        void OnHoverChanged(bool over)
        {
            _pointerOverSlot = over;
            RefreshXVisible();
        }

        void RefreshXVisible()
        {
            if (xButton == null)
                return;
            var dragging = slotEvents != null && slotEvents.DragStarted;
            var show = (_pointerOverSlot || _pointerOverX)
                       && _root != null && _root.States != null
                       && _root.States.Current == RunStateId.Plan
                       && _gem != null
                       && !dragging;
            xButton.gameObject.SetActive(show);
        }

        void OnXClicked()
        {
            if (_root == null || _popup == null || _gem == null)
                return;
            if (slotEvents != null && slotEvents.DragStarted)
                return;

            var gemName = _gem.DisplayName;
            var idx = _slotIndex;
            _popup.ShowConfirmOnceSuppressed(
                id: "DiscardGem",
                title: "Discard gem?",
                body: $"{gemName} will be lost.",
                onConfirm: () => _root.RequestDiscardAt(idx),
                pauseForFairness: true,
                yesText: "Yes", noText: "No");
        }

        void OnSlotClicked(PointerEventData eventData)
        {
            if (_root == null)
                return;

            var kb = Keyboard.current;
            var shift = kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
            _root.RequestInventorySlotClick(_slotIndex, shift);
        }

        void OnSlotRightClicked(PointerEventData eventData)
        {
            if (_root == null || _gem == null)
                return;
            if (_root.States == null)
                return;
            var s = _root.States.Current;
            if (s != RunStateId.Plan && s != RunStateId.Combat)
                return;

            // Socket into the selected tower's first free socket.
            _root.RequestSocketFromInventory(_slotIndex);
        }

        void OnBeginDrag(PointerEventData eventData)
        {
            s_dragSource = this;
            GemDragState.SetInventory(_slotIndex, _gem);
            canvasGroup.alpha = 0.35f;
            canvasGroup.blocksRaycasts = false;
            RefreshXVisible();
            ShowGhost(eventData);
        }

        void OnDrag(PointerEventData eventData)
        {
            if (s_dragSource != this)
                return;
            MoveGhost(eventData);
        }

        void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            DestroyGhost();
            s_dragSource = null;
            GemDragState.Clear();
            RefreshXVisible();
        }

        void OnDrop(PointerEventData eventData)
        {
            if (_root == null || _root.States == null)
                return;
            if (!IsReorderState(_root.States.Current))
                return;
            if (!GemDragState.HasDrag)
                return;

            if (GemDragState.Kind == GemDragState.SourceKind.Inventory)
            {
                var fromIndex = GemDragState.InventoryIndex;
                if (fromIndex < 0 || fromIndex == _slotIndex)
                    return;

                _root.RequestMoveOrSwapInventoryAt(fromIndex, _slotIndex);
                return;
            }

            if (GemDragState.Kind == GemDragState.SourceKind.Socket)
            {
                var fromSocketIndex = GemDragState.SocketIndex;
                if (fromSocketIndex < 0)
                    return;

                _root.RequestUnsocketToInventoryAt(fromSocketIndex, _slotIndex);
                return;
            }
        }

        static bool IsReorderState(RunStateId state)
        {
            return state == RunStateId.Plan || state == RunStateId.Combat;
        }

        bool CanStartDrag(PointerEventData eventData)
        {
            if (_root == null || _root.States == null)
                return false;
            if (!IsReorderState(_root.States.Current) || _gem == null)
                return false;
            if (xButton != null && eventData.pointerEnter == xButton.gameObject)
                return false;
            return true;
        }

        void ShowGhost(PointerEventData eventData)
        {
            DestroyGhost();
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            s_ghostCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            var go = new GameObject("InventoryDragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            s_ghost = go.GetComponent<RectTransform>();
            s_ghost.SetParent(s_ghostCanvas.transform, false);
            s_ghost.SetAsLastSibling();
            s_ghost.sizeDelta = ((RectTransform)transform).rect.size;

            var group = go.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0.9f;

            var bg = go.GetComponent<Image>();
            bg.raycastTarget = false;
            bg.color = FilledColor;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.SetParent(s_ghost, false);
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = nameLabel != null ? nameLabel.fontSize : 16f;
            tmp.text = nameLabel != null ? nameLabel.text : (_gem != null ? _gem.DisplayName : "—");
            if (nameLabel != null)
                tmp.font = nameLabel.font;

            MoveGhost(eventData);
        }

        static void MoveGhost(PointerEventData eventData)
        {
            if (s_ghost == null || s_ghostCanvas == null)
                return;

            var canvasRt = (RectTransform)s_ghostCanvas.transform;
            Camera cam = null;
            if (s_ghostCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : s_ghostCanvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRt, eventData.position, cam, out var local))
                return;

            s_ghost.anchoredPosition = local;
        }

        static void DestroyGhost()
        {
            if (s_ghost == null)
                return;
            Destroy(s_ghost.gameObject);
            s_ghost = null;
            s_ghostCanvas = null;
        }
    }
}
