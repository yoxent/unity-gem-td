using UnityEngine;
using GemTD.Core;
using GemTD.Gameplay;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;
using System.Collections.Generic;
using TMPro;

namespace GemTD.UI
{
    /// <summary>Lives on BuildBar prefab. Pooled build-tower buttons for Plan + Combat placement.</summary>
    public sealed class BuildBarController : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text towerCountLabel;
        [SerializeField] TMP_Text[] categoryLabels;
        [SerializeField] List<BuildTowerButton> buildButtons = new List<BuildTowerButton>();
        [SerializeField] BuildTowerTooltip buildTowerTooltip;

        static readonly TowerRosterCategory[] CategoryOrder =
        {
            TowerRosterCategory.Damaging,
            TowerRosterCategory.Curse,
            TowerRosterCategory.Aura
        };

        GameCompositionRoot _root;
        bool _buttonsBound;

        void OnEnable()
        {
            GameEvents.RunStateChanged += Refresh;
            GameEvents.GoldChanged += OnGoldChanged;
            GameEvents.PlaceModeChanged += Refresh;
            GameEvents.TowerRosterChanged += Refresh;
        }

        void OnDisable()
        {
            GameEvents.RunStateChanged -= Refresh;
            GameEvents.GoldChanged -= OnGoldChanged;
            GameEvents.PlaceModeChanged -= Refresh;
            GameEvents.TowerRosterChanged -= Refresh;
        }

        public void Bind(GameCompositionRoot root)
        {
            _root = root;
            if (buildButtons == null)
                buildButtons = new List<BuildTowerButton>();
            _buttonsBound = true;
            Refresh();
        }

        void WireClick(BuildTowerButton button, int index)
        {
            var btn = button.GetButton();
            if (btn == null)
                return;
            btn.onClick.RemoveAllListeners();
            var idx = index;
            btn.onClick.AddListener(() =>
            {
                UiSfx.Click();
                _root?.SetPlaceTower(idx);
            });
        }

        void OnGoldChanged(int _) => Refresh();

        void Refresh()
        {
            if (!_buttonsBound || _root == null) return;
            if (panel == null)
            {
                Debug.LogError("BuildBarController: assign Panel on the prefab.", this);
                return;
            }

            var state = _root.States != null ? _root.States.Current : RunStateId.Boot;
            var showBar = state == RunStateId.Plan || state == RunStateId.Combat;
            panel.SetActive(showBar);
            if (!showBar) return;

            var gold = _root.Economy != null ? _root.Economy.Gold : 0;
            var filledCount = _root.BuildBarTowerCount;
            var maxSlots = _root.Draft != null && _root.Draft.Roster != null
                ? _root.Draft.Roster.MaxSlots
                : 0;
            if (towerCountLabel != null)
                towerCountLabel.text = $"{filledCount}/{maxSlots}";
            if (maxSlots <= 0)
            {
                for (var i = 0; i < buildButtons.Count; i++)
                {
                    if (buildButtons[i] != null)
                        buildButtons[i].gameObject.SetActive(false);
                }

                return;
            }

            ApplyCategoryLabels();

            var roster = _root.Draft.Roster;
            var damageCap = roster.CountIn(TowerRosterCategory.Damaging) + roster.Remaining(TowerRosterCategory.Damaging);
            var curseCap = roster.CountIn(TowerRosterCategory.Curse) + roster.Remaining(TowerRosterCategory.Curse);
            var auraCap = roster.CountIn(TowerRosterCategory.Aura) + roster.Remaining(TowerRosterCategory.Aura);
            if (buildButtons.Count < damageCap + curseCap + auraCap)
                Debug.LogError("BuildBarController: pre-place one empty slot per roster cap, in Damage, Curse, Aura order.", this);

            for (var i = 0; i < buildButtons.Count; i++)
            {
                if (buildButtons[i] == null)
                    continue;
                var show = i < maxSlots;
                buildButtons[i].SetTooltip(buildTowerTooltip);
                buildButtons[i].gameObject.SetActive(show);
                if (!show)
                    continue;
                buildButtons[i].BindEmpty();
                var emptyButton = buildButtons[i].GetButton();
                if (emptyButton != null)
                    emptyButton.onClick.RemoveAllListeners();
            }

            var towers = _root.GetBuildBarTowers();
            var filledInCategory = new int[CategoryOrder.Length];
            for (var i = 0; i < filledCount && i < towers.Length; i++)
            {
                var category = TowerRosterCategoryRules.Of(towers[i]);
                var categoryIndex = CategoryIndex(category);
                var slot = SlotIndex(category, filledInCategory[categoryIndex], damageCap, curseCap);
                filledInCategory[categoryIndex]++;
                if (slot < 0 || slot >= buildButtons.Count || buildButtons[slot] == null)
                    continue;

                var button = buildButtons[slot];
                button.gameObject.SetActive(true);
                var tower = towers[i];
                var level = _root.GetPlaceTowerLevel(i);
                var cost = _root.GetPlaceTowerCost(i);
                button.UpdateTowerButton(
                    _root.GetPlaceTowerName(i),
                    cost,
                    level);
                if (tower != null)
                    button.SetTooltipData(
                        BuildTowerTooltipData.From(tower, level, button.GetTowerIconSprite()));
                WireClick(button, i);
                var click = button.GetButton();
                if (click != null)
                    click.interactable = gold >= cost;
            }
        }

        void ApplyCategoryLabels()
        {
            if (categoryLabels == null)
                return;

            for (var i = 0; i < categoryLabels.Length && i < CategoryOrder.Length; i++)
            {
                if (categoryLabels[i] == null)
                    continue;
                categoryLabels[i].text = CategoryLabel(CategoryOrder[i]);
                categoryLabels[i].gameObject.SetActive(true);
            }
        }

        static int CategoryIndex(TowerRosterCategory category)
        {
            for (var i = 0; i < CategoryOrder.Length; i++)
            {
                if (CategoryOrder[i] == category)
                    return i;
            }

            return 0;
        }

        static int SlotIndex(TowerRosterCategory category, int indexInCategory, int damageCap, int curseCap)
        {
            switch (category)
            {
                case TowerRosterCategory.Curse:
                    return damageCap + indexInCategory;
                case TowerRosterCategory.Aura:
                    return damageCap + curseCap + indexInCategory;
                default:
                    return indexInCategory;
            }
        }

        static string CategoryLabel(TowerRosterCategory category)
        {
            switch (category)
            {
                case TowerRosterCategory.Curse:
                    return "Curse";
                case TowerRosterCategory.Aura:
                    return "Aura";
                default:
                    return "Damage";
            }
        }
    }
}
