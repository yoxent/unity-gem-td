using System;

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
        ReservationPercent
    }

    public enum RoleModifierOperation
    {
        Set,
        Add,
        Multiply
    }

    [Serializable]
    public struct RoleStatModifier
    {
        public RoleStat Stat;
        public RoleModifierOperation Operation;
        public float Value;
    }

    [Serializable]
    public sealed class RoleLevelDefinition
    {
        public int SourceLevel;
        public RoleStatModifier[] Modifiers;
    }
}
