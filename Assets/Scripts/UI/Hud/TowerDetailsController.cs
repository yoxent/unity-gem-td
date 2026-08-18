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
        [SerializeField] Button[] priorityButtons = new Button[3];
        [SerializeField] TMP_Text[] priorityLabels = new TMP_Text[3];
        [SerializeField] Button scopeThisButton;
        [SerializeField] Button scopeTypeButton;
        [SerializeField] Button scopeAllButton;

        GameCompositionRoot _root;
        bool _priorityClicksWired;

        void OnEnable()
        {
            GameEvents.RequestTargetingAllConfirm += OnRequestTargetingAllConfirm;
        }

        void OnDisable()
        {
            GameEvents.RequestTargetingAllConfirm -= OnRequestTargetingAllConfirm;
        }

        void Start()
        {
            if (panel == null) panel = gameObject;
            if (sellButton != null) sellButton.onClick.AddListener(OnSell);

            if (priorityButtons == null || priorityButtons.Length != 3)
                Debug.LogError("TowerDetailsController: priorityButtons must be 3 inspector-dragged Buttons.", this);
            if (priorityLabels == null || priorityLabels.Length != 3)
                Debug.LogError("TowerDetailsController: priorityLabels must be 3 inspector-dragged TMP_Texts.", this);
            if (scopeThisButton == null || scopeTypeButton == null || scopeAllButton == null)
                Debug.LogError("TowerDetailsController: scope This/Type/All Buttons must be assigned.", this);

            WirePriorityClicks();
            if (scopeThisButton != null)
                scopeThisButton.onClick.AddListener(() => _root?.SetApplyScope(TargetingApplyScope.ThisTower));
            if (scopeTypeButton != null)
                scopeTypeButton.onClick.AddListener(() => _root?.SetApplyScope(TargetingApplyScope.ThisType));
            if (scopeAllButton != null)
                scopeAllButton.onClick.AddListener(ConfirmAllThenSet);
        }

        void WirePriorityClicks()
        {
            if (_priorityClicksWired || priorityButtons == null)
                return;
            for (var i = 0; i < priorityButtons.Length; i++)
            {
                if (priorityButtons[i] == null)
                    continue;
                var slot = i;
                priorityButtons[i].onClick.AddListener(() => _root?.CyclePriority(slot));
            }
            _priorityClicksWired = true;
        }

        void Update()
        {
            if (_root == null) _root = GameCompositionRoot.Instance;
            if (_root == null) return;

            var show = _root.HasSelectedTower && _root.States != null
                       && _root.States.Current != RunStateId.Defeat
                       && _root.States.Current != RunStateId.VictorySummary;

            panel.SetActive(show);
            if (!show) return;

            Refresh();
        }

        void Refresh()
        {
            if (detailsText != null) detailsText.text = _root.BuildSelectedTowerDetailsText();

            var plan = _root.States != null && _root.States.Current == RunStateId.Plan;
            if (sellButton != null)
                sellButton.gameObject.SetActive(plan);

            var tower = _root.Placement?.Selected;
            for (var i = 0; i < socketSlots.Length; i++)
            {
                if (socketSlots[i] == null || tower == null) continue;
                var gem = tower.Sockets != null && i < tower.Sockets.Length ? tower.Sockets[i] : null;
                socketSlots[i].Configure(_root, i, gem);
            }

            if (tower != null && priorityLabels != null)
            {
                for (var i = 0; i < priorityLabels.Length; i++)
                {
                    if (priorityLabels[i] == null) continue;
                    priorityLabels[i].text = TargetingKeyLabels.For(tower.Targeting.Get(i));
                }
            }

            HighlightScope(_root.CurrentApplyScope);
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

            var popup = FindFirstObjectByType<PopupManager>(FindObjectsInactive.Include);
            if (popup == null)
            {
                _root.SetApplyScope(TargetingApplyScope.AllTowers);
                return;
            }

            popup.ShowConfirmOnceSuppressed(
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

            var popup = FindFirstObjectByType<PopupManager>(FindObjectsInactive.Include);

            if (!_root.CanSellSelected)
            {
                if (popup != null)
                {
                    popup.ShowInfo(
                        title: "Can't sell",
                        body: "Inventory cannot fit this tower's socketed gems. Discard gems first.");
                }
                return;
            }

            if (popup == null)
            {
                _root.RequestSellSelected();
                return;
            }

            popup.ShowConfirmOnceSuppressed(
                id: "SellConfirm",
                title: "Sell tower?",
                body: _root.SelectedHasSocketedGems
                    ? "Socketed gems return to inventory. 50% refund of purchase + upgrade spend."
                    : "50% refund of purchase + upgrade spend.",
                onConfirm: () => _root.RequestSellSelected(),
                pauseForFairness: false,
                yesText: "Yes", noText: "No");
        }
    }
}
