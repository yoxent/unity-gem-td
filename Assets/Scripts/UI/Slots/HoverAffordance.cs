using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GemTD.UI
{
    /// <summary>Shared hover-X mechanic for slot prefabs. Binds pointer-enter/exit on the slot
    /// button to toggle an X child (gated by canShow). Written once; used by Inventory/Tower slots.</summary>
    public static class HoverAffordance
    {
        public static void BindXHover(Button slotButton, GameObject xChild, Func<bool> canShow)
        {
            if (slotButton == null || xChild == null)
                return;
            var trigger = slotButton.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = slotButton.gameObject.AddComponent<EventTrigger>();
            if (trigger.triggers == null)
                trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => xChild.SetActive(canShow != null && canShow()));
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => xChild.SetActive(false));
            trigger.triggers.Add(exit);

            xChild.SetActive(false); // hidden by default
        }
    }
}