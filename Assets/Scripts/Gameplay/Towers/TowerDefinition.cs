using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    [CreateAssetMenu(menuName = "Gem TD/Tower Definition", fileName = "Tower_")]
    public sealed class TowerDefinition : ScriptableObject
    {
        public string DisplayName = "Tower";
        public int Cost = 50;
        public float Range = 5f;
        public float Damage = 10f;
        public float AttackInterval = 1f;
        public int SocketCount = 3;
    }
}
