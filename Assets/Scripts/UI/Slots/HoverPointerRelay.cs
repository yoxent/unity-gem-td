using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GemTD.UI
{
    /// <summary>Pointer enter/exit only. Unlike EventTrigger, this does not implement
    /// drag/drop interfaces, so parent IBeginDragHandler still receives events.</summary>
    public sealed class HoverPointerRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action OnEnter;
        public Action OnExit;

        public void OnPointerEnter(PointerEventData eventData) => OnEnter?.Invoke();
        public void OnPointerExit(PointerEventData eventData) => OnExit?.Invoke();
    }
}
