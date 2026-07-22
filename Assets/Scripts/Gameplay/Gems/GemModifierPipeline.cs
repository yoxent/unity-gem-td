using System.Collections.Generic;
using GemTD.Gameplay.Combat;

namespace GemTD.Gameplay.Gems
{
    public sealed class GemModifierPipeline
    {
        public AttackSpec Apply(AttackSpec baseline, IReadOnlyList<IAttackModifier> modifiers)
        {
            var spec = baseline;
            if (modifiers == null || modifiers.Count == 0)
                return spec;

            for (var i = 0; i < modifiers.Count; i++)
            {
                var mod = modifiers[i];
                if (mod != null)
                    spec = mod.Modify(spec);
            }

            return spec;
        }
    }
}
