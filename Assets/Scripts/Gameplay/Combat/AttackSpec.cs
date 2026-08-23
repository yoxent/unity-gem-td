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
        public float AttackSpeedMultiplier;
        public float CastSpeedMultiplier;
        public float RangeMultiplier;
        public float ProjectileSpeedMultiplier;
        public int EchoVolleyCount;
        public float EchoDamageFactor;
        public bool Pierce;
        public bool Ignite;
        public bool Chill;
        public bool Shock;
        public bool Proliferate;
        public float KnockbackChance;
        public float KnockbackDistance;

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
                AttackSpeedMultiplier = 1f,
                CastSpeedMultiplier = 1f,
                RangeMultiplier = 1f,
                ProjectileSpeedMultiplier = 1f,
                EchoVolleyCount = 1,
                EchoDamageFactor = 1f,
                Pierce = false,
                Ignite = false,
                Chill = false,
                Shock = false,
                Proliferate = false,
                KnockbackChance = 0f,
                KnockbackDistance = 0f
            };
        }
    }
}
