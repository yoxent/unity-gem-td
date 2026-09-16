using System;
using System.Collections.Generic;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;
using UnityEngine;

namespace GemTD.Gameplay.Balance
{
    public readonly struct BalanceHitResult
    {
        public readonly bool IsValid;
        public readonly float RawDamageMin;
        public readonly float RawDamageMax;
        public readonly float ScaledHealth;
        public readonly float ScaledShield;
        public readonly float EffectiveDamageMin;
        public readonly float EffectiveDamageMax;
        public readonly float EffectiveActionDamageMin;
        public readonly float EffectiveActionDamageMax;
        public readonly float FireInterval;
        public readonly int ProjectileCount;
        public readonly int BestCaseActionsToKill;
        public readonly int WorstCaseActionsToKill;
        public readonly float BestCaseTimeToKill;
        public readonly float WorstCaseTimeToKill;

        public BalanceHitResult(
            bool isValid,
            float rawDamageMin,
            float rawDamageMax,
            float scaledHealth,
            float scaledShield,
            float effectiveDamageMin,
            float effectiveDamageMax,
            float effectiveActionDamageMin,
            float effectiveActionDamageMax,
            float fireInterval,
            int projectileCount,
            int bestCaseActionsToKill,
            int worstCaseActionsToKill,
            float bestCaseTimeToKill,
            float worstCaseTimeToKill)
        {
            IsValid = isValid;
            RawDamageMin = rawDamageMin;
            RawDamageMax = rawDamageMax;
            ScaledHealth = scaledHealth;
            ScaledShield = scaledShield;
            EffectiveDamageMin = effectiveDamageMin;
            EffectiveDamageMax = effectiveDamageMax;
            EffectiveActionDamageMin = effectiveActionDamageMin;
            EffectiveActionDamageMax = effectiveActionDamageMax;
            FireInterval = fireInterval;
            ProjectileCount = projectileCount;
            BestCaseActionsToKill = bestCaseActionsToKill;
            WorstCaseActionsToKill = worstCaseActionsToKill;
            BestCaseTimeToKill = bestCaseTimeToKill;
            WorstCaseTimeToKill = worstCaseTimeToKill;
        }
    }

    public readonly struct BalanceWaveResult
    {
        public readonly int WaveNumber;
        public readonly int EnemyCount;
        public readonly float SpawnInterval;
        public readonly float SpawnDuration;
        public readonly float BaseHealth;
        public readonly float ScaledHealth;
        public readonly int TotalLeakDamage;
        public readonly int TotalKillGold;

        public BalanceWaveResult(
            int waveNumber,
            int enemyCount,
            float spawnInterval,
            float spawnDuration,
            float baseHealth,
            float scaledHealth,
            int totalLeakDamage,
            int totalKillGold)
        {
            WaveNumber = waveNumber;
            EnemyCount = enemyCount;
            SpawnInterval = spawnInterval;
            SpawnDuration = spawnDuration;
            BaseHealth = baseHealth;
            ScaledHealth = scaledHealth;
            TotalLeakDamage = totalLeakDamage;
            TotalKillGold = totalKillGold;
        }
    }

    public readonly struct BalancePayloadResult
    {
        public readonly int PayloadCount;
        // Upper-bound total when every payload hits the same target.
        public readonly float TotalRawDamageMin;
        public readonly float TotalRawDamageMax;
        public readonly float MaxAoeRadius;

        public BalancePayloadResult(
            int payloadCount,
            float totalRawDamageMin,
            float totalRawDamageMax,
            float maxAoeRadius)
        {
            PayloadCount = payloadCount;
            TotalRawDamageMin = totalRawDamageMin;
            TotalRawDamageMax = totalRawDamageMax;
            MaxAoeRadius = maxAoeRadius;
        }
    }

    /// <summary>
    /// Offline balance calculations. This intentionally models one fixed damage
    /// state at a time; dynamic targeting and path interactions belong in a
    /// deterministic encounter calculator built on top of these results.
    /// </summary>
    public static class BalanceCalculator
    {
        public const int NoKill = -1;
        const int MaxActionCount = 1000000;
        const float ProgressEpsilon = 0.0001f;

