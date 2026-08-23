using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    [CreateAssetMenu(menuName = "Gem TD/Tower Roles/Mine", fileName = "Role_Mine_")]
    public sealed class MineRoleDefinition : DamageRoleDefinition
    {
        public float CastTime = 0.75f;
        public float CastSpeed = 100f;
        [Tooltip("Metres. Targeting range for this mine role.")]
        public float TowerRadius = 3.5f;

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
