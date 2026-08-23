using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    [CreateAssetMenu(menuName = "Gem TD/Tower Roles/Attack", fileName = "Role_Attack_")]
    public sealed class AttackRoleDefinition : DamageRoleDefinition
    {
        [Tooltip("Seconds per swing before attack speed. PoE attack time.")]
        public float AttackTime = 1f;

        [Tooltip("Percent of attack time. 100 = AttackTime as-is. PoE Cleave is 80.")]
        public float AttackSpeed = 100f;

        [Tooltip("Metres. Targeting range for this attack role.")]
        public float TowerRadius = 5f;

        public override float BaseFireInterval =>
            AttackTime / Mathf.Max(0.01f, AttackSpeed / 100f);

        public override bool UsesAttackSpeed => true;

        public override float GetBaseFireInterval(int sourceLevel)
        {
            var attackTime = ResolveStat(RoleStat.AttackTime, sourceLevel);
            var attackSpeed = ResolveStat(RoleStat.AttackSpeed, sourceLevel);
            return attackTime / Mathf.Max(0.01f, attackSpeed / 100f);
        }

        protected override float GetBaseStat(RoleStat stat)
        {
            switch (stat)
            {
                case RoleStat.AttackTime:
                    return AttackTime;
                case RoleStat.AttackSpeed:
                    return AttackSpeed;
                case RoleStat.TowerRadius:
                    return TowerRadius;
                default:
                    return base.GetBaseStat(stat);
            }
        }
    }
}
