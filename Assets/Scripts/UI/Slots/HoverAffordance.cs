using System;
using UnityEngine;

namespace GemTD.UI
{
    /// <summary>Shared hover-X mechanic for slot prefabs. Shows the X child when the pointer is
    /// over the slot OR the X (so moving from slot to X doesn't hide X mid-click).
    /// Relays must already exist on the prefab — this only assigns callbacks.</summary>
    public static class HoverAffordance
    {
        public static void BindXHover(
            HoverPointerRelay slotRelay,
            HoverPointerRelay xRelay,
            GameObject xChild,
            Func<bool> canShow)
        {
            if (slotRelay == null || xChild == null)
                return;

            var overSlot = false;
            var overX = false;

            void Refresh()
            {
                var show = (overSlot || overX) && (canShow == null || canShow());
                xChild.SetActive(show);
            }

            slotRelay.OnEnter = () => { overSlot = true; Refresh(); };
            slotRelay.OnExit = () => { overSlot = false; Refresh(); };

            if (xRelay != null)
            {
                xRelay.OnEnter = () => { overX = true; Refresh(); };
                xRelay.OnExit = () => { overX = false; Refresh(); };
            }

            xChild.SetActive(false);
        }
    }
}
