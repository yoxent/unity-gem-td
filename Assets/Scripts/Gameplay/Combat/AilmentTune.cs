namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// Authored ailment rows from gems. Chance is 0–1. Duration 0 = runtime default.
    /// Effect 0 = 1 (no extra chill/shock strength). Flags are 100% apply.
    /// </summary>
    public struct AilmentTune
    {
        public bool Ignite;
        public bool Chill;
        public bool Shock;
        public float IgniteChance;
        public float IgniteDuration;
        public float ChillEffect;
        public float ChillDuration;
        public float ShockChance;
        public float ShockEffect;
        public float ShockDuration;
        public float BleedChance;
        public float BleedDamageMultiplier;
        public float BleedDuration;
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
        public bool HallowingFlame;
        public float AilmentDamageMultiplier;
        public float AilmentDurationMultiplier;

        public static AilmentTune FromSkillSpec(SkillSpec spec)
        {
            return new AilmentTune
            {
                Ignite = spec.Ignite,
                Chill = spec.Chill,
                Shock = spec.Shock,
                IgniteChance = spec.IgniteChance,
                IgniteDuration = spec.IgniteDuration,
                ChillEffect = spec.ChillEffect,
                ChillDuration = spec.ChillDuration,
                ShockChance = spec.ShockChance,
                ShockEffect = spec.ShockEffect,
                ShockDuration = spec.ShockDuration,
                BleedChance = spec.BleedChance,
                BleedDamageMultiplier = spec.BleedDamageMultiplier,
                BleedDuration = spec.BleedDuration,
                FreezeChance = spec.FreezeChance,
                FreezeDuration = spec.FreezeDuration,
                PoisonChance = spec.PoisonChance,
                PoisonDuration = spec.PoisonDuration,
                StunChance = spec.StunChance,
                StunDuration = spec.StunDuration,
                BurningDamageMultiplier = spec.BurningDamageMultiplier,
                PhysAsExtraFire = spec.PhysAsExtraFire,
                PhysAsExtraCold = spec.PhysAsExtraCold,
                PhysAsExtraLightning = spec.PhysAsExtraLightning,
                PhysAsExtraChaos = spec.PhysAsExtraChaos,
                HallowingFlame = spec.HallowingFlame,
                AilmentDamageMultiplier = spec.AilmentDamageMultiplier == 0f
                    ? 1f
                    : spec.AilmentDamageMultiplier,
                AilmentDurationMultiplier = spec.AilmentDurationMultiplier == 0f
                    ? 1f
                    : spec.AilmentDurationMultiplier
            };
        }
    }
}
