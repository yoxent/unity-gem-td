using TMPro;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Towers;
using UnityEngine.UI;

namespace GemTD.UI
{
    public readonly struct BuildTowerTooltipData
    {
        public readonly string DisplayName;
        public readonly Sprite TowerIcon;
        public readonly int Level;
        public readonly string Damage;
        public readonly string ActionSpeed;
        public readonly string Range;
        public readonly string CriticalChance;
        public readonly string CriticalDamage;

        public BuildTowerTooltipData(
            string displayName,
            Sprite towerIcon,
            int level,
            string damage,
            string actionSpeed,
            string range,
            string criticalChance,
            string criticalDamage)
        {
            DisplayName = displayName;
            TowerIcon = towerIcon;
            Level = level;
            Damage = damage;
            ActionSpeed = actionSpeed;
            Range = range;
            CriticalChance = criticalChance;
            CriticalDamage = criticalDamage;
        }

        public static BuildTowerTooltipData From(
            TowerDefinition definition,
            int level,
            Sprite towerIcon)
        {
            if (definition == null)
                return default;

            var sourceLevel = level > 0 ? level : 1;
            var damage = definition.GetDamageRange(sourceLevel);
            var projectileCount = definition.GetProjectileCount(sourceLevel);
            var spec = SkillSpec.FromBase(
                damage.Min,
                damage.Max,
                Mathf.Max(1, projectileCount),
                definition.GetSplashRadius(sourceLevel),
                definition.GetChainCount(sourceLevel),
                definition.GetForkCount(sourceLevel));
            var actionsPerSecond = definition.GetBaseActionsPerSecond(sourceLevel);
            var towerName = !string.IsNullOrEmpty(definition.DisplayName)
                ? definition.DisplayName
                : definition.name;

            return new BuildTowerTooltipData(
                towerName,
                towerIcon,
                sourceLevel,
                FormatDamage(damage.Min, damage.Max, projectileCount),
                actionsPerSecond > 0.01f ? $"{actionsPerSecond:0.##}/s" : "—",
                FormatValue(definition.GetPlacementTowerRadius(sourceLevel)),
                FormatPercent(spec.CritChance),
                $"{spec.CritMultiplier:0.##}x");
        }

        static string FormatDamage(float min, float max, int projectileCount)
        {
            var damageText = Mathf.Abs(max - min) > 0.01f
                ? $"{min:0.#}–{max:0.#}"
                : min.ToString("0.#");
            return projectileCount > 1
                ? $"{damageText} ×{projectileCount}"
                : damageText;
        }

        static string FormatValue(float value)
        {
            return value > 0.01f ? value.ToString("0.#") : "—";
        }

        static string FormatPercent(float fraction)
        {
            var percent = fraction * 100f;
            if (Mathf.Abs(percent - Mathf.Round(percent)) < 0.05f)
                return $"{Mathf.RoundToInt(percent)}%";
            return $"{percent:0.#}%";
        }
    }

    /// <summary>
    /// Fixed-position build-bar tooltip. The scene owns the position; only the
    /// inspector-assigned child information panel is shown or hidden.
    /// </summary>
    public sealed class BuildTowerTooltip : MonoBehaviour
    {
        [SerializeField] GameObject infoPanel;
        [SerializeField] Image towerIcon;
        [SerializeField] TMP_Text towerNameText;
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text damageText;
        [SerializeField] TMP_Text actionSpeedText;
        [SerializeField] TMP_Text rangeText;
        [SerializeField] TMP_Text criticalChanceText;
        [SerializeField] TMP_Text criticalDamageText;

        void Awake()
        {
            Hide();
        }

        void OnDisable()
        {
            Hide();
        }

        public void Show(BuildTowerTooltipData data)
        {
            if (towerIcon != null)
            {
                towerIcon.sprite = data.TowerIcon;
                towerIcon.preserveAspect = true;
                towerIcon.gameObject.SetActive(data.TowerIcon != null);
            }

            SetText(towerNameText, data.DisplayName);
            SetText(levelText, $"Lv. {data.Level}");
            SetText(damageText, data.Damage);
            SetText(actionSpeedText, data.ActionSpeed);
            SetText(rangeText, data.Range);
            SetText(criticalChanceText, data.CriticalChance);
            SetText(criticalDamageText, data.CriticalDamage);

            if (infoPanel != null)
                infoPanel.SetActive(true);
        }

        public void Hide()
        {
            if (infoPanel != null)
                infoPanel.SetActive(false);
        }

        static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }
    }
}
