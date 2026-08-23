using System;
using UnityEngine;
using GemTD.Gameplay.Enemies;

namespace GemTD.Gameplay.Run
{
    [Serializable]
    public struct WaveSpawnEntry
    {
        public EnemyDefinition Enemy;
        public int Count;
    }

    [CreateAssetMenu(menuName = "Gem TD/Wave Definition", fileName = "Wave_")]
    public sealed class WaveDefinition : ScriptableObject
    {
        public int WaveNumber = 1;
        public float SpawnInterval = 0.5f;
        public WaveSpawnEntry[] Entries;
        [Tooltip("Ignored. Campaign drafts after every wave except EndWave; Endless never drafts.")]
        public bool OfferDraftAfterClear;
        public bool EndsCampaign;
    }
}
