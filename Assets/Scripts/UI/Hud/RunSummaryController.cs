using System.Collections.Generic;
using System.Globalization;
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
        [SerializeField] LayoutElement towerSectionsLayoutElement;
        [SerializeField] Transform towerSectionsContent;
        [SerializeField] RunSummarySection towerSummarySectionPrefab;
        [SerializeField] Button endlessButton;
        [SerializeField] Button mainMenuButton;

        const float MaxTowerSectionsHeight = 380f;

        readonly List<RunSummarySection> _sectionPool = new List<RunSummarySection>(4);

        GameCompositionRoot _root;
        bool _towerSectionsHeightCapped;

        void Awake()
        {
            if (panel == null)
                Debug.LogError("RunSummaryController: panel is not assigned.", this);
            if (towerSectionsContent == null)
                Debug.LogError("RunSummaryController: towerSectionsParent is not assigned.", this);
            if (towerSummarySectionPrefab == null)
                Debug.LogError("RunSummaryController: towerSummarySectionPrefab is not assigned.", this);
            if (mainMenuButton == null)
                Debug.LogError("RunSummaryController: mainMenuButton is not assigned.", this);
            else
                mainMenuButton.onClick.AddListener(() =>
                {
                    UiSfx.Click();
                    LoadMainMenu();
                });

            if (endlessButton != null)
                endlessButton.onClick.AddListener(() =>
                {
                    UiSfx.Click();
                    OnEndlessClicked();
                });
            else
                Debug.LogWarning("RunSummaryController: endlessButton is not assigned (Victory Endless CTA).", this);

            if (panel != null)
                panel.SetActive(false);

            ResetTowerSectionsHeight();
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
            if (endlessButton != null)
                endlessButton.gameObject.SetActive(victory);

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
                totalDamageText.text = FormatCount(Mathf.RoundToInt(snapshot.TotalDamage));
            if (totalKillsText != null)
                totalKillsText.text = FormatCount(snapshot.TotalKills);
            if (totalGoldText != null)
                totalGoldText.text = FormatCount(snapshot.TotalGoldEarned);
            if (totalBuiltText != null)
                totalBuiltText.text = FormatCount(snapshot.TotalBuilt);

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

            var widestValue = 0f;
            for (var i = 0; i < entries.Length; i++)
            {
                var width = _sectionPool[i].WidestSummaryValueWidth;
                if (width > widestValue)
                    widestValue = width;
            }

            for (var i = 0; i < entries.Length; i++)
            {
                _sectionPool[i].SetSummaryValueWidth(widestValue);
                IncludeSectionHeight(_sectionPool[i]);
            }
        }

        void ClearSections()
        {
            for (var i = 0; i < _sectionPool.Count; i++)
            {
                if (_sectionPool[i] != null)
                    _sectionPool[i].gameObject.SetActive(false);
            }

            ResetTowerSectionsHeight();
        }

        void ResetTowerSectionsHeight()
        {
            _towerSectionsHeightCapped = false;
            if (towerSectionsLayoutElement != null)
                towerSectionsLayoutElement.preferredHeight = 0f;
        }

        void IncludeSectionHeight(RunSummarySection section)
        {
            if (_towerSectionsHeightCapped || towerSectionsLayoutElement == null || section == null)
                return;

            var height = towerSectionsLayoutElement.preferredHeight;
            if (height > 0f && towerSectionsContent != null)
            {
                var group = towerSectionsContent.GetComponent<VerticalLayoutGroup>();
                if (group != null)
                    height += group.spacing;
            }

            height += section.PreferredHeight;
            if (height >= MaxTowerSectionsHeight)
            {
                towerSectionsLayoutElement.preferredHeight = MaxTowerSectionsHeight;
                _towerSectionsHeightCapped = true;
                return;
            }

            towerSectionsLayoutElement.preferredHeight = height;
        }

        RunSummarySection GetOrCreateSection(int index)
        {
            while (_sectionPool.Count <= index)
            {
                var section = Instantiate(towerSummarySectionPrefab, towerSectionsContent);
                section.gameObject.SetActive(false);
                _sectionPool.Add(section);
            }

            return _sectionPool[index];
        }

        void OnEndlessClicked()
        {
            if (_root == null)
                return;
            _root.BeginEndless();
        }

        static string FormatCount(int value) =>
            value.ToString("N0", CultureInfo.InvariantCulture);

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
