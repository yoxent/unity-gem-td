using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GemTD.Gameplay;
using GemTD.Gameplay.Combat;

namespace GemTD.UI
{
    /// <summary>One Tower Details priority row — left/right cycle buttons for P1–P3.</summary>
    public sealed class TowerTargetPriority : MonoBehaviour
    {
        [SerializeField] Button cycleLeft;
        [SerializeField] Button cycleRight;
        [SerializeField] TMP_Text priorityLabel;

        GameCompositionRoot _root;
        int _slot = -1;

        void Awake()
        {
            if (cycleLeft == null || cycleRight == null)
            {
                Debug.LogError("TowerTargetPriority: assign cycleLeft and cycleRight on the prefab.", this);
                return;
            }

            if (priorityLabel == null)
                Debug.LogError("TowerTargetPriority: assign priorityLabel on the prefab.", this);

            cycleLeft.onClick.AddListener(OnCycleLeft);
            cycleRight.onClick.AddListener(OnCycleRight);
        }

        public void Bind(GameCompositionRoot root, int slot)
        {
            _root = root;
            _slot = slot;
        }

        public void Refresh(TargetingKey key)
        {
            if (priorityLabel != null)
                priorityLabel.text = TargetingKeyLabels.For(key);
        }

        void OnCycleLeft()
        {
            if (_root == null || _slot < 0)
                return;
            _root.CyclePriority(_slot, -1);
        }

        void OnCycleRight()
        {
            if (_root == null || _slot < 0)
                return;
            _root.CyclePriority(_slot, 1);
        }
    }
}