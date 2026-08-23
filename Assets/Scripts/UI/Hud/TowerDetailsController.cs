using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GemTD.Core;
using GemTD.Gameplay;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Run;

namespace GemTD.UI
{
    /// <summary>Lives on TowerDetailsPanel prefab. Stats, sockets, targeting rows, Sell.</summary>
    public sealed class TowerDetailsController : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text detailsText;
        [SerializeField] TowerGemSlot[] socketSlots = new TowerGemSlot[3];
        [SerializeField] Button sellButton;
        [SerializeField] TMP_Text sellLabel;
        [SerializeField] TowerTargetPriority[] priorityButtons = new TowerTargetPriority[3];
        [SerializeField] Button scopeThisButton;
        [SerializeField] Button scopeTypeButton;
        [SerializeField] Button scopeAllButton;

        GameCompositionRoot _root;
        PopupManager _popup;
        bool _visible;
        bool _lockOverlayShown;

        void OnEnable()
        {
            GameEvents.RunStateChanged += OnHudDirty;
            GameEvents.TowerSelectionChanged += OnHudDirty;
            GameEvents.TargetingChanged += OnHudDirty;
            GameEvents.InventoryChanged += OnHudDirty;
            GameEvents.RequestTargetingAllConfirm += OnRequestTargetingAllConfirm;
        }

        void OnDisable()
        {
            GameEvents.RunStateChanged -= OnHudDirty;
            GameEvents.TowerSelectionChanged -= OnHudDirty;
            GameEvents.TargetingChanged -= OnHudDirty;
            GameEvents.InventoryChanged -= OnHudDirty;
            GameEvents.RequestTargetingAllConfirm -= OnRequestTargetingAllConfirm;
        }

        public void Bind(GameCompositionRoot root, PopupManager popup)
        {
            _root = root;
            _popup = popup;
            if (panel == null) panel = gameObject;
            if (sellButton != null) sellButton.onClick.AddListener(OnSell);

            if (priorityButtons == null || priorityButtons.Length != 3)
                Debug.LogError("TowerDetailsController: priorityButtons must be 3 inspector-dragged TowerTargetPriority rows.", this);

            for (var i = 0; i < priorityButtons.Length; i++)
            {
                if (priorityButtons[i] != null)
                    priorityButtons[i].Bind(_root, i);
            }

            if (scopeThisButton != null)
                scopeThisButton.onClick.AddListener(() => _root?.SetApplyScope(TargetingApplyScope.ThisTower));
            if (scopeTypeButton != null)
                scopeTypeButton.onClick.AddListener(() => _root?.SetApplyScope(TargetingApplyScope.ThisType));
            if (scopeAllButton != null)
                scopeAllButton.onClick.AddListener(ConfirmAllThenSet);

            Refresh();
        }

        void Update()
        {
            if (!_visible || _root == null)
                return;
            var lockLeft = _root.SelectedSocketLockRemaining;
            if (lockLeft > 0f)
                RefreshDetailsText();
            if (lockLeft > 0f || _lockOverlayShown)
                RefreshSocketLockOverlays();
            _lockOverlayShown = lockLeft > 0f;
        }

        void OnHudDirty() => Refresh();

        void Refresh()
        {
            if (_root == null) return;

            _visible = _root.HasSelectedTower && _root.States != null
                       && _root.States.Current != RunStateId.Defeat
                       && _root.States.Current != RunStateId.VictorySummary;

            panel.SetActive(_visible);
            if (!_visible) return;

            RefreshDetailsText();

            var planOrCombat = _root.States != null
                               && (_root.States.Current == RunStateId.Plan
                                   || _root.States.Current == RunStateId.Combat);
            if (sellButton != null)
                sellButton.gameObject.SetActive(planOrCombat);

            var tower = _root.Placement?.Selected;
            if (sellLabel != null && planOrCombat && tower != null)
                sellLabel.text = $"Sell {RunEconomy.ComputeSellRefund(tower.PurchaseCost, tower.UpgradeSpend)}";

            var socketCount = tower?.Def != null ? tower.Def.SocketCount : 0;
            for (var i = 0; i < socketSlots.Length; i++)
            {
                if (socketSlots[i] == null) continue;
                var showSlot = tower != null && i < socketCount;
                socketSlots[i].gameObject.SetActive(showSlot);
                if (!showSlot) continue;
                var gem = tower.Sockets != null && i < tower.Sockets.Length ? tower.Sockets[i] : null;
                socketSlots[i].Configure(_root, i, gem);
            }
            _lockOverlayShown = _root.SelectedSocketLockRemaining > 0f;

            if (tower != null && priorityButtons != null)
            {
                for (var i = 0; i < priorityButtons.Length; i++)
                {
                    if (priorityButtons[i] == null) continue;
                    priorityButtons[i].Refresh(tower.Targeting.Get(i));
                }
            }

            HighlightScope(_root.CurrentApplyScope);
        }

        void RefreshDetailsText()
        {
            if (detailsText != null && _root != null)
                detailsText.text = _root.BuildSelectedTowerDetailsText();
        }

        void RefreshSocketLockOverlays()
        {
            for (var i = 0; i < socketSlots.Length; i++)
            {
                if (socketSlots[i] == null || !socketSlots[i].gameObject.activeSelf)
                    continue;
                socketSlots[i].RefreshLockOverlay();
            }
        }

        void HighlightScope(TargetingApplyScope scope)
        {
            SetScopeHighlight(scopeThisButton, scope == TargetingApplyScope.ThisTower);
            SetScopeHighlight(scopeTypeButton, scope == TargetingApplyScope.ThisType);
            SetScopeHighlight(scopeAllButton, scope == TargetingApplyScope.AllTowers);
        }

        static void SetScopeHighlight(Button button, bool on)
        {
            if (button == null) return;
            var colors = button.colors;
            colors.colorMultiplier = on ? 1f : 0.65f;
            button.colors = colors;
        }

        void ConfirmAllThenSet()
        {
            if (_root == null) return;
            if (_root.CurrentApplyScope == TargetingApplyScope.AllTowers)
                return;

            if (_popup == null)
            {
                _root.SetApplyScope(TargetingApplyScope.AllTowers);
                return;
            }

            _popup.ShowConfirmOnceSuppressed(
                id: "TargetingApplyAll",
                title: "Apply to all towers?",
                body: "This targeting will apply to every placed tower.",
                onConfirm: () => _root.SetApplyScope(TargetingApplyScope.AllTowers),
                pauseForFairness: false,
                yesText: "Yes",
                noText: "No");
        }

        void OnRequestTargetingAllConfirm() => ConfirmAllThenSet();

        void OnSell()
        {
            if (_root == null || !_root.HasSelectedTower) return;

            if (!_root.CanSellSelected)
            {
                if (_popup != null)
                {
                    _popup.ShowInfo(
                        title: "Can't sell",
                        body: "Inventory cannot fit this tower's socketed gems. Discard gems first.");
                }
                return;
            }

            if (_popup == null)
            {
                _root.RequestSellSelected();
                return;
            }

            _popup.ShowConfirmOnceSuppressed(
                id: "SellConfirm",
                title: "Sell tower?",
                body: _root.SelectedHasSocketedGems
                    ? "Socketed gems return to inventory. Full refund of purchase + upgrade spend."
                    : "Full refund of purchase + upgrade spend.",
                onConfirm: () => _root.RequestSellSelected(),
                pauseForFairness: false,
                yesText: "Yes", noText: "No");
        }
    }
}
