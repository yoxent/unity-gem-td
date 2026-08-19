using UnityEngine;

namespace GemTD.Gameplay.Combat.DamageFormulas
{
    /// <summary>
    /// Abstract ScriptableObject base for all damage formulas.
    /// Subclass this and add [CreateAssetMenu] to register a new formula type.
    /// Assign instances to TowerDefinition.DamageFormula via the inspector.
    /// </summary>
    public abstract class DamageFormulaSO : ScriptableObject, IDamageFormula
    {
        public abstract float Calculate(in AttackerStats attacker, in DefenderStats target,
                                        System.ReadOnlySpan<float> extraParams);
    }
}
