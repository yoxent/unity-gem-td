namespace GemTD.Gameplay.Combat.DamageFormulas
{
    /// <summary>
    /// Snapshot of an enemy's defensive stats passed to IDamageFormula.
    /// Populated from EnemyRuntime at time of projectile hit.
    /// Add resistance fields here as new elements are introduced.
    /// </summary>
    public readonly struct DefenderStats
    {
        /// <summary>Flat physical damage reduction.</summary>
        public readonly float Armor;

        /// <summary>0–1 fractional magic/elemental resistance (0 = no resist, 1 = immune).</summary>
        public readonly float ElementalResistance;

        /// <summary>Current HP — available for executes or threshold-based formulas.</summary>
        public readonly float CurrentHp;

        /// <summary>Max HP — available for percentage-health formulas.</summary>
        public readonly float MaxHp;

        public DefenderStats(float armor = 0f, float elementalResistance = 0f,
                             float currentHp = 1f, float maxHp = 1f)
        {
            Armor = armor;
            ElementalResistance = elementalResistance;
            CurrentHp = currentHp;
            MaxHp = maxHp;
        }
    }
}
