using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GemTD.UI
{
    /// <summary>Shared hover-X mechanic for slot prefabs. Shows the X child when the pointer is
    /// over the slot OR the X (so moving from slot to X doesn't hide X mid-click).
    /// Written once; used by Inventory/Tower slots.</summary>
    public static class HoverAffordance
    {
        public static void BindXHover(Button slotButton, GameObject xChild, Func<bool> canShow)
        {
            if (slotButton == null || xChild == null)
                return;

            // Shared flag: pointer is over either the slot or the X.
            var overSlot = false;

            void Refresh()
            {
                var show = overSlot && (canShow == null || canShow());
                xChild.SetActive(show);
            }

            // --- Triggers on the slot button ---
            var slotTrigger = slotButton.GetComponent<EventTrigger>();
            if (slotTrigger == null) slotTrigger = slotButton.gameObject.AddComponent<EventTrigger>();
            if (slotTrigger.triggers == null) slotTrigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();
            else slotTrigger.triggers.RemoveAll(e => e.eventID == EventTriggerType.PointerEnter || e.eventID == EventTriggerType.PointerExit);

            var slotEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            slotEnter.callback.AddListener(_ => { overSlot = true; Refresh(); });
            slotTrigger.triggers.Add(slotEnter);

            var slotExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            slotExit.callback.AddListener(_ => { overSlot = false; Refresh(); });
            slotTrigger.triggers.Add(slotExit);

            // --- Triggers on the X child (so moving pointer onto X keeps it visible) ---
            var xButton = xChild.GetComponent<Button>();
            if (xButton != null)
            {
                var xTrigger = xButton.GetComponent<EventTrigger>();
                if (xTrigger == null) xTrigger = xButton.gameObject.AddComponent<EventTrigger>();
                if (xTrigger.triggers == null) xTrigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();
                else xTrigger.triggers.RemoveAll(e => e.eventID == EventTriggerType.PointerEnter || e.eventID == EventTriggerType.PointerExit);

                var xEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                xEnter.callback.AddListener(_ => { overSlot = true; Refresh(); });
                xTrigger.triggers.Add(xEnter);

                var xExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                xExit.callback.AddListener(_ => { overSlot = false; Refresh(); });
                xTrigger.triggers.Add(xExit);
            }

            xChild.SetActive(false); // hidden by default
        }
    }
}