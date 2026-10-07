using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using GemTD.Gameplay;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;
using GemTD.Core;

namespace GemTD.UI
{
    /// <summary>Inventory bar slot. Pointer/drag is handled on this composite root via
    /// <see cref="SlotEventHandler"/> so its child controls share one hover boundary.</summary>
    public sealed class InventoryGemSlot : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] Button discardButton;
        [SerializeField] SlotEventHandler slotEvents;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] InventoryDragGhost dragGhostPrefab;
        [SerializeField] GameObject disabledOverlay;
        [SerializeField] string dropSfxKey = SfxKeys.Drop;

        static InventoryGemSlot s_dragSource;

        public int SlotIndex => _slotIndex;

        GameCompositionRoot _root;
        PopupManager _popup;
        int _slotIndex = -1;
        GemInstance _gem;
        bool _pointerOverSlot;
        bool _pointerInteractable;
        InventoryGemTooltip _tooltip;

        public void Configure(GameCompositionRoot root, PopupManager popup, int slotIndex, GemInstance gem)
        {
            _root = root;
            _popup = popup;
            _slotIndex = slotIndex;
            _gem = gem;
            if (icon != null)
            {
                var filled = !gem.IsEmpty;
                icon.gameObject.SetActive(filled);
                if (filled)
                {
                    icon.sprite = gem.Def.Icon;
                    icon.preserveAspect = true;
                }
            }
            if (nameLabel != null) nameLabel.text = !gem.IsEmpty ? gem.DisplayName : "—";
            RefreshDisabledOverlay();
            RefreshDiscardButtonVisible();
            RefreshGemTooltip();
        }

        public static bool ShouldShowDisabledOverlay(
            GemInstance gem,
            TowerDefinition selectedTower)
        {
            return !gem.IsEmpty
                && selectedTower != null
                && !GemTags.CanSocket(selectedTower, gem);
        }

        public void RefreshDisabledOverlay()
        {
            if (disabledOverlay == null)
                return;
            var selected = _root != null && _root.HasSelectedTower
                ? _root.Placement.Selected
                : null;
            var towerDef = selected != null ? selected.Def : null;
            disabledOverlay.SetActive(ShouldShowDisabledOverlay(_gem, towerDef));
        }

        public void SetPointerInteractable(bool interactable)
        {
            _pointerInteractable = interactable;
            if (slotEvents != null)
                slotEvents.SetInteractable(interactable);
        }

        public void SetTooltip(InventoryGemTooltip tooltip)
        {
            if (_tooltip == tooltip)
                return;

            if (_pointerOverSlot)
                _tooltip?.Hide();
            _tooltip = tooltip;
            RefreshGemTooltip();
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

            if (discardButton != null)
                discardButton.onClick.AddListener(OnDiscardClicked);

            slotEvents.CanBeginDrag = CanStartDrag;
            slotEvents.Clicked = OnSlotClicked;
            slotEvents.RightClicked = OnSlotRightClicked;
            slotEvents.BeginDrag = OnBeginDrag;
            slotEvents.Drag = OnDrag;
            slotEvents.EndDrag = OnEndDrag;
            slotEvents.Drop = OnDrop;

            if (discardButton != null)
                discardButton.gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _pointerOverSlot = true;
            RefreshDiscardButtonVisible();
            RefreshGemTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _pointerOverSlot = false;
            RefreshDiscardButtonVisible();
            _tooltip?.Hide();
        }

        void RefreshGemTooltip()
        {
            if (!_pointerOverSlot)
                return;

            if (!_gem.IsEmpty)
                _tooltip?.Show(_gem);
            else
                _tooltip?.Hide();
        }

        void RefreshDiscardButtonVisible()
        {
            if (discardButton == null)
                return;
            var dragging = slotEvents != null && slotEvents.DragStarted;
            var show = _pointerOverSlot
                       && _root != null && _root.States != null
                       && _root.States.Current == RunStateId.Plan
                       && !_gem.IsEmpty
                       && !dragging;
            if (discardButton.gameObject.activeSelf != show)
                discardButton.gameObject.SetActive(show);
        }

        void OnDiscardClicked()
        {
            UiSfx.Click();
            if (_root == null || _popup == null || _gem.IsEmpty)
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

            UiSfx.Click();
            var kb = Keyboard.current;
            var shift = kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
            _root.RequestInventorySlotClick(_slotIndex, shift);
        }

        void OnSlotRightClicked(PointerEventData eventData)
        {
            if (_root == null || _gem.IsEmpty)
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
            GemDragState.SetInventory(_slotIndex);
            canvasGroup.alpha = 0.35f;
            canvasGroup.blocksRaycasts = false;
            RefreshDiscardButtonVisible();
            ShowGhost(eventData);
        }

        void OnDrag(PointerEventData eventData)
        {
            if (s_dragSource != this)
                return;
            GemDragGhostPool.Move(eventData);
        }

        void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            GemDragGhostPool.Release();
            s_dragSource = null;
            GemDragState.Clear();
            RefreshDiscardButtonVisible();
        }

        void OnDisable()
        {
            if (_pointerOverSlot)
                _tooltip?.Hide();
            _pointerOverSlot = false;

            if (s_dragSource != this)
                return;

            GemDragGhostPool.Release();
            s_dragSource = null;
            GemDragState.Clear();
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

                GameEvents.RaisePlaySfx(dropSfxKey);
                _root.RequestMoveOrSwapInventoryAt(fromIndex, _slotIndex);
                return;
            }

            if (GemDragState.Kind == GemDragState.SourceKind.Socket)
            {
                var fromSocketIndex = GemDragState.SocketIndex;
                if (fromSocketIndex < 0)
                    return;

                GameEvents.RaisePlaySfx(dropSfxKey);
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
            if (!_pointerInteractable || _root == null || _root.States == null)
                return false;
            if (!IsReorderState(_root.States.Current) || _gem.IsEmpty)
                return false;
            var pointerEnter = eventData != null ? eventData.pointerEnter : null;
            if (discardButton != null && pointerEnter != null
                && (pointerEnter == discardButton.gameObject
                    || pointerEnter.transform.IsChildOf(discardButton.transform)))
                return false;
            return true;
        }

        void ShowGhost(PointerEventData eventData)
        {
            GemDragGhostPool.Show(
                    dragGhostPrefab,
                    this,
                    ((RectTransform)transform).rect.size,
                    !_gem.IsEmpty ? _gem.Def.Icon : null,
                    nameLabel != null ? nameLabel.text : (!_gem.IsEmpty ? _gem.DisplayName : "—"),
                    nameLabel != null ? nameLabel.font : null,
                    nameLabel != null ? nameLabel.fontSize : 16f,
                    eventData);
        }
    }
}
