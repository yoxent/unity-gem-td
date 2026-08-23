using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    [CreateAssetMenu(menuName = "Gem TD/Tower Roles/Spell", fileName = "Role_Spell_")]
    public sealed class SpellRoleDefinition : DamageRoleDefinition
    {
        [Tooltip("Seconds per cast before cast speed. PoE cast time.")]
        public float CastTime = 0.75f;

        [Tooltip("Percent of cast time. 100 = CastTime as-is.")]
        public float CastSpeed = 100f;

        [Tooltip("Metres. Targeting range for this spell role.")]
        public float TowerRadius = 5f;

        public override float BaseFireInterval =>
            CastTime / Mathf.Max(0.01f, CastSpeed / 100f);

        public override bool UsesAttackSpeed => false;

        public override float GetBaseFireInterval(int sourceLevel)
        {
            var castTime = ResolveStat(RoleStat.CastTime, sourceLevel);
            var castSpeed = ResolveStat(RoleStat.CastSpeed, sourceLevel);
            return castTime / Mathf.Max(0.01f, castSpeed / 100f);
        }

        protected override float GetBaseStat(RoleStat stat)
        {
            switch (stat)
            {
                case RoleStat.CastTime:
                    return CastTime;
                case RoleStat.CastSpeed:
                    return CastSpeed;
                case RoleStat.TowerRadius:
                    return TowerRadius;
                default:
                    return base.GetBaseStat(stat);
            }
        }
    }
}
