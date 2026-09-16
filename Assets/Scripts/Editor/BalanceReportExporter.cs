using System.Globalization;
using System.IO;
using System.Text;
using GemTD.Gameplay.Balance;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;
using UnityEditor;
using UnityEngine;

namespace GemTD.Editor
{
    public static class BalanceReportExporter
    {
        const string SelectedStarterCatalogPath =
            "Assets/Data/Towers/StarterTowersCatalog_Selected.asset";
        const string StarterCatalogPath = "Assets/Data/Towers/StarterTowersCatalog.asset";
        const string WaveCatalogPath = "Assets/Data/Waves/WaveCatalog.asset";
        const string RunConfigPath = "Assets/Data/RunConfig_Default.asset";
        const string RunnerPath = "Assets/Data/Enemies/Enemy_Runner.asset";
        const string ArmoredPath = "Assets/Data/Enemies/Enemy_Armored.asset";

        [MenuItem("Gem TD/Balance/Export Starter Baseline CSV")]
        public static void ExportStarterBaseline()
        {
            var towers = AssetDatabase.LoadAssetAtPath<TowerCatalog>(SelectedStarterCatalogPath);
            if (towers == null)
                towers = AssetDatabase.LoadAssetAtPath<TowerCatalog>(StarterCatalogPath);

            var waves = AssetDatabase.LoadAssetAtPath<WaveCatalog>(WaveCatalogPath);
            var config = AssetDatabase.LoadAssetAtPath<RunConfig>(RunConfigPath);
            var runner = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(RunnerPath);
            var armored = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(ArmoredPath);

            if (towers == null || waves == null || config == null || runner == null || armored == null)
            {
                EditorUtility.DisplayDialog(
                    "Gem TD Balance Report",
                    "Could not load the starter catalog, wave catalog, run config, Runner, or Armored asset.",
                    "OK");
                return;
            }

            var path = EditorUtility.SaveFilePanel(
                "Export Gem TD balance report",
                Application.dataPath,
                "GemTD_Balance_Starter_Baseline",
                "csv");
            if (string.IsNullOrEmpty(path))
                return;

            var waveDefinitions = waves.GetWavesOrEmpty();
            var enemies = new[] { runner, armored };
            var report = new StringBuilder(8192);
            report.AppendLine(
                "RowType,Tower,Enemy,Wave,Count,SpawnInterval,SpawnDuration,BaseHealth,ScaledHealth,"
                + "RawDamageMin,RawDamageMax,EffectiveDamageMin,EffectiveDamageMax,"
                + "EffectiveActionDamageMin,EffectiveActionDamageMax,FireInterval,Projectiles,"
                + "DeliveryPattern,PayloadCount,PayloadAllHitRawDamageMin,PayloadAllHitRawDamageMax,PayloadAoeRadius,"
                + "BestActionsToKill,WorstActionsToKill,BestTimeToKill,WorstTimeToKill,"
                + "DpsMin,DpsMax,TowerCost,NextTowerCostAtZero,GoldBeforeWave,EndWaveGold,KillGold,LeakDamage");

            AppendTowerRows(report, towers.GetTowersOrEmpty(), enemies);
            AppendWaveRows(report, config, waveDefinitions, towers.GetTowersOrEmpty());

            File.WriteAllText(path, report.ToString(), new UTF8Encoding(false));
            Debug.Log("[Gem TD] Starter balance report exported: " + path);
            EditorUtility.DisplayDialog(
                "Gem TD Balance Report",
                "Exported starter baseline balance data:\n" + path,
                "OK");
        }

