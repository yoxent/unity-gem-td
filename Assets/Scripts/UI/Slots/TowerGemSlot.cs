using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GemTD.Gameplay;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;
using GemTD.Core;
using UnityEngine.EventSystems;

namespace GemTD.UI
{
    /// <summary>Tower Details socketed-gem slot. Hover shows X; X click = instant unsocket. Prefab-based.</summary>
    public sealed class TowerGemSlot : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] Button xButton;
        [SerializeField] Button slotButton;
        [SerializeField] GameObject lockedIcon;
        [SerializeField] HoverPointerRelay slotHover;
        [SerializeField] HoverPointerRelay xHover;
        [SerializeField] string dropSfxKey = "Drop";

        GameCompositionRoot _root;
        int _socketIndex = -1;
        GemInstance _gem;

        static TowerGemSlot s_dragSource;
        static RectTransform s_ghost;
        static Canvas s_ghostCanvas;

        public Button SlotButton => slotButton;

        void Awake()
        {
            if (lockedIcon == null)
                Debug.LogError("TowerGemSlot: assign Locked Icon on the prefab.", this);

            if (xButton != null)
                xButton.onClick.AddListener(OnXClicked);

            if (slotHover == null)
            {
                Debug.LogError("TowerGemSlot: assign Slot Hover (HoverPointerRelay) on the prefab.", this);
                return;
            }

            HoverAffordance.BindXHover(
                slotHover,
                xHover,
                xButton != null ? xButton.gameObject : null,
                () => _root != null && _root.CanUnsocketSelected(_socketIndex));
        }

        public void Configure(GameCompositionRoot root, int socketIndex, GemInstance gem)
        {
            _root = root;
            _socketIndex = socketIndex;
            _gem = gem;
            if (icon != null) icon.color = !gem.IsEmpty ? Color.white : new Color(0.18f, 0.18f, 0.22f, 1f);
            if (nameLabel != null) nameLabel.text = !gem.IsEmpty ? gem.DisplayName : "—";
            if (xButton != null && (_root == null || !_root.CanUnsocketSelected(_socketIndex)))
                xButton.gameObject.SetActive(false);
            RefreshLockOverlay();
        }

        public void RefreshLockOverlay()
        {
            var locked = _root != null && _root.SelectedSocketsLocked;
            if (lockedIcon != null && lockedIcon.activeSelf != locked)
                lockedIcon.SetActive(locked);
            if (slotButton != null)
                slotButton.interactable = !locked;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanBeginDrag())
                return;
            if (s_dragSource != null)
                return;

            s_dragSource = this;
            GemDragState.SetSocket(_socketIndex, _gem);

            ShowGhost(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (s_dragSource != this)
                return;
            MoveGhost(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (s_dragSource != this)
                return;

            DestroyGhost();
            s_dragSource = null;
            GemDragState.Clear();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_root == null || _root.States == null)
                return;

            if (!(_root.States.Current == RunStateId.Plan || _root.States.Current == RunStateId.Combat))
                return;

            if (!GemDragState.HasDrag)
                return;

            // Inventory -> socket is the main direction.
            if (GemDragState.Kind != GemDragState.SourceKind.Inventory)
                return;

            if (GemDragState.InventoryIndex < 0 || _socketIndex < 0)
                return;

            GameEvents.RaisePlaySfx(dropSfxKey);
            _root.RequestSocketFromInventoryAt(GemDragState.InventoryIndex, _socketIndex);
        }

        void OnXClicked()
        {
            if (_root == null)
                return;
            _root.RequestUnsocket(_socketIndex);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right)
                return;
            if (_root == null || _gem.IsEmpty)
                return;
            if (_root.States == null)
                return;
            var s = _root.States.Current;
            if (s != RunStateId.Plan && s != RunStateId.Combat)
                return;
            if (!_root.CanUnsocketSelected(_socketIndex))
                return;

            _root.RequestUnsocket(_socketIndex);
        }

        bool CanBeginDrag()
        {
            if (_root == null || _gem.IsEmpty)
                return false;
            if (_socketIndex < 0)
                return false;
            if (!_root.CanUnsocketSelected(_socketIndex))
                return false;
            if (_root.States == null)
                return false;

            var s = _root.States.Current;
            return s == RunStateId.Plan || s == RunStateId.Combat;
        }

        void ShowGhost(PointerEventData eventData)
        {
            DestroyGhost();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            s_ghostCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            var go = new GameObject("SocketDragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
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
            bg.color = new Color(0.28f, 0.42f, 0.32f, 1f);

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
            tmp.text = nameLabel != null ? nameLabel.text : (!_gem.IsEmpty ? _gem.DisplayName : "—");
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