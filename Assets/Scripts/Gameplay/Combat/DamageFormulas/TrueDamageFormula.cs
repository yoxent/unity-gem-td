using UnityEngine;

namespace GemTD.Gameplay.Combat.DamageFormulas
{
    /// <summary>
    /// True damage: ignores armor and elemental resistance entirely.
    /// extraParams[0] (optional) — scalar multiplier. Defaults to 1.
    ///
    /// Formula: max(0, Damage * multiplier)
    ///
    /// Useful for DoT ticks (Ignite, Bleed), execute effects, and any gem that
    /// should bypass defences.
    /// </summary>
    [CreateAssetMenu(menuName = "GemTD/DamageFormula/TrueDamage", fileName = "DmgFormula_TrueDamage")]
    public sealed class TrueDamageFormula : DamageFormulaSO
    {
        public override float Calculate(in AttackerStats attacker, in DefenderStats target,
                                        System.ReadOnlySpan<float> extraParams)
        {
            float multiplier = extraParams.Length > 0 ? extraParams[0] : 1f;
            return Mathf.Max(0f, attacker.Damage * multiplier);
        }
    }
}
