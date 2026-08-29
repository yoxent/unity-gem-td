using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GemTD.Gameplay.Towers
{
    public enum RoleStat
    {
        AttackTime,
        AttackSpeed,
        CastTime,
        CastSpeed,
        TowerRadius,
        SplashRadius,
        Damage,
        ReservationPercent,
        ProjectileSpeed,
        ProjectileCount,
        ChainCount
    }

    public enum RoleModifierOperation
    {
        Set,
        Add,
        Multiply
    }

    public enum RoleStatValueKind
    {
        Single,
        Range
    }

    [Serializable]
    public struct RoleStatValue
    {
        public float Min;
        public float Max;

        public bool IsRange => Max > Min + 0.0001f;

        public float Midpoint => (Min + Max) * 0.5f;

        public float Scalar => IsRange ? Midpoint : Min;

        public static RoleStatValue FromSingle(float value)
        {
            return new RoleStatValue { Min = value, Max = value };
        }

        public static RoleStatValue FromRange(float min, float max)
        {
            if (max < min)
            {
                var swap = min;
                min = max;
                max = swap;
            }

            return new RoleStatValue { Min = min, Max = max };
        }

        public static float SampleHitDamage(float min, float max)
        {
            if (max <= min)
                return min;

            var lo = Mathf.FloorToInt(min);
            var hi = Mathf.FloorToInt(max);
            if (hi > lo)
                return Random.Range(lo, hi + 1);

            return min;
        }
    }

    [Serializable]
    public struct RoleStatModifier
    {
        public RoleStat Stat;
        public RoleModifierOperation Operation;
        [Tooltip("Single uses Value. Range uses Min and Max. Add/Multiply may be Single even when the stat was Set as a range.")]
        public RoleStatValueKind ValueKind;
        public float Value;
        public float Min;
        public float Max;

        public float OperandMin => ValueKind == RoleStatValueKind.Range ? Min : Value;

        public float OperandMax => ValueKind == RoleStatValueKind.Range ? Max : Value;

        public static RoleStatModifier Single(RoleStat stat, RoleModifierOperation operation, float value)
        {
            return new RoleStatModifier
            {
                Stat = stat,
                Operation = operation,
                ValueKind = RoleStatValueKind.Single,
                Value = value
            };
        }

        public static RoleStatModifier Range(
            RoleStat stat,
            RoleModifierOperation operation,
            float min,
            float max)
        {
            return new RoleStatModifier
            {
                Stat = stat,
                Operation = operation,
                ValueKind = RoleStatValueKind.Range,
                Min = min,
                Max = max
            };
        }
    }

    public enum RoleEffectKind
    {
        AllyOutgoingDamageMultiplier,
        EnemyMoveSpeedMultiplier,
        AllyAddedAttackFireDamage,
        AllyAddedSpellFireDamage,
        SkillDuration,
        EnemyColdResistance
    }

    [Serializable]
    public struct RoleEffectModifier
    {
        public RoleEffectKind Kind;
        public RoleModifierOperation Operation;
        [Tooltip("Single uses Value. Range uses Min and Max. Add/Multiply may be Single even when the effect was Set as a range.")]
        public RoleStatValueKind ValueKind;
        public float Value;
        public float Min;
        public float Max;

        public float OperandMin => ValueKind == RoleStatValueKind.Range ? Min : Value;

        public float OperandMax => ValueKind == RoleStatValueKind.Range ? Max : Value;

        public static RoleEffectModifier Single(
            RoleEffectKind kind,
            RoleModifierOperation operation,
            float value)
        {
            return new RoleEffectModifier
            {
                Kind = kind,
                Operation = operation,
                ValueKind = RoleStatValueKind.Single,
                Value = value
            };
        }

        public static RoleEffectModifier Range(
            RoleEffectKind kind,
            RoleModifierOperation operation,
            float min,
            float max)
        {
            return new RoleEffectModifier
            {
                Kind = kind,
                Operation = operation,
                ValueKind = RoleStatValueKind.Range,
                Min = min,
                Max = max
            };
        }
    }

    [Serializable]
    public sealed class RoleLevelDefinition
    {
        public int SourceLevel;
        public RoleStatModifier[] Modifiers;
        public RoleEffectModifier[] Effects;
    }
}