        static void AppendTowerRows(
            StringBuilder report,
            TowerDefinition[] towers,
            EnemyDefinition[] enemies)
        {
            if (towers == null || enemies == null)
                return;

            for (var t = 0; t < towers.Length; t++)
            {
                var tower = towers[t];
                if (tower == null || !(tower.FireRole is DamageRoleDefinition))
                    continue;

                var payload = BalanceCalculator.EvaluatePayloads(tower);
                for (var e = 0; e < enemies.Length; e++)
                {
                    var enemy = enemies[e];
                    if (enemy == null)
                        continue;

                    var result = BalanceCalculator.Evaluate(tower, enemy);
                    var dpsMin = result.FireInterval > 0f
                        ? result.EffectiveActionDamageMin / result.FireInterval
                        : 0f;
                    var dpsMax = result.FireInterval > 0f
                        ? result.EffectiveActionDamageMax / result.FireInterval
                        : 0f;

                    AppendRow(
                        report,
                        "TowerEnemy",
                        tower.DisplayName,
                        enemy.DisplayName,
                        0,
                        0,
                        0f,
                        0f,
                        0f,
                        0f,
                        result.RawDamageMin,
                        result.RawDamageMax,
                        result.EffectiveDamageMin,
                        result.EffectiveDamageMax,
                        result.EffectiveActionDamageMin,
                        result.EffectiveActionDamageMax,
                        result.FireInterval,
                        result.ProjectileCount,
                        tower.GetDeliveryPattern().ToString(),
                        payload.PayloadCount,
                        payload.TotalRawDamageMin,
                        payload.TotalRawDamageMax,
                        payload.MaxAoeRadius,
                        result.BestCaseActionsToKill,
                        result.WorstCaseActionsToKill,
                        result.BestCaseTimeToKill,
                        result.WorstCaseTimeToKill,
                        dpsMin,
                        dpsMax,
                        tower.Cost,
                        BalanceCalculator.ComputeNextTowerCost(tower, 0),
                        0,
                        0,
                        0,
                        0);
                }
            }
        }

        static void AppendWaveRows(
            StringBuilder report,
            RunConfig config,
            WaveDefinition[] waves,
            TowerDefinition[] towers)
        {
            if (config == null || waves == null)
                return;

            var modeHpMultiplier = config.GetHpMultiplier();
            for (var i = 0; i < waves.Length; i++)
            {
                var wave = waves[i];
                if (wave == null || wave.WaveNumber <= 0)
                    continue;

                var scale = WaveScaling.HpScale(wave.WaveNumber, modeHpMultiplier);
                var result = BalanceCalculator.EvaluateWave(wave, scale);
                var goldBefore = BalanceCalculator.ComputeGoldBeforeWave(
                    config,
                    waves,
                    wave.WaveNumber);
                var endWaveGold = WaveScaling.ScaleEndWaveGold(
                    config.EndWaveGold,
                    wave.WaveNumber);

                AppendRow(
                    report,
                    "Wave",
                    string.Empty,
                    string.Empty,
                    wave.WaveNumber,
                    result.EnemyCount,
                    result.SpawnInterval,
                    result.SpawnDuration,
                    result.BaseHealth,
                    result.ScaledHealth,
                    0f,
                    0f,
                    0f,
                    0f,
                    0f,
                    0f,
                    0f,
                    0,
                    string.Empty,
                    0,
                    0f,
                    0f,
                    0f,
                    0,
                    0,
                    -1f,
                    -1f,
                    0f,
                    0f,
                    0,
                    0,
                    goldBefore,
                    endWaveGold,
                    result.TotalKillGold,
                    result.TotalLeakDamage);

                AppendTowerWaveRows(
                    report,
                    towers,
                    wave,
                    scale,
                    result.SpawnDuration,
                    goldBefore,
                    endWaveGold);
            }
        }

