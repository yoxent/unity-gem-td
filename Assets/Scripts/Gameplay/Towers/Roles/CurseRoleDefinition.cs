using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    [CreateAssetMenu(menuName = "Gem TD/Tower Roles/Curse", fileName = "Role_Curse_")]
    public sealed class CurseRoleDefinition : DamageRoleDefinition
    {
        public override float BaseFireInterval => 0f;

        public override bool UsesAttackSpeed => false;

        public override float GetBaseFireInterval(int sourceLevel)
        {
            return 0f;
        }
    }
}
