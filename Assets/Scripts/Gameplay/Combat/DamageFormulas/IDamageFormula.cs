namespace GemTD.Gameplay.Combat.DamageFormulas
{
    /// <summary>
    /// Stateless damage calculation contract.
    /// Implement as a ScriptableObject subclass — one SO per formula type.
    /// Towers hold a serialized reference; swapping the SO changes damage behaviour
    /// without touching tower code.
    ///
    /// Pattern inspired by the RMMZ "GlobalDamageFormulas" plugin (FlynnSP):
    ///   named formula registry where attacker stats, defender stats, and extra
    ///   scalar params are passed in at call time.  eval() replaced by explicit
    ///   typed implementations (no reflection, no runtime compilation).
    ///
    /// Adding a new damage type / element:
    ///   1. Create a new class that inherits DamageFormulaSO (or implements this
    ///      directly if you don't need SO serialization).
    ///   2. Add a [CreateAssetMenu] attribute so the SO can be made from the
    ///      Project window.
    ///   3. Assign it to the relevant TowerDefinition.DamageFormula slot.
    ///   No other code changes required.
    /// </summary>
    public interface IDamageFormula
    {
        /// <param name="attacker">Resolved tower stats after gem modifiers.</param>
        /// <param name="target">Live enemy stats at time of hit.</param>
        /// <param name="extraParams">
        ///   Optional per-call scalars (e.g. gem-power multiplier, combo count).
        ///   Index contract is formula-specific; pass an empty span when unused.
        /// </param>
        /// <returns>Final damage value (clamped ≥ 0 by each implementation).</returns>
        float Calculate(in AttackerStats attacker, in DefenderStats target,
                        System.ReadOnlySpan<float> extraParams);
    }
}