        public static BalanceHitResult Evaluate(
            in SkillSpec spec,
            float fireInterval,
            EnemyDefinition enemy,
            float healthScale = 1f,
            float firstHitDelay = 0f)
        {
            if (enemy == null)
                return default;

            var rawMin = spec.DamageMin;
            var rawMax = spec.DamageMax;
            if (rawMin <= 0f && spec.Damage > 0f)
                rawMin = spec.Damage;
            if (rawMax <= 0f && spec.Damage > 0f)
                rawMax = spec.Damage;
            if (rawMax < rawMin)
            {
                var swap = rawMin;
                rawMin = rawMax;
                rawMax = swap;
            }

            rawMin = Mathf.Max(0f, rawMin);
            rawMax = Mathf.Max(0f, rawMax);
            var safeHealthScale = Mathf.Max(0f, healthScale);
            var scaledHealth = enemy.MaxHealth * safeHealthScale;
            var scaledShield = enemy.ShieldMax;
            var projectileCount = spec.ProjectileCount > 0 ? spec.ProjectileCount : 1;
            var safeInterval = Mathf.Max(0f, fireInterval);
            var safeFirstHitDelay = Mathf.Max(0f, firstHitDelay);
            var initialRuntime = CreateRuntime(enemy, safeHealthScale);
            var effectiveMin = IncomingHit.Mitigate(rawMin, spec, initialRuntime, null);
            var effectiveMax = IncomingHit.Mitigate(rawMax, spec, initialRuntime, null);
            var effectiveActionMin = MeasureActionDamage(
                rawMin,
                spec,
                enemy,
                healthScale,
                projectileCount);
            var effectiveActionMax = MeasureActionDamage(
                rawMax,
                spec,
                enemy,
                healthScale,
                projectileCount);
            var bestActions = CountActionsToKill(
                rawMax,
                spec,
                enemy,
                healthScale,
                projectileCount);
            var worstActions = CountActionsToKill(
                rawMin,
                spec,
                enemy,
                healthScale,
                projectileCount);

            return new BalanceHitResult(
                rawMax > 0f && enemy.MaxHealth * Mathf.Max(0f, healthScale) + enemy.ShieldMax > 0f,
                rawMin,
                rawMax,
                scaledHealth,
                scaledShield,
                effectiveMin,
                effectiveMax,
                effectiveActionMin,
                effectiveActionMax,
                safeInterval,
                projectileCount,
                bestActions,
                worstActions,
                TimeToKill(bestActions, safeInterval, safeFirstHitDelay),
                TimeToKill(worstActions, safeInterval, safeFirstHitDelay));
        }

        public static BalanceHitResult Evaluate(
            TowerDefinition tower,
            EnemyDefinition enemy,
            int sourceLevel = TowerInstance.DefaultLevel,
            float healthScale = 1f,
            float firstHitDelay = 0f)
        {
            if (tower == null)
                return default;

            var instance = new TowerInstance(Vector2Int.zero, tower);
            instance.SetLevel(sourceLevel);
            var pipeline = new GemModifierPipeline();
            var spec = pipeline.ResolveBaseline(instance);
            return Evaluate(
                spec,
                tower.FireInterval(spec, instance.Level),
                enemy,
                healthScale,
                firstHitDelay);
        }

        /// <summary>
        /// Summarizes authored secondary damage. Spatial overlap and target
        /// selection are intentionally left to an encounter-level model.
        /// </summary>
        public static BalancePayloadResult EvaluatePayloads(
            in SkillSpec spec,
            in SkillSpec baseline,
            IReadOnlyList<EffectPayloadDefinition> payloads)
        {
            if (payloads == null || payloads.Count == 0)
                return default;

            var payloadCount = 0;
            var totalRawMin = 0f;
            var totalRawMax = 0f;
            var maxAoeRadius = 0f;
            var baseProjectileCount = Mathf.Max(0, baseline.ProjectileCount);
            var projectileCount = Mathf.Max(0, spec.ProjectileCount);
            var rawMin = spec.DamageMin;
            var rawMax = spec.DamageMax;
            if (rawMin <= 0f && spec.Damage > 0f)
                rawMin = spec.Damage;
            if (rawMax <= 0f && spec.Damage > 0f)
                rawMax = spec.Damage;
            rawMin = Mathf.Max(0f, rawMin);
            rawMax = Mathf.Max(0f, rawMax);
            if (rawMax < rawMin)
            {
                var swap = rawMin;
                rawMin = rawMax;
                rawMax = swap;
            }

            for (var i = 0; i < payloads.Count; i++)
            {
                var payload = payloads[i];
                if (payload == null || !payload.IsValid)
                    continue;

                var count = payload.Count;
                if ((payload.Tags & GemTag.Projectile) != 0)
                    count += Mathf.Max(0, projectileCount - baseProjectileCount);

                if (count <= 0)
                    continue;

                payloadCount += count;
                totalRawMin += rawMin * payload.DamageMultiplier * count;
                totalRawMax += rawMax * payload.DamageMultiplier * count;
                maxAoeRadius = Mathf.Max(maxAoeRadius, payload.AoeRadius);
            }

            return new BalancePayloadResult(
                payloadCount,
                Mathf.Max(0f, totalRawMin),
                Mathf.Max(0f, totalRawMax),
                maxAoeRadius);
        }

        public static BalancePayloadResult EvaluatePayloads(
            TowerDefinition tower,
            int sourceLevel = TowerInstance.DefaultLevel)
        {
            if (tower == null)
                return default;

            var instance = new TowerInstance(Vector2Int.zero, tower);
            instance.SetLevel(sourceLevel);
            var pipeline = new GemModifierPipeline();
            var baseline = pipeline.ResolveBaseline(instance);
            return EvaluatePayloads(
                baseline,
                baseline,
                tower.GetEffectPayloads());
        }

