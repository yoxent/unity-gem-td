using UnityEngine;
using GemTD.Gameplay.Gems;

namespace GemTD.Gameplay.Towers
{
    public enum TowerKind
    {
        Projectile = 0,
        Splash = 1,
        Aura = 2,
    }

    [CreateAssetMenu(menuName = "Gem TD/Tower Definition", fileName = "Tower_")]
    public sealed class TowerDefinition : ScriptableObject
    {
        public string DisplayName = "Tower";
        public TowerKind Kind = TowerKind.Projectile;
        public int Cost = 50;
        [Tooltip("Added per same-type tower already on map. placeCost = Cost + BuildIncrement × countOnMap")]
        public int BuildIncrement = 25;
        public float Range = 5f;
        public float Damage = 10f;
        public float AttackInterval = 1f;
        public float SplashRadius;
        public float AllyAuraRadius = 3f;
        public float AllyDamageMultiplier = 1.25f;
        public float EnemyAuraRadius = 3f;
        public float EnemySlowMultiplier = 0.7f;
        public int SocketCount = 3;
        public bool AllowsHydraEvolution;
        [Tooltip("PoE-style tags. None = infer from Kind (Ballista Attack+Projectile, Cannon +AoE, Beacon Aura).")]
        public AttackTag Tags = AttackTag.None;
    }
}
