using System.Collections.Generic;
using GemTD.Gameplay.Run;
using TMPro;
using UnityEngine;

namespace GemTD.UI
{
    public sealed class RunSummarySection : MonoBehaviour
    {
        const int StatRowCount = 3;

        [SerializeField] TMP_Text towerName;
        [SerializeField] Transform summaryElementsParent;
        [SerializeField] RunSummaryElement summaryElementPrefab;

        readonly List<RunSummaryElement> _elementPool = new List<RunSummaryElement>(StatRowCount);

        void Awake()
        {
            if (towerName == null)
                Debug.LogError("RunSummarySection: towerName is not assigned.", this);
            if (summaryElementsParent == null)
                Debug.LogError("RunSummarySection: summaryElementsParent is not assigned.", this);
            if (summaryElementPrefab == null)
                Debug.LogError("RunSummarySection: summaryElementPrefab is not assigned.", this);
        }

        public void Bind(string displayName, Color towerColor, RunStatsTowerEntry entry)
        {
            if (towerName != null)
            {
                towerName.text = displayName;
                towerName.color = towerColor;
            }

            BindElement(0, "Damage", entry.Damage, entry.DamagePercent, towerColor);
            BindElement(1, "Kills", entry.Kills, entry.KillPercent, towerColor);
            BindElement(2, "Built", entry.Built, entry.BuiltPercent, towerColor);

            for (var i = StatRowCount; i < _elementPool.Count; i++)
                _elementPool[i].gameObject.SetActive(false);
        }

        void BindElement(int index, string label, float value, float percent, Color barColor)
        {
            var element = GetOrCreateElement(index);
            element.gameObject.SetActive(true);
            element.Bind(label, value, percent, barColor);
        }

        RunSummaryElement GetOrCreateElement(int index)
        {
            while (_elementPool.Count <= index)
            {
                var element = Instantiate(summaryElementPrefab, summaryElementsParent);
                element.gameObject.SetActive(false);
                _elementPool.Add(element);
            }

            return _elementPool[index];
        }
    }
}