        public static BalanceWaveResult EvaluateWave(
            WaveDefinition wave,
            float healthScale = 1f)
        {
            if (wave == null)
                return default;

            var safeHealthScale = Mathf.Max(0f, healthScale);
            var enemyCount = 0;
            var baseHealth = 0f;
            var totalLeakDamage = 0;
            var totalKillGold = 0;
            var entries = wave.Entries;
            if (entries != null)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    if (entry.Enemy == null || entry.Count <= 0)
                        continue;

                    enemyCount += entry.Count;
                    baseHealth += entry.Enemy.MaxHealth * entry.Count;
                    totalLeakDamage += entry.Enemy.LeakDamage * entry.Count;
                    totalKillGold += entry.Enemy.KillGold * entry.Count;
                }
            }

            var spawnInterval = Mathf.Max(0f, wave.SpawnInterval);
            var spawnDuration = enemyCount > 1
                ? (enemyCount - 1) * spawnInterval
                : 0f;
            return new BalanceWaveResult(
                wave.WaveNumber,
                enemyCount,
                spawnInterval,
                spawnDuration,
                baseHealth,
                baseHealth * safeHealthScale,
                totalLeakDamage,
                totalKillGold);
        }

        public static int ComputeNextTowerCost(
            TowerDefinition tower,
            int sameTypeCount)
        {
            if (tower == null)
                return 0;

            return Mathf.Max(0, tower.Cost)
                + Mathf.Max(0, sameTypeCount) * Mathf.Max(0, tower.BuildIncrement);
        }

        public static int ComputeGoldBeforeWave(
            RunConfig config,
            WaveDefinition[] waves,
            int waveNumber)
        {
            if (config == null)
                return 0;

            var gold = config.StartingGold;
            if (waves == null)
                return gold;

            for (var i = 0; i < waves.Length; i++)
            {
                var wave = waves[i];
                if (wave == null || wave.WaveNumber <= 0 || wave.WaveNumber >= waveNumber)
                    continue;

                gold += ComputeWaveKillGold(wave);
                gold += WaveScaling.ScaleEndWaveGold(config.EndWaveGold, wave.WaveNumber);
            }

            return gold;
        }

        public static int ComputeWaveKillGold(WaveDefinition wave)
        {
            if (wave == null || wave.Entries == null)
                return 0;

            var gold = 0;
            for (var i = 0; i < wave.Entries.Length; i++)
            {
                var entry = wave.Entries[i];
                if (entry.Enemy == null || entry.Count <= 0)
                    continue;

                var perEnemy = entry.Enemy.IsBoss
                    ? WaveScaling.ScaleBossBounty(entry.Enemy.KillGold, wave.WaveNumber)
                    : entry.Enemy.KillGold;
                gold += perEnemy * entry.Count;
            }

            return gold;
        }

        static int CountActionsToKill(
            float rawDamage,
            in SkillSpec spec,
            EnemyDefinition enemy,
            float healthScale,
            int projectileCount)
        {
            if (rawDamage <= 0f || enemy == null)
                return NoKill;

            var runtime = CreateRuntime(enemy, healthScale);
            if (runtime.Hp + runtime.ShieldHp <= 0f)
                return 0;

            for (var action = 1; action <= MaxActionCount; action++)
            {
                var before = runtime.Hp + runtime.ShieldHp;
                ApplyAction(runtime, rawDamage, spec, projectileCount);
                if (!runtime.IsAlive)
                    return action;

                var after = runtime.Hp + runtime.ShieldHp;
                if (before - after <= ProgressEpsilon)
                    return NoKill;
            }

            return NoKill;
        }

        static float MeasureActionDamage(
            float rawDamage,
            in SkillSpec spec,
            EnemyDefinition enemy,
            float healthScale,
            int projectileCount)
        {
            if (rawDamage <= 0f || enemy == null)
                return 0f;

            var runtime = CreateRuntime(enemy, healthScale);
            var before = runtime.Hp + runtime.ShieldHp;
            ApplyAction(runtime, rawDamage, spec, projectileCount);
            return Mathf.Max(0f, before - runtime.Hp - runtime.ShieldHp);
        }

        static void ApplyAction(
            EnemyRuntime runtime,
            float rawDamage,
            in SkillSpec spec,
            int projectileCount)
        {
            for (var i = 0; i < projectileCount && runtime.IsAlive; i++)
                runtime.ApplyDamage(rawDamage, spec, null);
        }

        static EnemyRuntime CreateRuntime(
            EnemyDefinition enemy,
            float healthScale)
        {
            var runtime = new EnemyRuntime();
            runtime.Init(
                enemy,
                Array.Empty<Vector3>(),
                Mathf.Max(0f, healthScale));
            return runtime;
        }

        static float TimeToKill(
            int actions,
            float fireInterval,
            float firstHitDelay)
        {
            if (actions == NoKill)
                return -1f;
            if (actions <= 1)
                return firstHitDelay;

            return firstHitDelay + (actions - 1) * fireInterval;
        }
    }
}
