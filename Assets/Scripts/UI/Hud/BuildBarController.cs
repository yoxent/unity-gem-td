using UnityEngine;
using UnityEngine.UI;
using GemTD.Core;
using GemTD.Gameplay;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;
using System.Collections.Generic;

namespace GemTD.UI
{
    /// <summary>Lives on BuildBar prefab. Pooled build-tower buttons for Plan + Combat placement.</summary>
    public sealed class BuildBarController : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Transform buildButtonsParent;
        [SerializeField] BuildTowerButton buttonPrefab;
        [SerializeField] List<BuildTowerButton> buildButtons = new List<BuildTowerButton>();

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
            BindExistingButtons();
            _buttonsBound = true;
            Refresh();
        }

        void BindExistingButtons()
        {
            if (buildButtons == null)
                buildButtons = new List<BuildTowerButton>();

            for (var i = 0; i < buildButtons.Count; i++)
            {
                if (buildButtons[i] == null)
                    continue;
                WireClick(buildButtons[i], i);
            }
        }

        void WireClick(BuildTowerButton button, int index)
        {
            var btn = button.GetButton();
            if (btn == null)
                return;
            btn.onClick.RemoveAllListeners();
            var idx = index;
            btn.onClick.AddListener(() => _root?.SetPlaceTower(idx));
        }

        void EnsureButtonCount(int needed)
        {
            if (needed <= 0)
                return;

            while (buildButtons.Count < needed)
            {
                BuildTowerButton extra = null;
                var parent = buildButtonsParent != null
                    ? buildButtonsParent
                    : (buildButtons.Count > 0 && buildButtons[0] != null
                        ? buildButtons[0].transform.parent
                        : transform);

                if (buttonPrefab != null)
                    extra = Instantiate(buttonPrefab, parent);
                else if (buildButtons.Count > 0 && buildButtons[0] != null)
                    extra = Instantiate(buildButtons[0], parent);

                if (extra == null)
                {
                    Debug.LogError("BuildBarController: assign buttonPrefab (or seed BuildTowerButton refs) on the prefab.", this);
                    return;
                }

                extra.name = "BuildTowerButton_" + buildButtons.Count;
                var idx = buildButtons.Count;
                WireClick(extra, idx);
                buildButtons.Add(extra);
            }
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
            if (maxSlots <= 0)
            {
                for (var i = 0; i < buildButtons.Count; i++)
                {
                    if (buildButtons[i] != null)
                        buildButtons[i].gameObject.SetActive(false);
                }
                return;
            }

            EnsureButtonCount(maxSlots);

            for (var i = 0; i < buildButtons.Count; i++)
            {
                if (buildButtons[i] == null) continue;
                var show = i < maxSlots;
                buildButtons[i].gameObject.SetActive(show);
                if (!show) continue;

                if (i < filledCount)
                {
                    buildButtons[i].UpdateTowerButton(_root.GetPlaceTowerName(i), _root.GetPlaceTowerCost(i));
                    var btn = buildButtons[i].GetButton();
                    if (btn != null)
                        btn.interactable = gold >= _root.GetPlaceTowerCost(i);
                }
                else
                    buildButtons[i].BindEmpty();
            }
        }
    }
}
