using UnityEngine;
using GemTD.Gameplay.Enemies;

namespace GemTD.Gameplay.Run
{
    [CreateAssetMenu(menuName = "Gem TD/Wave Definition", fileName = "Wave_")]
    public sealed class WaveDefinition : ScriptableObject
    {
        public int WaveNumber = 1;
        public EnemyDefinition Enemy;
        public int Count = 10;
        public float SpawnInterval = 0.5f;
    }
}