        static void AppendTowerWaveRows(
            StringBuilder report,
            TowerDefinition[] towers,
            WaveDefinition wave,
            float healthScale,
            float spawnDuration,
            int goldBeforeWave,
            int endWaveGold)
        {
            if (towers == null || wave == null || wave.Entries == null)
                return;

            for (var e = 0; e < wave.Entries.Length; e++)
            {
                var entry = wave.Entries[e];
                var enemy = entry.Enemy;
                if (enemy == null || entry.Count <= 0)
                    continue;

                var perEnemyKillGold = enemy.IsBoss
                    ? WaveScaling.ScaleBossBounty(enemy.KillGold, wave.WaveNumber)
                    : enemy.KillGold;
                var entryKillGold = perEnemyKillGold * entry.Count;
                var entryLeakDamage = enemy.LeakDamage * entry.Count;

                for (var t = 0; t < towers.Length; t++)
                {
                    var tower = towers[t];
                    if (tower == null || !(tower.FireRole is DamageRoleDefinition))
                        continue;

                    var result = BalanceCalculator.Evaluate(
                        tower,
                        enemy,
                        TowerInstance.DefaultLevel,
                        healthScale);
                    var payload = BalanceCalculator.EvaluatePayloads(tower);
                    var dpsMin = result.FireInterval > 0f
                        ? result.EffectiveActionDamageMin / result.FireInterval
                        : 0f;
                    var dpsMax = result.FireInterval > 0f
                        ? result.EffectiveActionDamageMax / result.FireInterval
                        : 0f;

                    AppendRow(
                        report,
                        "TowerWaveEnemy",
                        tower.DisplayName,
                        enemy.DisplayName,
                        wave.WaveNumber,
                        entry.Count,
                        wave.SpawnInterval,
                        spawnDuration,
                        enemy.MaxHealth,
                        result.ScaledHealth,
                        result.RawDamageMin,
                        result.RawDamageMax,
                        result.EffectiveDamageMin,
                        result.EffectiveDamageMax,
                        result.EffectiveActionDamageMin,
                        result.EffectiveActionDamageMax,
                        result.FireInterval,
                        result.ProjectileCount,
                        tower.GetDeliveryPattern().ToString(),
                        payload.PayloadCount,
                        payload.TotalRawDamageMin,
                        payload.TotalRawDamageMax,
                        payload.MaxAoeRadius,
                        result.BestCaseActionsToKill,
                        result.WorstCaseActionsToKill,
                        result.BestCaseTimeToKill,
                        result.WorstCaseTimeToKill,
                        dpsMin,
                        dpsMax,
                        tower.Cost,
                        BalanceCalculator.ComputeNextTowerCost(tower, 0),
                        goldBeforeWave,
                        endWaveGold,
                        entryKillGold,
                        entryLeakDamage);
                }
            }
        }

        static void AppendRow(
            StringBuilder report,
            string rowType,
            string tower,
            string enemy,
            int wave,
            int count,
            float spawnInterval,
            float spawnDuration,
            float baseHealth,
            float scaledHealth,
            float rawDamageMin,
            float rawDamageMax,
            float effectiveDamageMin,
            float effectiveDamageMax,
            float effectiveActionDamageMin,
            float effectiveActionDamageMax,
            float fireInterval,
            int projectiles,
            string deliveryPattern,
            int payloadCount,
            float payloadAllHitRawDamageMin,
            float payloadAllHitRawDamageMax,
            float payloadAoeRadius,
            int bestActionsToKill,
            int worstActionsToKill,
            float bestTimeToKill,
            float worstTimeToKill,
            float dpsMin,
            float dpsMax,
            int towerCost,
            int nextTowerCostAtZero,
            int goldBeforeWave,
            int endWaveGold,
            int killGold,
            int leakDamage)
        {
            AppendField(report, rowType);
            AppendField(report, tower);
            AppendField(report, enemy);
            AppendField(report, wave);
            AppendField(report, count);
            AppendField(report, spawnInterval);
            AppendField(report, spawnDuration);
            AppendField(report, baseHealth);
            AppendField(report, scaledHealth);
            AppendField(report, rawDamageMin);
            AppendField(report, rawDamageMax);
            AppendField(report, effectiveDamageMin);
            AppendField(report, effectiveDamageMax);
            AppendField(report, effectiveActionDamageMin);
            AppendField(report, effectiveActionDamageMax);
            AppendField(report, fireInterval);
            AppendField(report, projectiles);
            AppendField(report, deliveryPattern);
            AppendField(report, payloadCount);
            AppendField(report, payloadAllHitRawDamageMin);
            AppendField(report, payloadAllHitRawDamageMax);
            AppendField(report, payloadAoeRadius);
            AppendField(report, bestActionsToKill);
            AppendField(report, worstActionsToKill);
            AppendField(report, bestTimeToKill);
            AppendField(report, worstTimeToKill);
            AppendField(report, dpsMin);
            AppendField(report, dpsMax);
            AppendField(report, towerCost);
            AppendField(report, nextTowerCostAtZero);
            AppendField(report, goldBeforeWave);
            AppendField(report, endWaveGold);
            AppendField(report, killGold);
            AppendField(report, leakDamage);
            report.AppendLine();
        }

        static void AppendField(StringBuilder report, string value)
        {
            if (value == null)
                value = string.Empty;

            report.Append('"');
            report.Append(value.Replace("\"", "\"\""));
            report.Append('"');
            report.Append(',');
        }

        static void AppendField(StringBuilder report, int value)
        {
            AppendField(report, value.ToString(CultureInfo.InvariantCulture));
        }

        static void AppendField(StringBuilder report, float value)
        {
            AppendField(report, value.ToString("0.####", CultureInfo.InvariantCulture));
        }
    }
}
