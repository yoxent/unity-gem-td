namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// Mutable attack description after gem modifiers are applied.
    /// </summary>
    public struct AttackSpec
    {
        public float Damage;
        public int ProjectileCount;
        public float SpreadDegrees;
        public int ChainCount;
        public int ForkCount;
        public float AoeRadius;
        public float FireRateMultiplier;
        public bool Pierce;
        public bool Ignite;
        public bool Shock;
        public bool Proliferate;

        public static AttackSpec FromBase(float damage, int projectiles = 1, float aoe = 0f)
        {
            return new AttackSpec
            {
                Damage = damage,
                ProjectileCount = projectiles,
                SpreadDegrees = 0f,
                ChainCount = 0,
                ForkCount = 0,
                AoeRadius = aoe,
                FireRateMultiplier = 1f,
                Pierce = false,
                Ignite = false,
                Shock = false,
                Proliferate = false
            };
        }
    }
}
