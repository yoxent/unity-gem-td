using System;
using System.Collections.Generic;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Gems
{
    public sealed class GemModifierPipeline
    {
        public SkillSpec Apply(SkillSpec baseline, IReadOnlyList<ISkillModifier> modifiers)
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
        /// Live skill spec for a tower (same path Combat uses). <paramref name="scratch"/>
        /// is cleared and filled with socket modifiers — caller owns pooling.
        /// </summary>
        public SkillSpec Resolve(TowerInstance tower, List<ISkillModifier> scratch)
        {
            if (scratch == null)
                throw new ArgumentNullException(nameof(scratch));

            CollectSocketModifiers(tower, scratch);
            var baseline = SkillSpec.FromBase(0f, projectiles: 0);
            if (tower?.Def != null)
            {
                var damage = tower.Def.GetDamageRange(tower.Level);
                baseline = SkillSpec.FromBase(
                    damage.Min,
                    damage.Max,
                    tower.Def.GetProjectileCount(tower.Level),
                    tower.Def.GetSplashRadius(tower.Level));
                baseline.ProjectileSpeedMultiplier =
                    tower.Def.GetProjectileSpeedMultiplier(tower.Level);
                baseline.PierceBehavior = tower.Def.GetProjectilePierceMode();
                baseline.AimMode = tower.Def.GetAimMode();
                baseline.DeliveryPattern = tower.Def.GetDeliveryPattern();
            }

            return Apply(baseline, scratch);
        }

        public static void CollectSocketModifiers(TowerInstance tower, List<ISkillModifier> into)
        {
            into.Clear();
            if (tower?.Sockets == null)
                return;

            var sockets = tower.Sockets;
            for (var i = 0; i < sockets.Length; i++)
            {
                var gem = sockets[i];
                if (gem == null)
                    continue;

                into.Add(new GemDefinitionModifier(gem));
            }
        }
    }
}
