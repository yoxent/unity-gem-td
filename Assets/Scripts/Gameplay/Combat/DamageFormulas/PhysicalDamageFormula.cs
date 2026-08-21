using UnityEngine;

namespace GemTD.Gameplay.Combat.DamageFormulas
{
    /// <summary>
    /// Physical damage: flat armor subtracted from base damage.
    /// extraParams[0] (optional) — scalar multiplier (e.g. gem power bonus). Defaults to 1.
    ///
    /// Formula: max(0, (Damage - Armor) * multiplier)
    /// </summary>
    [CreateAssetMenu(menuName = "GemTD/DamageFormula/Physical", fileName = "DmgFormula_Physical")]
    public sealed class PhysicalDamageFormula : DamageFormulaSO
    {
        public override float Calculate(in AttackerStats attacker, in DefenderStats target,
                                        System.ReadOnlySpan<float> extraParams)
        {
            float multiplier = extraParams.Length > 0 ? extraParams[0] : 1f;
            return Mathf.Max(0f, (attacker.Damage - target.Armor) * multiplier);
        }
    }
}
