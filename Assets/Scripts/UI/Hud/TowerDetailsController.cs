using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GemTD.Core;
using GemTD.Gameplay;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;

namespace GemTD.UI
{
    /// <summary>Lives on TowerDetailsPanel prefab. Stats, sockets, targeting rows, Sell.</summary>
    public sealed class TowerDetailsController : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] GameObject tagsPanel;
        [SerializeField] Transform tagSubParentTemplate;
        [SerializeField] TagLabel tagLabelPrefab;
        [SerializeField] List<TagLabel> tagLabels = new List<TagLabel>();
        [SerializeField] TowerGemSlot[] socketSlots = new TowerGemSlot[3];
        [SerializeField] Button sellButton;
        [SerializeField] TMP_Text sellLabel;
        [SerializeField] TowerTargetPriority[] priorityButtons = new TowerTargetPriority[3];
        [SerializeField] Button scopeThisButton;
        [SerializeField] Button scopeTypeButton;
        [SerializeField] Button scopeAllButton;

        [Header("Tower Info")]
        [SerializeField] TMP_Text towerNameText;
        [SerializeField] TMP_Text towerLevelText;
        [SerializeField] TMP_Text towerDamageValueText;
        [SerializeField] TMP_Text towerAttackSpeedValueText;
        [SerializeField] TMP_Text towerAttackRangeValueText;
        [SerializeField] TMP_Text towerCriticalChanceValue;
        [SerializeField] TMP_Text towerCriticalDamageValue;

        GameCompositionRoot _root;
        PopupManager _popup;
        bool _visible;
        bool _lockOverlayShown;

        readonly List<string> _tagNames = new List<string>(8);

        void Awake()
        {
            if (tagSubParentTemplate != null)
                tagSubParentTemplate.gameObject.SetActive(false);
        }

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
                scopeThisButton.onClick.AddListener(() =>
                {
                    UiSfx.Click();
                    _root?.SetApplyScope(TargetingApplyScope.ThisTower);
                });
            if (scopeTypeButton != null)
                scopeTypeButton.onClick.AddListener(() =>
                {
                    UiSfx.Click();
                    _root?.SetApplyScope(TargetingApplyScope.ThisType);
                });
            if (scopeAllButton != null)
                scopeAllButton.onClick.AddListener(ConfirmAllThenSet);

            Refresh();
        }

        void Update()
        {
            if (!_visible || _root == null)
                return;
            var lockLeft = _root.SelectedSocketLockRemaining;
            if (lockLeft > 0f || _lockOverlayShown)
                RefreshSocketLockOverlays();
            _lockOverlayShown = lockLeft > 0f;
        }

        void OnHudDirty() => Refresh();

        void Refresh()
        {
            if (_root == null) return;

            _visible = _root.HasSelectedTower && _root.States != null
                       && _root.States.Current != RunStateId.Draft
                       && _root.States.Current != RunStateId.Defeat
                       && _root.States.Current != RunStateId.VictorySummary;

            panel.SetActive(_visible);
            if (!_visible) return;

            RefreshTowerInfo();

            var planOrCombat = _root.States != null
                               && (_root.States.Current == RunStateId.Plan
                                   || _root.States.Current == RunStateId.Combat);
            if (sellButton != null)
                sellButton.gameObject.SetActive(planOrCombat);

            var tower = _root.Placement?.Selected;
            SetTags(tower != null && tower.Def != null
                ? GemTags.EffectiveTowerTags(tower.Def)
                : GemTag.None);
            if (sellLabel != null && planOrCombat && tower != null)
                sellLabel.text = $"Sell · {RunEconomy.ComputeSellRefund(tower.PurchaseCost, tower.UpgradeSpend)}g";

            var socketCount = tower?.Def != null ? tower.Def.SocketCount : 0;
            for (var i = 0; i < socketSlots.Length; i++)
            {
                if (socketSlots[i] == null) continue;
                var showSlot = tower != null && i < socketCount;
                socketSlots[i].gameObject.SetActive(showSlot);
                if (!showSlot) continue;
                var gem = tower.Sockets != null && i < tower.Sockets.Length ? tower.Sockets[i] : default;
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

        void RefreshTowerInfo()
        {
            var tower = _root?.Placement?.Selected;
            if (tower == null || tower.Def == null || _root == null)
                return;

            var spec = _root.ResolveLiveSpec(tower);
            var interval = tower.Def.FireInterval(spec, tower.Level);
            var attackRate = interval > 0.01f ? 1f / interval : 0f;

            SetInfo(towerNameText, tower.Def.DisplayName);
            SetInfo(towerLevelText, $"Lv. {tower.LevelIndex + 1}");
            SetInfo(towerDamageValueText, FormatDamage(spec));
            SetInfo(towerAttackSpeedValueText, attackRate.ToString("0.##"));
            SetInfo(towerAttackRangeValueText, _root.GetEffectiveAttackRange(tower).ToString("0.#"));
            SetInfo(towerCriticalChanceValue, FormatPercent(spec.CritChance));
            var critMultiplier = spec.CritMultiplier > 0f ? spec.CritMultiplier : 1.5f;
            SetInfo(towerCriticalDamageValue, $"{critMultiplier:0.##}x");
        }

        static string FormatDamage(SkillSpec spec)
        {
            var text = spec.DamageMax > spec.DamageMin + 0.01f
                ? $"{spec.DamageMin:0.#}–{spec.DamageMax:0.#}"
                : spec.Damage.ToString("0.#");
            if (spec.ProjectileCount > 1)
                text = $"{text} ×{spec.ProjectileCount}";
            return text;
        }

        static string FormatPercent(float fraction)
        {
            var percent = fraction * 100f;
            if (Mathf.Abs(percent - Mathf.Round(percent)) < 0.05f)
                return $"{Mathf.RoundToInt(percent)}%";
            return $"{percent:0.#}%";
        }

        void SetInfo(TMP_Text label, string value)
        {
            if (label == null)
            {
                Debug.LogError("TowerDetailsController: assign Tower Info text on the prefab.", this);
                return;
            }

            label.text = value;
        }

        void SetTags(GemTag tags)
        {
            if (tagsPanel == null)
            {
                Debug.LogError("TowerDetailsController: assign tagsPanel on the prefab.", this);
                return;
            }

            if (tagSubParentTemplate == null)
            {
                Debug.LogError("TowerDetailsController: assign tagSubParentTemplate on the prefab.", this);
                return;
            }

            tagSubParentTemplate.gameObject.SetActive(false);
            GemTags.CollectNames(tags, _tagNames);

            for (var i = 0; i < _tagNames.Count; i++)
            {
                var label = GetOrCreateTag(i);
                if (label == null)
                    continue;
                if (label.transform.parent != tagsPanel.transform)
                    label.transform.SetParent(tagsPanel.transform, false);
                label.transform.SetSiblingIndex(i);
                label.gameObject.SetActive(true);
                label.Bind(_tagNames[i]);
            }

            for (var i = _tagNames.Count; i < tagLabels.Count; i++)
            {
                if (tagLabels[i] != null)
                    tagLabels[i].gameObject.SetActive(false);
            }

            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)tagsPanel.transform);
        }

        TagLabel GetOrCreateTag(int index)
        {
            while (tagLabels.Count <= index)
            {
                if (tagLabelPrefab == null)
                {
                    Debug.LogError("TowerDetailsController: assign tagLabelPrefab (DraftTagLabel) on the prefab.", this);
                    return null;
                }

                var label = Instantiate(tagLabelPrefab, tagsPanel.transform);
                label.gameObject.SetActive(false);
                tagLabels.Add(label);
            }

            return tagLabels[index];
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
            UiSfx.Click();
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
            UiSfx.Click();
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

            var tower = _root.Placement.Selected;
            var refund = RunEconomy.ComputeSellRefund(tower.PurchaseCost, tower.UpgradeSpend);
            var body = _root.SelectedHasSocketedGems
                ? $"You get {refund}g back. Gems in this tower return to your inventory."
                : $"You get {refund}g back.";
            _popup.ShowConfirmOnceSuppressed(
                id: "SellConfirm",
                title: "Sell tower?",
                body: body,
                onConfirm: () => _root.RequestSellSelected(),
                pauseForFairness: false,
                yesText: "Yes", noText: "No");
        }
    }
}
