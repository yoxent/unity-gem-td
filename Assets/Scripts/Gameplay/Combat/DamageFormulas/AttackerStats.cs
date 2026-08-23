namespace GemTD.Gameplay.Combat.DamageFormulas
{
    /// <summary>
    /// Snapshot of a tower's resolved offensive stats passed to IDamageFormula.
    /// Populated from TowerInstance + AttackSpec after gem modifiers are applied.
    /// Add fields here as new stats are introduced (e.g. SpellPower, CritMultiplier).
    /// </summary>
    public readonly struct AttackerStats
    {
        /// <summary>Base damage after gem modifier pipeline.</summary>
        public readonly float Damage;

        /// <summary>Physical power — drives melee/projectile physical formulas.</summary>
        public readonly float Attack;

        /// <summary>Elemental/spell power — drives fire, ice, lightning, etc. formulas.</summary>
        public readonly float SpellPower;

        /// <summary>0–1 probability of a critical hit (applied by caller before formula if desired).</summary>
        public readonly float CritChance;

        /// <summary>Multiplier applied to Damage on a critical hit.</summary>
        public readonly float CritMultiplier;

        public AttackerStats(float damage, float attack = 0f, float spellPower = 0f,
                             float critChance = 0f, float critMultiplier = 1.5f)
        {
            Damage = damage;
            Attack = attack;
            SpellPower = spellPower;
            CritChance = critChance;
            CritMultiplier = critMultiplier;
        }
    }
}
