using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    [CreateAssetMenu(menuName = "Gem TD/Tower Roles/Curse", fileName = "Role_Curse_")]
    public sealed class CurseRoleDefinition : DamageRoleDefinition
    {
        public override float BaseFireInterval =>
            GetBaseFireInterval(TowerInstance.DefaultLevel);

        public override bool UsesAttackSpeed => false;

        public override float GetBaseFireInterval(int sourceLevel)
        {
            var castTime = ResolveStat(RoleStat.CastTime, sourceLevel);
            var castSpeed = ResolveStat(RoleStat.CastSpeed, sourceLevel);
            return castTime / Mathf.Max(0.01f, castSpeed / 100f);
        }
    }
}
