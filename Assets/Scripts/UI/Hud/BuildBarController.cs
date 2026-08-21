using UnityEngine;
using UnityEngine.UI;
using GemTD.Core;
using GemTD.Gameplay;
using GemTD.Gameplay.Run;
using System.Collections.Generic;

namespace GemTD.UI
{
    /// <summary>Lives on BuildBar prefab. Build-tower buttons for Plan + Combat placement.</summary>
    public sealed class BuildBarController : MonoBehaviour
    {
        [SerializeField] GameObject panel;
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
            if (buildButtons == null || buildButtons.Count == 0)
            {
                Debug.LogError("BuildBarController: assign BuildTowerButton refs on the prefab.", this);
                return;
            }

            for (var i = 0; i < buildButtons.Count; i++)
            {
                var idx = i;
                if (buildButtons[i] != null)
                    buildButtons[i].GetButton().onClick.AddListener(() => _root?.SetPlaceTower(idx));
            }

            _buttonsBound = true;
            Refresh();
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
            var towerCount = _root.BuildBarTowerCount;
            for (var i = 0; i < buildButtons.Count; i++)
            {
                if (buildButtons[i] == null) continue;
                var show = i < towerCount;
                buildButtons[i].gameObject.SetActive(show);
                if (!show) continue;
                buildButtons[i].UpdateTowerButton(_root.GetPlaceTowerName(i), _root.GetPlaceTowerCost(i));
                var btn = buildButtons[i].GetButton();
                if (btn != null)
                    btn.interactable = gold >= _root.GetPlaceTowerCost(i);
            }
        }
    }
}
