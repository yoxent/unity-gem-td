using System;
using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    public enum AuraEffectKind
    {
        AllyOutgoingDamageMultiplier,
        EnemyMoveSpeedMultiplier
    }

    [Serializable]
    public struct AuraEffectModifier
    {
        public AuraEffectKind Kind;
        public float Value;
    }

    [CreateAssetMenu(menuName = "Gem TD/Tower Roles/Aura", fileName = "Role_Aura_")]
    public sealed class AuraRoleDefinition : TowerRoleDefinition
    {
        [Tooltip("Metres. Influence range for this aura role.")]
        public float TowerRadius = 1.5f;

        [Tooltip("Reservation percent if we model upkeep. PoE mana reservation.")]
        public float ReservationPercent = 50f;

        [Tooltip("Typed aura payload reserved for future TD effect activation. Each entry targets one runtime effect.")]
        public AuraEffectModifier[] Effects;

        public override float BaseFireInterval => 0f;

        public override bool UsesAttackSpeed => false;

        protected override float GetBaseStat(RoleStat stat)
        {
            switch (stat)
            {
                case RoleStat.TowerRadius:
                    return TowerRadius;
                case RoleStat.ReservationPercent:
                    return ReservationPercent;
                default:
                    return base.GetBaseStat(stat);
            }
        }
    }
}
