using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Skill-gem bucket payload attached to a <see cref="TowerDefinition"/>.
    /// Assign only the role this tower uses — attack towers do not carry spell/aura data.
    /// </summary>
    public abstract class TowerRoleDefinition : ScriptableObject
    {
        [Tooltip("Base and level-independent modifiers. Constants belong here; selected-level scaling is stored in Levels.")]
        public RoleStatModifier[] Modifiers;

        [Tooltip("Base and level-independent effect payloads. Constants belong here; selected-level scaling is stored in Levels[].Effects.")]
        public RoleEffectModifier[] Effects;

        [Tooltip("Only stats and effects that scale at a selected source level. SourceLevel values are the relabelled 1-10 levels.")]
        public RoleLevelDefinition[] Levels;

        [Tooltip("Secondary effects emitted after the primary delivery resolves (magma fountains, aftershocks, etc.).")]
        public EffectPayloadDefinition[] EffectPayloads;

        public abstract float BaseFireInterval { get; }

        /// <summary>True = Faster Attacks / attack speed. False = Faster Casting / cast speed.</summary>
        public abstract bool UsesAttackSpeed { get; }

        public virtual float GetTowerRadius(int sourceLevel)
        {
            return ResolveStat(RoleStat.TowerRadius, sourceLevel);
        }

        public virtual float GetBaseFireInterval(int sourceLevel)
        {
            return BaseFireInterval;
        }

        public float ResolveStat(RoleStat stat, int sourceLevel)
        {
            return ResolveStatValue(stat, sourceLevel).Scalar;
        }

        public int GetProjectileCount(int sourceLevel)
        {
            if (!ValidateProjectileCountModifiers(Modifiers, "Modifiers"))
                return 0;

            if (Levels != null)
            {
                for (var i = 0; i < Levels.Length; i++)
                {
                    var level = Levels[i];
                    if (level != null
                        && !ValidateProjectileCountModifiers(
                            level.Modifiers,
                            $"Levels[{level.SourceLevel}].Modifiers"))
                    {
                        return 0;
                    }
                }
            }

            var value = ResolveStat(RoleStat.ProjectileCount, sourceLevel);
            if (value <= 0f)
                return 0;

            if (!IsWholeNumber(value))
            {
                Debug.LogError(
                    $"Role '{name}' resolved ProjectileCount to {value}, but projectile count must be a whole number.");
                return 0;
            }

            return (int)value;
        }

        public RoleStatValue ResolveStatValue(RoleStat stat, int sourceLevel)
        {
            var value = RoleStatValue.FromSingle(GetBaseStat(stat));
            ApplyStatOperationGroups(ref value, stat, Modifiers);
            var level = FindLevel(sourceLevel);
            if (level != null)
                ApplyStatOperationGroups(ref value, stat, level.Modifiers);
            return ClampStat(stat, value);
        }

        public float ResolveEffect(RoleEffectKind kind, int sourceLevel)
        {
            return ResolveEffectValue(kind, sourceLevel).Scalar;
        }

        public RoleStatValue ResolveEffectValue(RoleEffectKind kind, int sourceLevel)
        {
            var value = RoleStatValue.FromSingle(0f);
            ApplyEffectOperationGroups(ref value, kind, Effects);
            var level = FindLevel(sourceLevel);
            if (level != null)
                ApplyEffectOperationGroups(ref value, kind, level.Effects);
            return ClampEffect(kind, value);
        }

        protected virtual float GetBaseStat(RoleStat stat)
        {
            if (stat == RoleStat.ProjectileSpeed)
                return 1f;
            return 0f;
        }

        protected virtual RoleStatValue ClampStat(RoleStat stat, RoleStatValue value)
        {
            value.Min = ClampComponent(stat, value.Min);
            value.Max = ClampComponent(stat, value.Max);
            if (value.Max < value.Min)
            {
                var swap = value.Min;
                value.Min = value.Max;
                value.Max = swap;
            }

            return value;
        }

        static float ClampComponent(RoleStat stat, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 0f;

            switch (stat)
            {
                case RoleStat.AttackSpeed:
                case RoleStat.CastSpeed:
                case RoleStat.ProjectileSpeed:
                    return Mathf.Max(0.01f, value);
                case RoleStat.ReservationPercent:
                    return Mathf.Clamp(value, 0f, 100f);
                default:
                    return Mathf.Max(0f, value);
            }
        }

        RoleLevelDefinition FindLevel(int sourceLevel)
        {
            if (Levels == null)
                return null;

            RoleLevelDefinition selected = null;
            for (var i = 0; i < Levels.Length; i++)
            {
                var candidate = Levels[i];
                if (candidate == null)
                    continue;

                if (selected == null
                    || IsBetterLevel(candidate.SourceLevel, selected.SourceLevel, sourceLevel))
                {
                    selected = candidate;
                }
            }

            return selected;
        }

        static bool IsBetterLevel(int candidate, int selected, int requested)
        {
            var candidateIsAtOrBelow = candidate <= requested;
            var selectedIsAtOrBelow = selected <= requested;
            if (candidateIsAtOrBelow != selectedIsAtOrBelow)
                return candidateIsAtOrBelow;

            if (candidateIsAtOrBelow)
                return candidate > selected;

            return candidate < selected;
        }

        protected virtual RoleStatValue ClampEffect(RoleEffectKind kind, RoleStatValue value)
        {
            value.Min = ClampEffectComponent(kind, value.Min);
            value.Max = ClampEffectComponent(kind, value.Max);
            if (value.Max < value.Min)
            {
                var swap = value.Min;
                value.Min = value.Max;
                value.Max = swap;
            }

            return value;
        }

        static float ClampEffectComponent(RoleEffectKind kind, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 0f;

            switch (kind)
            {
                case RoleEffectKind.AllyOutgoingDamageMultiplier:
                case RoleEffectKind.EnemyMoveSpeedMultiplier:
                case RoleEffectKind.AllyAddedAttackFireDamage:
                case RoleEffectKind.AllyAddedSpellFireDamage:
                case RoleEffectKind.SkillDuration:
                    return Mathf.Max(0f, value);
                case RoleEffectKind.EnemyColdResistance:
                    return Mathf.Clamp(value, -100f, 100f);
                default:
                    return Mathf.Max(0f, value);
            }
        }

        bool ValidateProjectileCountModifiers(RoleStatModifier[] modifiers, string source)
        {
            if (modifiers == null)
                return true;

            var valid = true;
            for (var i = 0; i < modifiers.Length; i++)
            {
                var modifier = modifiers[i];
                if (modifier.Stat != RoleStat.ProjectileCount)
                    continue;

                if (modifier.Operation == RoleModifierOperation.Multiply
                    || modifier.ValueKind == RoleStatValueKind.Range
                    || !IsWholeNumber(modifier.OperandMin)
                    || !IsWholeNumber(modifier.OperandMax))
                {
                    Debug.LogError(
                        $"Role '{name}' has invalid ProjectileCount modifier at {source}[{i}]. "
                        + "Use whole-number Set/Add values only; Multiply and Range are unsupported.");
                    valid = false;
                }
            }

            return valid;
        }

        static bool IsWholeNumber(float value)
        {
            return !float.IsNaN(value)
                && !float.IsInfinity(value)
                && value == Mathf.Floor(value);
        }

        static void ApplyStatOperationGroups(ref RoleStatValue value, RoleStat stat, RoleStatModifier[] modifiers)
        {
            if (modifiers == null)
                return;

            ApplyStatOperation(ref value, stat, modifiers, RoleModifierOperation.Set);
            ApplyStatOperation(ref value, stat, modifiers, RoleModifierOperation.Add);
            if (stat != RoleStat.ProjectileCount)
                ApplyStatOperation(ref value, stat, modifiers, RoleModifierOperation.Multiply);
        }

        static void ApplyStatOperation(
            ref RoleStatValue value,
            RoleStat stat,
            RoleStatModifier[] modifiers,
            RoleModifierOperation operation)
        {
            for (var i = 0; i < modifiers.Length; i++)
            {
                var modifier = modifiers[i];
                if (modifier.Stat != stat || modifier.Operation != operation)
                    continue;

                ApplyOperand(ref value, operation, modifier.OperandMin, modifier.OperandMax, modifier.ValueKind);
            }
        }

        static void ApplyEffectOperationGroups(
            ref RoleStatValue value,
            RoleEffectKind kind,
            RoleEffectModifier[] effects)
        {
            if (effects == null)
                return;

            ApplyEffectOperation(ref value, kind, effects, RoleModifierOperation.Set);
            ApplyEffectOperation(ref value, kind, effects, RoleModifierOperation.Add);
            ApplyEffectOperation(ref value, kind, effects, RoleModifierOperation.Multiply);
        }

        static void ApplyEffectOperation(
            ref RoleStatValue value,
            RoleEffectKind kind,
            RoleEffectModifier[] effects,
            RoleModifierOperation operation)
        {
            for (var i = 0; i < effects.Length; i++)
            {
                var effect = effects[i];
                if (effect.Kind != kind || effect.Operation != operation)
                    continue;

                ApplyOperand(ref value, operation, effect.OperandMin, effect.OperandMax, effect.ValueKind);
            }
        }

        static void ApplyOperand(
            ref RoleStatValue value,
            RoleModifierOperation operation,
            float min,
            float max,
            RoleStatValueKind valueKind)
        {
            if (valueKind == RoleStatValueKind.Range && max < min)
            {
                var swap = min;
                min = max;
                max = swap;
            }

            switch (operation)
            {
                case RoleModifierOperation.Set:
                    value.Min = min;
                    value.Max = max;
                    break;
                case RoleModifierOperation.Add:
                    value.Min += min;
                    value.Max += max;
                    break;
                case RoleModifierOperation.Multiply:
                    value.Min *= min;
                    value.Max *= max;
                    break;
            }
        }
    }
}
