using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    [CreateAssetMenu(menuName = "Gem TD/Tower Roles/Attack", fileName = "Role_Attack_")]
    public sealed class AttackRoleDefinition : DamageRoleDefinition
    {
        public override float BaseFireInterval =>
            GetBaseFireInterval(TowerInstance.DefaultLevel);

        public override bool UsesAttackSpeed => true;

        public override float GetBaseFireInterval(int sourceLevel)
        {
            var attackTime = ResolveStat(RoleStat.AttackTime, sourceLevel);
            var attackSpeed = ResolveStat(RoleStat.AttackSpeed, sourceLevel);
            return attackTime / Mathf.Max(0.01f, attackSpeed / 100f);
        }
    }
}
