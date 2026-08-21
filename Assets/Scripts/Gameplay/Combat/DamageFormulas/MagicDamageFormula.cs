using UnityEngine;

namespace GemTD.Gameplay.Combat.DamageFormulas
{
    /// <summary>
    /// Magic / elemental damage: reduced by a fractional resistance.
    /// extraParams[0] (optional) — scalar multiplier (e.g. gem power bonus). Defaults to 1.
    ///
    /// Formula: max(0, Damage * (1 - ElementalResistance) * multiplier)
    ///
    /// This formula is intentionally element-agnostic at the code level.
    /// Individual elements (Fire, Ice, Lightning, …) get their own SO asset that
    /// sets default resistance interaction in data (e.g. via a future ResistanceTable SO).
    /// </summary>
    [CreateAssetMenu(menuName = "GemTD/DamageFormula/Magic", fileName = "DmgFormula_Magic")]
    public sealed class MagicDamageFormula : DamageFormulaSO
    {
        public override float Calculate(in AttackerStats attacker, in DefenderStats target,
                                        System.ReadOnlySpan<float> extraParams)
        {
            float multiplier = extraParams.Length > 0 ? extraParams[0] : 1f;
            float resist = Mathf.Clamp01(target.ElementalResistance);
            return Mathf.Max(0f, attacker.Damage * (1f - resist) * multiplier);
        }
    }
}
