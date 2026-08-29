namespace GemTD.Gameplay.Combat
{
    public enum PierceMode
    {
        Finite,
        Infinite
    }

    public enum AimMode
    {
        Direct,
        Ground
    }

    public enum DeliveryPattern
    {
        Straight,
        PayloadNova,
        WarpStrike,
        GroundPulse,
        GroundPath,
        CasterNova,
        Rain
    }

    /// <summary>
    /// Mutable skill snapshot after gem modifiers are applied (Attack and Spell towers share this path).
    /// </summary>
    public struct SkillSpec
    {
        public float Damage;
        public float DamageMin;
        public float DamageMax;
        public int ProjectileCount;
        public float SpreadDegrees;
        public int ChainCount;
        public float ChainHopFalloff;
        public int ForkCount;
        public float AoeRadius;
        /// <summary>
        /// Accumulated multiply from AoE support gems. Applies to AoE-tagged payload radii even when
        /// <see cref="AoeRadius"/> is zero on the primary delivery.
        /// </summary>
        public float AoeRadiusMultiplier;
        public float FireRateMultiplier;
        public float AttackSpeedMultiplier;
        public float CastSpeedMultiplier;
        public float RangeMultiplier;
        public float ProjectileSpeedMultiplier;
        public int EchoVolleyCount;
        public float EchoDamageFactor;
        public PierceMode PierceBehavior;
        public int PierceCount;
        public AimMode AimMode;
        public DeliveryPattern DeliveryPattern;
        public bool Ignite;
        public bool Chill;
        public bool Shock;
        public bool Proliferate;
        public float KnockbackChance;
        public float KnockbackDistance;
        public float BleedChance;
        public float BleedDamageMultiplier;
        public float BleedDuration;
        public float IgniteChance;
        public float IgniteDuration;
        public float ChillEffect;
        public float ChillDuration;
        public float ShockChance;
        public float ShockEffect;
        public float ShockDuration;
        public float FreezeChance;
        public float FreezeDuration;
        public float PoisonChance;
        public float PoisonDuration;
        public float StunChance;
        public float StunDuration;
        public float BurningDamageMultiplier;
        public float PhysAsExtraFire;
        public float PhysAsExtraCold;
        public float PhysAsExtraLightning;
        public float PhysAsExtraChaos;
        public float ConvertFireToCold;
        public float ConvertColdToLightning;
        public float ConvertLightningToPhysical;
        public float ConvertFireToChaos;
        public float ConvertLightningToChaos;
        public float ConvertColdToChaos;
        public bool HallowingFlame;
        public float AilmentDamageMultiplier;
        public float AilmentDurationMultiplier;

        public bool Pierce => PierceBehavior == PierceMode.Infinite || PierceCount > 0;

        public static SkillSpec FromBase(float damage, int projectiles = 1, float aoe = 0f, int chainCount = 0)
        {
            return FromBase(damage, damage, projectiles, aoe, chainCount);
        }

        public static SkillSpec FromBase(
            float damageMin,
            float damageMax,
            int projectiles = 1,
            float aoe = 0f,
            int chainCount = 0)
        {
            if (damageMax < damageMin)
            {
                var swap = damageMin;
                damageMin = damageMax;
                damageMax = swap;
            }

            return new SkillSpec
            {
                Damage = (damageMin + damageMax) * 0.5f,
                DamageMin = damageMin,
                DamageMax = damageMax,
                ProjectileCount = projectiles,
                SpreadDegrees = 0f,
                ChainCount = chainCount,
                ChainHopFalloff = 1f,
                ForkCount = 0,
                AoeRadius = aoe,
                AoeRadiusMultiplier = 1f,
                FireRateMultiplier = 1f,
                AttackSpeedMultiplier = 1f,
                CastSpeedMultiplier = 1f,
                RangeMultiplier = 1f,
                ProjectileSpeedMultiplier = 1f,
                EchoVolleyCount = 1,
                EchoDamageFactor = 1f,
                PierceBehavior = PierceMode.Finite,
                PierceCount = 0,
                AimMode = AimMode.Direct,
                DeliveryPattern = DeliveryPattern.Straight,
                Ignite = false,
                Chill = false,
                Shock = false,
                Proliferate = false,
                KnockbackChance = 0f,
                KnockbackDistance = 0f,
                BleedChance = 0f,
                BleedDamageMultiplier = 1f,
                BleedDuration = 0f,
                IgniteChance = 0f,
                IgniteDuration = 0f,
                ChillEffect = 1f,
                ChillDuration = 0f,
                ShockChance = 0f,
                ShockEffect = 1f,
                ShockDuration = 0f,
                FreezeChance = 0f,
                FreezeDuration = 0f,
                PoisonChance = 0f,
                PoisonDuration = 0f,
                StunChance = 0f,
                StunDuration = 0f,
                BurningDamageMultiplier = 1f,
                PhysAsExtraFire = 0f,
                PhysAsExtraCold = 0f,
                PhysAsExtraLightning = 0f,
                PhysAsExtraChaos = 0f,
                ConvertFireToCold = 0f,
                ConvertColdToLightning = 0f,
                ConvertLightningToPhysical = 0f,
                ConvertFireToChaos = 0f,
                ConvertLightningToChaos = 0f,
                ConvertColdToChaos = 0f,
                HallowingFlame = false,
                AilmentDamageMultiplier = 1f,
                AilmentDurationMultiplier = 1f
            };
        }

        public void ScaleDamage(float multiplier)
        {
            DamageMin *= multiplier;
            DamageMax *= multiplier;
            Damage = (DamageMin + DamageMax) * 0.5f;
        }

        public void AddFlatDamage(float amount)
        {
            DamageMin += amount;
            DamageMax += amount;
            Damage = (DamageMin + DamageMax) * 0.5f;
        }

        public void AddPierce(int extraHits)
        {
            if (extraHits <= 0 || PierceBehavior == PierceMode.Infinite)
                return;

            PierceCount += extraHits;
        }

        public int GetPierceRemaining()
        {
            if (PierceBehavior == PierceMode.Infinite)
                return ProjectileRuntime.InfinitePierceRemaining;
            if (PierceCount <= 0)
                return 0;
            return PierceCount;
        }
    }
}
