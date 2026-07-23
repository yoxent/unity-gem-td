using UnityEngine;

namespace GemTD.Gameplay.Enemies
{
    [CreateAssetMenu(menuName = "Gem TD/Enemy Definition", fileName = "Enemy_")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public string DisplayName = "Enemy";
        public float MaxHealth = 20f;
        public float MoveSpeed = 2f;
        public int Armor;
        public Material PlaceholderMaterial;
    }
}
