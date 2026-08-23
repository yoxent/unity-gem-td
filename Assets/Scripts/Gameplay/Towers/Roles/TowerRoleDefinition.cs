using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Skill-gem bucket payload attached to a <see cref="TowerDefinition"/>.
    /// Assign only the role this tower uses — attack towers do not carry spell/aura data.
    /// </summary>
    public abstract class TowerRoleDefinition : ScriptableObject
    {
        [Tooltip("Always-on modifiers applied after the selected source level.")]
        public RoleStatModifier[] Modifiers;

        [Tooltip("Crawled source-level values. Keep the source level key unchanged.")]
        public RoleLevelDefinition[] Levels;

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
            var value = GetBaseStat(stat);
            var level = FindLevel(sourceLevel);
            if (level != null)
                ApplyOperationGroups(ref value, stat, level.Modifiers);
            ApplyOperationGroups(ref value, stat, Modifiers);
            return ClampStat(stat, value);
        }

        protected virtual float GetBaseStat(RoleStat stat)
        {
            return 0f;
        }

        protected virtual float ClampStat(RoleStat stat, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 0f;

            switch (stat)
            {
                case RoleStat.AttackSpeed:
                case RoleStat.CastSpeed:
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

        static void ApplyOperationGroups(ref float value, RoleStat stat, RoleStatModifier[] modifiers)
        {
            if (modifiers == null)
                return;

            ApplyOperation(ref value, stat, modifiers, RoleModifierOperation.Set);
            ApplyOperation(ref value, stat, modifiers, RoleModifierOperation.Add);
            ApplyOperation(ref value, stat, modifiers, RoleModifierOperation.Multiply);
        }

        static void ApplyOperation(
            ref float value,
            RoleStat stat,
            RoleStatModifier[] modifiers,
            RoleModifierOperation operation)
        {
            for (var i = 0; i < modifiers.Length; i++)
            {
                var modifier = modifiers[i];
                if (modifier.Stat != stat || modifier.Operation != operation)
                    continue;

                switch (operation)
                {
                    case RoleModifierOperation.Set:
                        value = modifier.Value;
                        break;
                    case RoleModifierOperation.Add:
                        value += modifier.Value;
                        break;
                    case RoleModifierOperation.Multiply:
                        value *= modifier.Value;
                        break;
                }
            }
        }
    }
}
