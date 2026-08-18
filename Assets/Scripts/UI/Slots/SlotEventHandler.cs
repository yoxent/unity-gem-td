using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GemTD.UI
{
    /// <summary>Pointer/drag sink on the raycast Graphic (usually child Slot).
    /// Implements EventSystem interfaces directly. Prefab assigns <see cref="targetGraphic"/>
    /// and tint <see cref="colors"/> — no Button required.</summary>
    public sealed class SlotEventHandler : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] Graphic targetGraphic;
        [SerializeField] ColorBlock colors = ColorBlock.defaultColorBlock;

        Color _baseColor = Color.white;
        bool _interactable = true;
        bool _hovered;
        bool _pressed;
        bool _dragStarted;
        bool _suppressPointerUp;

        public Func<PointerEventData, bool> CanBeginDrag;
        public Action<PointerEventData> Clicked;
        public Action<PointerEventData> BeginDrag;
        public Action<PointerEventData> Drag;
        public Action<PointerEventData> EndDrag;
        public Action<PointerEventData> Drop;
        public Action<bool> HoverChanged;

        public bool DragStarted => _dragStarted;

        public void SetBaseColor(Color color)
        {
            _baseColor = color;
            ApplyVisual();
        }

        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            if (!_interactable)
                _pressed = false;
            ApplyVisual();
        }

        void Awake()
        {
            if (targetGraphic == null)
            {
                Debug.LogError(
                    "SlotEventHandler: assign Target Graphic on the prefab (the raycast Image).",
                    this);
                return;
            }

            ApplyVisual();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            ApplyVisual();
            HoverChanged?.Invoke(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            ApplyVisual();
            HoverChanged?.Invoke(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_interactable)
                return;
            _pressed = true;
            ApplyVisual();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            ApplyVisual();

            if (_suppressPointerUp)
            {
                _suppressPointerUp = false;
                return;
            }
            if (!_interactable)
                return;
            Clicked?.Invoke(eventData);
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (CanBeginDrag != null && !CanBeginDrag(eventData))
                return;

            _dragStarted = true;
            _suppressPointerUp = true;
            _pressed = false;
            ApplyVisual();
            BeginDrag?.Invoke(eventData);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (!_dragStarted)
                return;
            Drag?.Invoke(eventData);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (!_dragStarted)
                return;

            _dragStarted = false;
            ApplyVisual();
            EndDrag?.Invoke(eventData);
        }

        public void OnDrop(PointerEventData eventData)
        {
            Drop?.Invoke(eventData);
        }

        void ApplyVisual()
        {
            if (targetGraphic == null)
                return;

            Color color;
            if (!_interactable)
                color = Multiply(_baseColor, colors.disabledColor);
            else if (_pressed || _dragStarted)
                color = Multiply(_baseColor, colors.pressedColor);
            else if (_hovered)
                color = Multiply(_baseColor, colors.highlightedColor);
            else
                color = Multiply(_baseColor, colors.normalColor);

            targetGraphic.color = color;
        }

        static Color Multiply(Color a, Color b)
        {
            return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
        }
    }
}
