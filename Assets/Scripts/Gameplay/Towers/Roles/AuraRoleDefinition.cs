using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    [CreateAssetMenu(menuName = "Gem TD/Tower Roles/Aura", fileName = "Role_Aura_")]
    public sealed class AuraRoleDefinition : TowerRoleDefinition
    {
        public override float BaseFireInterval => 0f;

        public override bool UsesAttackSpeed => false;
    }
}
