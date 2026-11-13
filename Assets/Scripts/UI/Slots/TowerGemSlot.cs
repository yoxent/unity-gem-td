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
    /// <summary>Tower Details socketed-gem slot. The remove button is visible whenever unsocketing is allowed.</summary>
    public sealed class TowerGemSlot : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        const string EmptySlotText = "—";

        [SerializeField] Image icon;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] Button removeButton;
        [SerializeField] Button slotButton;
        [SerializeField] GameObject lockedIcon;
        [SerializeField] InventoryDragGhost dragGhostPrefab;
        [SerializeField] string dropSfxKey = SfxKeys.Drop;

        GameCompositionRoot _root;
        int _socketIndex = -1;
        GemInstance _gem;
        bool _available;
        bool _pointerOver;
        TowerGemTooltip _tooltip;

        static TowerGemSlot s_dragSource;

        void Awake()
        {
            if (lockedIcon == null)
                Debug.LogError("TowerGemSlot: assign Locked Icon on the prefab.", this);

            if (removeButton != null)
            {
                removeButton.onClick.AddListener(OnRemoveClicked);
                removeButton.gameObject.SetActive(false);
            }
        }

        public void Configure(GameCompositionRoot root, int socketIndex, GemInstance gem)
        {
            _available = true;
            _root = root;
            _socketIndex = socketIndex;
            _gem = gem;
            if (icon != null)
            {
                var filled = !gem.IsEmpty;
                icon.gameObject.SetActive(filled);
                icon.sprite = filled ? gem.Def.Icon : null;
                if (filled) icon.preserveAspect = true;
            }
            if (nameLabel != null) nameLabel.text = !gem.IsEmpty ? gem.DisplayName : EmptySlotText;
            RefreshLockOverlay();
            RefreshTooltip();
        }

        /// <summary>Socket this tower does not have. Stay visible, show the lock icon, and block input.</summary>
        public void SetDisabled()
        {
            _available = false;
            _socketIndex = -1;
            _gem = default;
            if (icon != null)
            {
                icon.gameObject.SetActive(false);
                icon.sprite = null;
            }
            if (nameLabel != null)
                nameLabel.text = EmptySlotText;
            RefreshLockOverlay();
            RefreshTooltip();
        }

        public void SetTooltip(TowerGemTooltip tooltip)
        {
            if (_tooltip == tooltip)
                return;

            if (_pointerOver)
                _tooltip?.Hide();
            _tooltip = tooltip;
            RefreshTooltip();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _pointerOver = true;
            RefreshTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _pointerOver = false;
            _tooltip?.Hide();
        }

        void RefreshTooltip()
        {
            if (!_pointerOver)
                return;

            if (_available && !_gem.IsEmpty)
                _tooltip?.Show(_gem);
            else
                _tooltip?.Hide();
        }

        public void RefreshLockOverlay()
        {
            var locked = !_available || _root == null || _root.SelectedSocketsLocked;
            if (lockedIcon != null && lockedIcon.activeSelf != locked)
                lockedIcon.SetActive(locked);
            if (slotButton != null)
                slotButton.interactable = !locked;
            if (removeButton != null)
                removeButton.gameObject.SetActive(CanUnsocket());
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (s_dragSource != null || !CanBeginDrag())
                return;

            s_dragSource = this;
            GemDragState.SetSocket(_socketIndex);

            ShowGhost(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (s_dragSource != this)
                return;
            GemDragGhostPool.Move(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (s_dragSource != this)
                return;

            GemDragGhostPool.Release();
            s_dragSource = null;
            GemDragState.Clear();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!_available || _root == null || _root.States == null
                || !IsSocketState(_root.States.Current))
                return;

            if (!GemDragState.HasDrag)
                return;

            if (GemDragState.Kind == GemDragState.SourceKind.Inventory)
            {
                if (GemDragState.InventoryIndex < 0 || _socketIndex < 0)
                    return;

                GameEvents.RaisePlaySfx(dropSfxKey);
                _root.RequestSocketFromInventoryAt(GemDragState.InventoryIndex, _socketIndex);
                return;
            }

            if (GemDragState.Kind == GemDragState.SourceKind.Socket)
            {
                if (GemDragState.SocketIndex < 0 || _socketIndex < 0
                    || GemDragState.SocketIndex == _socketIndex)
                    return;

                GameEvents.RaisePlaySfx(dropSfxKey);
                _root.RequestMoveOrSwapSocketAt(
                    GemDragState.SocketIndex, _socketIndex);
            }
        }

        void OnRemoveClicked()
        {
            if (!CanUnsocket())
                return;

            UiSfx.Click();
            _root.RequestUnsocket(_socketIndex);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right || !CanUnsocket())
                return;

            _root.RequestUnsocket(_socketIndex);
        }

        bool CanBeginDrag()
        {
            return CanUnsocket();
        }

        bool CanUnsocket()
        {
            return _available
                && _root != null
                && !_gem.IsEmpty
                && _socketIndex >= 0
                && _root.States != null
                && IsSocketState(_root.States.Current)
                && _root.CanUnsocketSelected(_socketIndex);
        }

        static bool IsSocketState(RunStateId state)
        {
            return state == RunStateId.Plan || state == RunStateId.Combat;
        }

        void OnDisable()
        {
            if (_pointerOver)
                _tooltip?.Hide();
            _pointerOver = false;

            if (s_dragSource != this)
                return;

            GemDragGhostPool.Release();
            s_dragSource = null;
            GemDragState.Clear();
        }

        void ShowGhost(PointerEventData eventData)
        {
            GemDragGhostPool.Show(
                    dragGhostPrefab,
                    this,
                    ((RectTransform)transform).rect.size,
                    !_gem.IsEmpty ? _gem.Def.Icon : null,
                    nameLabel != null ? nameLabel.text : (!_gem.IsEmpty ? _gem.DisplayName : EmptySlotText),
                    nameLabel != null ? nameLabel.font : null,
                    nameLabel != null ? nameLabel.fontSize : 16f,
                    eventData);
        }
    }
}