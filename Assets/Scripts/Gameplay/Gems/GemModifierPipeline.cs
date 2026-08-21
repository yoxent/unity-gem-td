using System;
using System.Collections.Generic;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Towers;

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

        /// <summary>
        /// Live attack spec for a tower (same path Combat uses). <paramref name="scratch"/>
        /// is cleared and filled with socket modifiers — caller owns pooling.
        /// </summary>
        public AttackSpec Resolve(TowerRuntime tower, List<IAttackModifier> scratch)
        {
            if (scratch == null)
                throw new ArgumentNullException(nameof(scratch));

            CollectSocketModifiers(tower, scratch);
            var baseline = tower?.Def == null
                ? AttackSpec.FromBase(0f)
                : AttackSpec.FromBase(tower.Def.Damage, 1, tower.Def.SplashRadius);
            return Apply(baseline, scratch);
        }

        public static void CollectSocketModifiers(TowerRuntime tower, List<IAttackModifier> into)
        {
            into.Clear();
            if (tower?.Sockets == null)
                return;

            var sockets = tower.Sockets;
            for (var i = 0; i < sockets.Length; i++)
            {
                var gem = sockets[i];
                if (gem == null || gem.Id == GemId.None)
                    continue;

                var mod = GemModifierFactory.Create(gem.Id);
                if (mod != null)
                    into.Add(mod);
            }
        }
    }
}
