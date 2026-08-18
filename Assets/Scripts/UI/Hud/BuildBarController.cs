using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GemTD.Gameplay;
using GemTD.Gameplay.Run;
using System.Collections.Generic;

namespace GemTD.UI
{
    /// <summary>Lives on BuildBar prefab. 3 BuildTowerButton children. Mouse-only placement.</summary>
    public sealed class BuildBarController : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Transform buildButtonsParent;
        [SerializeField] List<BuildTowerButton> buildButtons = new List<BuildTowerButton>();

        GameCompositionRoot _root;
        bool _buttonsBound = false;

        void Awake()
        {
            if (panel == null)
                Debug.LogError("BuildBarController: assign Panel on the prefab.", this);
            if (buildButtons == null || buildButtons.Count == 0)
                Debug.LogError("BuildBarController: assign BuildTowerButton refs on the prefab.", this);
            else
            {
                for (var i = 0; i < buildButtons.Count; i++)
                {
                    var idx = i;
                    if (buildButtons[i] != null)
                        buildButtons[i].GetButton().onClick.AddListener(() => _root?.SetPlaceTower(idx));
                }
            }

            _buttonsBound = buildButtons != null && buildButtons.Count > 0;
        }

        void Update()
        {
            if (!_buttonsBound) return;

            if (_root == null) _root = GameCompositionRoot.Instance;
            if (_root == null) return;

            // Set tower name labels once root is available.
            for (var i = 0; i < buildButtons.Count; i++)
            {
                if (buildButtons[i] != null)
                    buildButtons[i].UpdateLabel($"{_root.GetPlaceTowerName(i)}");
            }

            var plan = _root.States != null && _root.States.Current == RunStateId.Plan;
            panel.SetActive(plan);
            if (!plan) return;
            for (var i = 0; i < buildButtons.Count; i++)
            {
                if (buildButtons[i] == null) continue;
                var btn = buildButtons[i].GetButton();
                if (btn != null) btn.interactable = plan;
            }
        }
    }
}