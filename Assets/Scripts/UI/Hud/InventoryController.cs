using System.Collections.Generic;
using UnityEngine;
using TMPro;
using GemTD.Core;
using GemTD.Gameplay;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;

namespace GemTD.UI
{
    /// <summary>Lives on InventoryPanel prefab. Manages 10 InventoryGemSlot instances + hint text.</summary>
    public sealed class InventoryController : MonoBehaviour
    {
        [SerializeField] TMP_Text inventoryHintText;
        [SerializeField] GameObject panel;
        [SerializeField] List<InventoryGemSlot> slots = new List<InventoryGemSlot>();

        GameCompositionRoot _root;
        PopupManager _popup;
        bool _buttonsBound;

        void OnEnable()
        {
            GameEvents.RunStateChanged += Refresh;
            GameEvents.InventoryChanged += Refresh;
            GameEvents.TowerSelectionChanged += Refresh;
        }

        void OnDisable()
        {
            GameEvents.RunStateChanged -= Refresh;
            GameEvents.InventoryChanged -= Refresh;
            GameEvents.TowerSelectionChanged -= Refresh;
        }

        public void Bind(GameCompositionRoot root, PopupManager popup)
        {
            _root = root;
            _popup = popup;
            if (panel == null)
                Debug.LogError("InventoryController: assign Panel on the prefab.", this);
            if (slots == null || slots.Count == 0)
                Debug.LogError("InventoryController: assign InventoryGemSlot refs on the prefab.", this);
            _buttonsBound = slots != null && slots.Count > 0;
            Refresh();
        }

        void Refresh()
        {
            if (!_buttonsBound || _root == null) return;

            var show = _root.Inventory != null && _root.States != null
                       && _root.States.Current != RunStateId.Boot
                       && _root.States.Current != RunStateId.Defeat
                       && _root.States.Current != RunStateId.VictorySummary;
            panel.SetActive(show);
            if (!show) return;

            var inv = _root.Inventory;
            if (inv == null) return;
            var canSocket = (_root.States.Current == RunStateId.Plan || _root.States.Current == RunStateId.Combat)
                            && _root.HasSelectedTower && _root.SelectedSocketLockRemaining <= 0f;
            var replacePick = _root.States.Current == RunStateId.Draft
                              && _root.Draft != null
                              && _root.Draft.ReplacePhase == DraftReplacePhase.AwaitingInventoryPick;
            var inPlan = _root.States.Current == RunStateId.Plan;

            if (inventoryHintText != null)
            {
                if (replacePick)
                    inventoryHintText.text = "Inventory — click a gem to DESTROY & take draft card";
                else if (canSocket)
                    inventoryHintText.text = $"Inventory {inv.OccupiedCount}/{inv.Capacity} — click=socket | Shift+click=discard (Plan)";
                else if (inPlan)
                    inventoryHintText.text = $"Inventory {inv.OccupiedCount}/{inv.Capacity} — select a tower to socket";
                else
                    inventoryHintText.text = $"Inventory {inv.OccupiedCount}/{inv.Capacity}";
            }

            for (var i = 0; i < slots.Count && i < inv.Slots.Count; i++)
            {
                var gem = inv.Slots[i];
                if (slots[i] == null) continue;

                slots[i].Configure(_root, _popup, i, gem);

                var filled = gem != null;
                slots[i].SetPointerInteractable(filled && (canSocket || replacePick || inPlan));
            }
        }
    }
}
