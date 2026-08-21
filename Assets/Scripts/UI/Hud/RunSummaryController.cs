using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using GemTD.Core;
using GemTD.Gameplay;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;

namespace GemTD.UI
{
    /// <summary>Run Summary panel shown on VictorySummary and Defeat.</summary>
    public sealed class RunSummaryController : MonoBehaviour
    {
        static readonly Color[] TowerBarColors =
        {
            new Color(0.35f, 0.65f, 0.95f),
            new Color(0.95f, 0.55f, 0.25f),
            new Color(0.55f, 0.85f, 0.45f),
        };

        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text outcomeText;
        [SerializeField] TMP_Text waveText;
        [SerializeField] TMP_Text totalDamageText;
        [SerializeField] TMP_Text totalKillsText;
        [SerializeField] TMP_Text totalGoldText;
        [SerializeField] TMP_Text totalBuiltText;
        [SerializeField] TMP_Text skillsText;
        [SerializeField] Transform towerSectionsParent;
        [SerializeField] RunSummarySection towerSummarySectionPrefab;
        [SerializeField] Button mainMenuButton;

        readonly List<RunSummarySection> _sectionPool = new List<RunSummarySection>(4);

        GameCompositionRoot _root;

        void Awake()
        {
            if (panel == null)
                Debug.LogError("RunSummaryController: panel is not assigned.", this);
            if (towerSectionsParent == null)
                Debug.LogError("RunSummaryController: towerSectionsParent is not assigned.", this);
            if (towerSummarySectionPrefab == null)
                Debug.LogError("RunSummaryController: towerSummarySectionPrefab is not assigned.", this);
            if (mainMenuButton == null)
                Debug.LogError("RunSummaryController: mainMenuButton is not assigned.", this);
            else
                mainMenuButton.onClick.AddListener(LoadMainMenu);

            if (panel != null)
                panel.SetActive(false);
        }

        void OnEnable() => GameEvents.RunStateChanged += Refresh;
        void OnDisable() => GameEvents.RunStateChanged -= Refresh;

        public void Bind(GameCompositionRoot root)
        {
            _root = root;
            Refresh();
        }

        void Refresh()
        {
            if (_root == null || _root.States == null)
                return;

            var state = _root.States.Current;
            var show = state == RunStateId.VictorySummary || state == RunStateId.Defeat;
            if (panel != null)
                panel.SetActive(show);

            if (!show)
            {
                ClearSections();
                return;
            }

            var victory = state == RunStateId.VictorySummary;
            var snapshot = _root.RunStats.Snapshot(_root.CurrentWaveNumber, _root.GetBuildBarTowers());
            ApplySnapshot(snapshot, victory);
        }

        void ApplySnapshot(RunStatsSnapshot snapshot, bool victory)
        {
            if (outcomeText != null)
                outcomeText.text = victory ? "Victory" : "Defeat";
            if (waveText != null)
                waveText.text = $"Wave {snapshot.WaveReached}";
            if (totalDamageText != null)
                totalDamageText.text = $"Total damage: {Mathf.RoundToInt(snapshot.TotalDamage)}";
            if (totalKillsText != null)
                totalKillsText.text = $"Total kills: {snapshot.TotalKills}";
            if (totalGoldText != null)
                totalGoldText.text = $"Gold earned: {snapshot.TotalGoldEarned}";
            if (totalBuiltText != null)
                totalBuiltText.text = $"Towers built: {snapshot.TotalBuilt}";
            if (skillsText != null)
                skillsText.text = $"Skills: {snapshot.SkillsCount}";

            ClearSections();

            var entries = snapshot.TowersByType;
            for (var i = 0; i < entries.Length; i++)
            {
                var section = GetOrCreateSection(i);
                section.gameObject.SetActive(true);
                section.Bind(
                    GetTowerDisplayName(entries[i].Tower),
                    GetTowerColor(i),
                    entries[i]);
            }
        }

        void ClearSections()
        {
            for (var i = 0; i < _sectionPool.Count; i++)
            {
                if (_sectionPool[i] != null)
                    _sectionPool[i].gameObject.SetActive(false);
            }
        }

        RunSummarySection GetOrCreateSection(int index)
        {
            while (_sectionPool.Count <= index)
            {
                var section = Instantiate(towerSummarySectionPrefab, towerSectionsParent);
                section.gameObject.SetActive(false);
                _sectionPool.Add(section);
            }

            return _sectionPool[index];
        }

        static string GetTowerDisplayName(TowerDefinition tower)
        {
            if (tower == null)
                return "?";
            return !string.IsNullOrEmpty(tower.DisplayName) ? tower.DisplayName : tower.name;
        }

        static Color GetTowerColor(int index) =>
            index >= 0 && index < TowerBarColors.Length ? TowerBarColors[index] : TowerBarColors[0];

        static void LoadMainMenu() => SceneManager.LoadScene(SceneNames.MainMenu);
    }
}
