using System;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Gems
{
    public enum GemStat
    {
        Damage,
        ProjectileCount,
        SpreadDegrees,
        ChainCount,
        ForkCount,
        AoeRadius,
        FireRateMultiplier,
        AttackSpeedMultiplier,
        CastSpeedMultiplier,
        RangeMultiplier,
        ProjectileSpeedMultiplier,
        EchoVolleyCount,
        EchoDamageFactor,
        PierceCount,
        Ignite,
        Chill,
        Shock,
        Proliferate,
        KnockbackChance,
        KnockbackDistance,
        BleedChance,
        BleedDamage,
        BleedDuration,
        IgniteChance,
        IgniteDuration,
        ChillEffect,
        ChillDuration,
        ShockChance,
        ShockEffect,
        ShockDuration,
        FreezeChance,
        FreezeDuration,
        PoisonChance,
        PoisonDuration,
        StunChance,
        StunDuration,
        BurningDamage,
        PhysAsExtraFire,
        PhysAsExtraCold,
        PhysAsExtraLightning,
        PhysAsExtraChaos,
        ConvertFireToCold,
        ConvertColdToLightning,
        ConvertLightningToPhysical,
        ConvertFireToChaos,
        ConvertLightningToChaos,
        ConvertColdToChaos,
        HallowingFlame,
        AilmentDamage,
        AilmentDuration,
        AimMode,
        DeliveryPattern
    }

    [Serializable]
    public struct GemStatModifier
    {
        public GemStat Stat;
        public RoleModifierOperation Operation;
        [Tooltip("Single uses Value. Range uses Min and Max.")]
        public RoleStatValueKind ValueKind;
        public float Value;
        public float Min;
        public float Max;
        public float Lesser;
        public float Normal;
        public float Greater;
        [Tooltip("ChainCount only. Resolved per-hop damage factor. 0 = no falloff (hops keep full damage).")]
        public float Falloff;
        [Tooltip("ChainCount only. Lesser rarity hop falloff. 0 with other tiers 0 = use Falloff for every rarity.")]
        public float LesserFalloff;
        [Tooltip("ChainCount only. Normal rarity hop falloff.")]
        public float NormalFalloff;
        [Tooltip("ChainCount only. Greater rarity hop falloff.")]
        public float GreaterFalloff;

        public bool HasTierValues =>
            Lesser != 0f || Normal != 0f || Greater != 0f;

        public bool HasTierFalloff =>
            LesserFalloff != 0f || NormalFalloff != 0f || GreaterFalloff != 0f;

        public float OperandMin => ValueKind == RoleStatValueKind.Range ? Min : Value;

        public float OperandMax => ValueKind == RoleStatValueKind.Range ? Max : Value;

        public float EffectiveHopFalloff => Falloff == 0f ? 1f : Falloff;

        public static GemStatModifier Single(
            GemStat stat,
            RoleModifierOperation operation,
            float value,
            float falloff = 0f)
        {
            return TieredSingle(stat, operation, value, value, value, falloff, falloff, falloff);
        }

        public static GemStatModifier TieredSingle(
            GemStat stat,
            RoleModifierOperation operation,
            float lesser,
            float normal,
            float greater,
            float falloff = 0f)
        {
            return TieredSingle(stat, operation, lesser, normal, greater, falloff, falloff, falloff);
        }

        public static GemStatModifier TieredSingle(
            GemStat stat,
            RoleModifierOperation operation,
            float lesser,
            float normal,
            float greater,
            float lesserFalloff,
            float normalFalloff,
            float greaterFalloff)
        {
            return new GemStatModifier
            {
                Stat = stat,
                Operation = operation,
                ValueKind = RoleStatValueKind.Single,
                Lesser = lesser,
                Normal = normal,
                Greater = greater,
                Value = normal,
                Falloff = normalFalloff,
                LesserFalloff = lesserFalloff,
                NormalFalloff = normalFalloff,
                GreaterFalloff = greaterFalloff
            };
        }

        public static GemStatModifier Range(
            GemStat stat,
            RoleModifierOperation operation,
            float min,
            float max)
        {
            return new GemStatModifier
            {
                Stat = stat,
                Operation = operation,
                ValueKind = RoleStatValueKind.Range,
                Min = min,
                Max = max
            };
        }

        public GemStatModifier Resolve(GemRarity rarity)
        {
            if (!HasTierValues && !HasTierFalloff)
                return this;

            var normalized = GemRarityUtility.Normalize(rarity);
            var resolved = this;
            resolved.ValueKind = RoleStatValueKind.Single;

            if (HasTierValues)
            {
                var value = Value;
                switch (normalized)
                {
                    case GemRarity.Lesser:
                        value = Lesser;
                        break;
                    case GemRarity.Greater:
                        value = Greater;
                        break;
                    case GemRarity.Normal:
                    default:
                        value = Normal;
                        break;
                }

                resolved.Value = value;
                resolved.Min = value;
                resolved.Max = value;
            }

            if (HasTierFalloff)
            {
                switch (normalized)
                {
                    case GemRarity.Lesser:
                        resolved.Falloff = LesserFalloff;
                        break;
                    case GemRarity.Greater:
                        resolved.Falloff = GreaterFalloff;
                        break;
                    case GemRarity.Normal:
                    default:
                        resolved.Falloff = NormalFalloff;
                        break;
                }
            }

            return resolved;
        }
    }

    /// <summary>
    /// Applies gem SO rows onto a <see cref="SkillSpec"/>. Same Set → Add → Multiply order as roles.
    /// </summary>
    public static class GemStatResolver
    {
        public static SkillSpec Apply(SkillSpec spec, GemStatModifier[] modifiers)
        {
            return Apply(spec, modifiers, GemRarity.Normal);
        }

        public static SkillSpec Apply(
            SkillSpec spec,
            GemStatModifier[] modifiers,
            GemRarity rarity)
        {
            if (modifiers == null || modifiers.Length == 0)
                return spec;

            ApplyGroup(ref spec, modifiers, RoleModifierOperation.Set, rarity);
            ApplyGroup(ref spec, modifiers, RoleModifierOperation.Add, rarity);
            ApplyGroup(ref spec, modifiers, RoleModifierOperation.Multiply, rarity);
            return spec;
        }

        static void ApplyGroup(
            ref SkillSpec spec,
            GemStatModifier[] modifiers,
            RoleModifierOperation operation,
            GemRarity rarity)
        {
            for (var i = 0; i < modifiers.Length; i++)
            {
                var modifier = modifiers[i];
                if (modifier.Operation != operation)
                    continue;

                ApplyOne(ref spec, modifier.Resolve(rarity));
            }
        }

        static void ApplyOne(ref SkillSpec spec, GemStatModifier modifier)
        {
            var min = modifier.OperandMin;
            var max = modifier.OperandMax;
            if (modifier.ValueKind == RoleStatValueKind.Range && max < min)
            {
                var swap = min;
                min = max;
                max = swap;
            }

            var scalar = modifier.ValueKind == RoleStatValueKind.Range ? (min + max) * 0.5f : min;

            switch (modifier.Stat)
            {
                case GemStat.Damage:
                    ApplyDamage(ref spec, modifier.Operation, min, max);
                    break;
                case GemStat.ProjectileCount:
                    spec.ProjectileCount = WholeOp(spec.ProjectileCount, modifier.Operation, scalar);
                    break;
                case GemStat.SpreadDegrees:
                    spec.SpreadDegrees = FloatOp(spec.SpreadDegrees, modifier.Operation, scalar);
                    break;
                case GemStat.ChainCount:
                    spec.ChainCount = WholeOp(spec.ChainCount, modifier.Operation, scalar);
                    spec.ChainHopFalloff = modifier.EffectiveHopFalloff;
                    break;
                case GemStat.ForkCount:
                    spec.ForkCount = WholeOp(spec.ForkCount, modifier.Operation, scalar);
                    break;
                case GemStat.AoeRadius:
                    spec.AoeRadius = FloatOp(spec.AoeRadius, modifier.Operation, scalar);
                    if (modifier.Operation == RoleModifierOperation.Multiply)
                    {
                        spec.AoeRadiusMultiplier = FloatOp(
                            spec.AoeRadiusMultiplier,
                            RoleModifierOperation.Multiply,
                            scalar);
                    }

                    break;
                case GemStat.FireRateMultiplier:
                    spec.FireRateMultiplier = FloatOp(spec.FireRateMultiplier, modifier.Operation, scalar);
                    break;
                case GemStat.AttackSpeedMultiplier:
                    spec.AttackSpeedMultiplier = FloatOp(spec.AttackSpeedMultiplier, modifier.Operation, scalar);
                    break;
                case GemStat.CastSpeedMultiplier:
                    spec.CastSpeedMultiplier = FloatOp(spec.CastSpeedMultiplier, modifier.Operation, scalar);
                    break;
                case GemStat.RangeMultiplier:
                    spec.RangeMultiplier = FloatOp(spec.RangeMultiplier, modifier.Operation, scalar);
                    break;
                case GemStat.ProjectileSpeedMultiplier:
                    spec.ProjectileSpeedMultiplier = FloatOp(spec.ProjectileSpeedMultiplier, modifier.Operation, scalar);
                    break;
                case GemStat.EchoVolleyCount:
                    spec.EchoVolleyCount = WholeOp(spec.EchoVolleyCount, modifier.Operation, scalar);
                    break;
                case GemStat.EchoDamageFactor:
                    spec.EchoDamageFactor = FloatOp(spec.EchoDamageFactor, modifier.Operation, scalar);
                    break;
                case GemStat.PierceCount:
                    ApplyPierce(ref spec, modifier.Operation, scalar);
                    break;
                case GemStat.Ignite:
                    ApplyFlag(ref spec.Ignite, modifier.Operation, scalar);
                    break;
                case GemStat.Chill:
                    ApplyFlag(ref spec.Chill, modifier.Operation, scalar);
                    break;
                case GemStat.Shock:
                    ApplyFlag(ref spec.Shock, modifier.Operation, scalar);
                    break;
                case GemStat.Proliferate:
                    ApplyFlag(ref spec.Proliferate, modifier.Operation, scalar);
                    break;
                case GemStat.KnockbackChance:
                    spec.KnockbackChance = FloatOp(spec.KnockbackChance, modifier.Operation, scalar);
                    break;
                case GemStat.KnockbackDistance:
                    spec.KnockbackDistance = FloatOp(spec.KnockbackDistance, modifier.Operation, scalar);
                    break;
                case GemStat.BleedChance:
                    spec.BleedChance = FloatOp(spec.BleedChance, modifier.Operation, scalar);
                    break;
                case GemStat.BleedDamage:
                    spec.BleedDamageMultiplier = FloatOp(
                        spec.BleedDamageMultiplier,
                        modifier.Operation,
                        scalar);
                    break;
                case GemStat.BleedDuration:
                    spec.BleedDuration = FloatOp(spec.BleedDuration, modifier.Operation, scalar);
                    break;
                case GemStat.IgniteChance:
                    spec.IgniteChance = FloatOp(spec.IgniteChance, modifier.Operation, scalar);
                    break;
                case GemStat.IgniteDuration:
                    spec.IgniteDuration = FloatOp(spec.IgniteDuration, modifier.Operation, scalar);
                    break;
                case GemStat.ChillEffect:
                    spec.ChillEffect = FloatOp(spec.ChillEffect, modifier.Operation, scalar);
                    break;
                case GemStat.ChillDuration:
                    spec.ChillDuration = FloatOp(spec.ChillDuration, modifier.Operation, scalar);
                    break;
                case GemStat.ShockChance:
                    spec.ShockChance = FloatOp(spec.ShockChance, modifier.Operation, scalar);
                    break;
                case GemStat.ShockEffect:
                    spec.ShockEffect = FloatOp(spec.ShockEffect, modifier.Operation, scalar);
                    break;
                case GemStat.ShockDuration:
                    spec.ShockDuration = FloatOp(spec.ShockDuration, modifier.Operation, scalar);
                    break;
                case GemStat.FreezeChance:
                    spec.FreezeChance = FloatOp(spec.FreezeChance, modifier.Operation, scalar);
                    break;
                case GemStat.FreezeDuration:
                    spec.FreezeDuration = FloatOp(spec.FreezeDuration, modifier.Operation, scalar);
                    break;
                case GemStat.PoisonChance:
                    spec.PoisonChance = FloatOp(spec.PoisonChance, modifier.Operation, scalar);
                    break;
                case GemStat.PoisonDuration:
                    spec.PoisonDuration = FloatOp(spec.PoisonDuration, modifier.Operation, scalar);
                    break;
                case GemStat.StunChance:
                    spec.StunChance = FloatOp(spec.StunChance, modifier.Operation, scalar);
                    break;
                case GemStat.StunDuration:
                    spec.StunDuration = FloatOp(spec.StunDuration, modifier.Operation, scalar);
                    break;
                case GemStat.BurningDamage:
                    spec.BurningDamageMultiplier = FloatOp(
                        spec.BurningDamageMultiplier,
                        modifier.Operation,
                        scalar);
                    break;
                case GemStat.PhysAsExtraFire:
                    spec.PhysAsExtraFire = FloatOp(spec.PhysAsExtraFire, modifier.Operation, scalar);
                    break;
                case GemStat.PhysAsExtraCold:
                    spec.PhysAsExtraCold = FloatOp(spec.PhysAsExtraCold, modifier.Operation, scalar);
                    break;
                case GemStat.PhysAsExtraLightning:
                    spec.PhysAsExtraLightning = FloatOp(spec.PhysAsExtraLightning, modifier.Operation, scalar);
                    break;
                case GemStat.PhysAsExtraChaos:
                    spec.PhysAsExtraChaos = FloatOp(spec.PhysAsExtraChaos, modifier.Operation, scalar);
                    break;
                case GemStat.ConvertFireToCold:
                    spec.ConvertFireToCold = FloatOp(spec.ConvertFireToCold, modifier.Operation, scalar);
                    break;
                case GemStat.ConvertColdToLightning:
                    spec.ConvertColdToLightning = FloatOp(spec.ConvertColdToLightning, modifier.Operation, scalar);
                    break;
                case GemStat.ConvertLightningToPhysical:
                    spec.ConvertLightningToPhysical = FloatOp(
                        spec.ConvertLightningToPhysical,
                        modifier.Operation,
                        scalar);
                    break;
                case GemStat.ConvertFireToChaos:
                    spec.ConvertFireToChaos = FloatOp(spec.ConvertFireToChaos, modifier.Operation, scalar);
                    break;
                case GemStat.ConvertLightningToChaos:
                    spec.ConvertLightningToChaos = FloatOp(
                        spec.ConvertLightningToChaos,
                        modifier.Operation,
                        scalar);
                    break;
                case GemStat.ConvertColdToChaos:
                    spec.ConvertColdToChaos = FloatOp(spec.ConvertColdToChaos, modifier.Operation, scalar);
                    break;
                case GemStat.HallowingFlame:
                    ApplyFlag(ref spec.HallowingFlame, modifier.Operation, scalar);
                    break;
                case GemStat.AilmentDamage:
                    spec.AilmentDamageMultiplier = FloatOp(
                        spec.AilmentDamageMultiplier,
                        modifier.Operation,
                        scalar);
                    break;
                case GemStat.AilmentDuration:
                    spec.AilmentDurationMultiplier = FloatOp(
                        spec.AilmentDurationMultiplier,
                        modifier.Operation,
                        scalar);
                    break;
                case GemStat.AimMode:
                    ApplyAimMode(ref spec, modifier.Operation, scalar);
                    break;
                case GemStat.DeliveryPattern:
                    ApplyDeliveryPattern(ref spec, modifier.Operation, scalar);
                    break;
            }
        }

        static void ApplyDamage(
            ref SkillSpec spec,
            RoleModifierOperation operation,
            float min,
            float max)
        {
            switch (operation)
            {
                case RoleModifierOperation.Set:
                    spec.DamageMin = min;
                    spec.DamageMax = max;
                    spec.Damage = (spec.DamageMin + spec.DamageMax) * 0.5f;
                    break;
                case RoleModifierOperation.Add:
                    spec.DamageMin += min;
                    spec.DamageMax += max;
                    spec.Damage = (spec.DamageMin + spec.DamageMax) * 0.5f;
                    break;
                case RoleModifierOperation.Multiply:
                    spec.DamageMin *= min;
                    spec.DamageMax *= max;
                    spec.Damage = (spec.DamageMin + spec.DamageMax) * 0.5f;
                    break;
            }
        }

        static void ApplyPierce(ref SkillSpec spec, RoleModifierOperation operation, float scalar)
        {
            var amount = Mathf.RoundToInt(scalar);
            switch (operation)
            {
                case RoleModifierOperation.Set:
                    spec.PierceBehavior = PierceMode.Finite;
                    spec.PierceCount = amount > 0 ? amount : 0;
                    break;
                case RoleModifierOperation.Add:
                    spec.AddPierce(amount);
                    break;
                case RoleModifierOperation.Multiply:
                    spec.PierceCount = WholeOp(spec.PierceCount, RoleModifierOperation.Multiply, scalar);
                    break;
            }
        }

        static void ApplyFlag(ref bool flag, RoleModifierOperation operation, float scalar)
        {
            if (operation != RoleModifierOperation.Set)
                return;
            flag = scalar != 0f;
        }

        static void ApplyAimMode(ref SkillSpec spec, RoleModifierOperation operation, float scalar)
        {
            if (operation != RoleModifierOperation.Set)
                return;
            var ordinal = Mathf.RoundToInt(scalar);
            if (ordinal == (int)AimMode.Direct || ordinal == (int)AimMode.Ground)
                spec.AimMode = (AimMode)ordinal;
        }

        static void ApplyDeliveryPattern(ref SkillSpec spec, RoleModifierOperation operation, float scalar)
        {
            if (operation != RoleModifierOperation.Set)
                return;
            var ordinal = Mathf.RoundToInt(scalar);
            if (System.Enum.IsDefined(typeof(DeliveryPattern), ordinal))
                spec.DeliveryPattern = (DeliveryPattern)ordinal;
        }

        static float FloatOp(float current, RoleModifierOperation operation, float operand)
        {
            switch (operation)
            {
                case RoleModifierOperation.Set:
                    return operand;
                case RoleModifierOperation.Add:
                    return current + operand;
                case RoleModifierOperation.Multiply:
                    return current * operand;
                default:
                    return current;
            }
        }

        static int WholeOp(int current, RoleModifierOperation operation, float operand)
        {
            switch (operation)
            {
                case RoleModifierOperation.Set:
                    return Mathf.RoundToInt(operand);
                case RoleModifierOperation.Add:
                    return current + Mathf.RoundToInt(operand);
                case RoleModifierOperation.Multiply:
                    return Mathf.RoundToInt(current * operand);
                default:
                    return current;
            }
        }
    }
}
