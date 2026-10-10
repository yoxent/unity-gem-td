using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// Folds covering ally-aura maxima into a tower's skill spec after gems.
    /// Same-kind overlap keeps the stronger source only.
    /// </summary>
    public static class AuraInfluenceRuntime
    {
        const int KindCapacity = 64;

        static readonly float[] BestMin = new float[KindCapacity];
        static readonly float[] BestMax = new float[KindCapacity];
        static readonly byte[] HasKind = new byte[KindCapacity];

        public static Vector3 CellCenter(Vector2Int cell, float cellSize)
        {
            var size = cellSize > 0f ? cellSize : 1f;
            var half = size * 0.5f;
            return new Vector3(cell.x * size + half, 0f, cell.y * size + half);
        }

        public static void Apply(
            TowerInstance ally,
            IReadOnlyList<TowerInstance> sources,
            ref SkillSpec spec,
            float cellSize)
        {
            if (ally?.Def == null || sources == null)
                return;

            for (var i = 0; i < KindCapacity; i++)
                HasKind[i] = 0;

            var allyPos = CellCenter(ally.Cell, cellSize);
            for (var s = 0; s < sources.Count; s++)
            {
                var source = sources[s];
                if (source?.Def == null)
                    continue;

                var aura = source.Def.GetRole<AuraRoleDefinition>();
                if (aura == null)
                    continue;

                var self = ReferenceEquals(source, ally);
                if (self)
                {
                    if (source.Def.IsAuraOnly)
                        continue;
                    if ((GemTags.EffectiveTowerTags(source.Def) & GemTag.Aura) == 0)
                        continue;
                }

                var radius = source.Def.GetAuraTowerRadius(source.Level);
                if (radius <= 0f)
                    continue;

                var delta = CellCenter(source.Cell, cellSize) - allyPos;
                delta.y = 0f;
                if (delta.sqrMagnitude > radius * radius)
                    continue;

                Collect(aura, source.Level);
            }

            Fold(ally.Def, ref spec);
        }

        static void Collect(AuraRoleDefinition aura, int sourceLevel)
        {
            for (var k = 0; k < KindCapacity; k++)
            {
                var kind = (RoleEffectKind)k;
                if (!IsLiveKind(kind))
                    continue;

                var value = aura.ResolveEffectValue(kind, sourceLevel);
                if (value.Min <= 0f && value.Max <= 0f)
                    continue;

                Consider(k, value.Min, value.Max);
            }
        }

        static void Consider(int kindIndex, float min, float max)
        {
            var mag = (min + max) * 0.5f;
            if (HasKind[kindIndex] == 0)
            {
                HasKind[kindIndex] = 1;
                BestMin[kindIndex] = min;
                BestMax[kindIndex] = max;
                return;
            }

            var current = (BestMin[kindIndex] + BestMax[kindIndex]) * 0.5f;
            if (mag > current)
            {
                BestMin[kindIndex] = min;
                BestMax[kindIndex] = max;
            }
        }

        static void Fold(TowerDefinition allyDef, ref SkillSpec spec)
        {
            var attackAlly = allyDef.UsesAttackSpeed;

            ApplyPercent(RoleEffectKind.AllyAttackSpeedIncreased, ref spec.AttackSpeedMultiplier, attackAlly);
            ApplyPercent(RoleEffectKind.AllyCastSpeedIncreased, ref spec.CastSpeedMultiplier, !attackAlly);

            if (attackAlly)
            {
                AddDamage(RoleEffectKind.AllyAddedAttackFireDamage, ref spec);
                AddDamage(RoleEffectKind.AllyAddedAttackLightningDamage, ref spec);
                AddDamage(RoleEffectKind.AllyAddedAttackChaosDamage, ref spec);
                AddDamage(RoleEffectKind.AllyAddedAttackColdDamage, ref spec);
            }
            else
            {
                AddDamage(RoleEffectKind.AllyAddedSpellFireDamage, ref spec);
                if (!AddDamage(RoleEffectKind.AllyAddedSpellLightningDamage, ref spec))
                    AddDamage(RoleEffectKind.AllyAddedAttackLightningDamage, ref spec);
                AddDamage(RoleEffectKind.AllyAddedSpellChaosDamage, ref spec);
                AddDamage(RoleEffectKind.AllyAddedSpellColdDamage, ref spec);
            }

            ApplyPercent(RoleEffectKind.AllyDamageOverTimeMore, ref spec.AilmentDamageMultiplier, true);
            ApplyPercent(RoleEffectKind.AllySkillEffectDurationIncreased, ref spec.AilmentDurationMultiplier, true);

            var critIndex = (int)RoleEffectKind.AllyCriticalStrikeChanceIncreased;
            if (critIndex >= 0 && critIndex < KindCapacity && HasKind[critIndex] != 0)
            {
                spec.CritChance += BestMax[critIndex] / 100f;
                if (spec.CritChance > 1f)
                    spec.CritChance = 1f;
            }
        }

        static void ApplyPercent(RoleEffectKind kind, ref float multiplier, bool enabled)
        {
            if (!enabled)
                return;
            var index = (int)kind;
            if (index < 0 || index >= KindCapacity || HasKind[index] == 0)
                return;
            if (multiplier <= 0.01f)
                multiplier = 1f;
            multiplier *= 1f + BestMax[index] / 100f;
        }

        static bool AddDamage(RoleEffectKind kind, ref SkillSpec spec)
        {
            var index = (int)kind;
            if (index < 0 || index >= KindCapacity || HasKind[index] == 0)
                return false;
            spec.DamageMin += BestMin[index];
            spec.DamageMax += BestMax[index];
            spec.Damage = (spec.DamageMin + spec.DamageMax) * 0.5f;
            return true;
        }

        static bool IsLiveKind(RoleEffectKind kind)
        {
            switch (kind)
            {
                case RoleEffectKind.AllyAttackSpeedIncreased:
                case RoleEffectKind.AllyCastSpeedIncreased:
                case RoleEffectKind.AllyAddedAttackFireDamage:
                case RoleEffectKind.AllyAddedSpellFireDamage:
                case RoleEffectKind.AllyAddedAttackLightningDamage:
                case RoleEffectKind.AllyAddedSpellLightningDamage:
                case RoleEffectKind.AllyAddedAttackChaosDamage:
                case RoleEffectKind.AllyAddedSpellChaosDamage:
                case RoleEffectKind.AllyAddedAttackColdDamage:
                case RoleEffectKind.AllyAddedSpellColdDamage:
                case RoleEffectKind.AllyDamageOverTimeMore:
                case RoleEffectKind.AllySkillEffectDurationIncreased:
                case RoleEffectKind.AllyCriticalStrikeChanceIncreased:
                    return true;
                default:
                    return false;
            }
        }
    }
}
