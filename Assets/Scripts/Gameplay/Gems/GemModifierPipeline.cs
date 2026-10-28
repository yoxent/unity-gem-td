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
        public SkillSpec ResolveBaseline(TowerInstance tower)
        {
            var baseline = SkillSpec.FromBase(0f, projectiles: 0);
            if (tower?.Def != null)
            {
                var damage = tower.Def.GetDamageRange(tower.Level);
                baseline = SkillSpec.FromBase(
                    damage.Min,
                    damage.Max,
                    tower.Def.GetProjectileCount(tower.Level),
                    tower.Def.GetSplashRadius(tower.Level),
                    tower.Def.GetChainCount(tower.Level),
                    tower.Def.GetForkCount(tower.Level));
                baseline.ProjectileSpeedMultiplier =
                    tower.Def.GetProjectileSpeedMultiplier(tower.Level);
                baseline.PierceBehavior = tower.Def.GetProjectilePierceMode();
                baseline.AimMode = tower.Def.GetAimMode();
                baseline.DeliveryPattern = tower.Def.GetDeliveryPattern();
                var damageRole = tower.Def.FireRole as DamageRoleDefinition;
                if (damageRole != null)
                {
                    DamageMix.ToFractions(
                        damageRole.Mix,
                        out baseline.MixPhysical,
                        out baseline.MixFire,
                        out baseline.MixCold,
                        out baseline.MixLightning,
                        out baseline.MixChaos);
                    baseline.SpreadDegrees = damageRole.SpreadDegrees;
                    baseline.SequentialIntervalSeconds = damageRole.SequentialIntervalSeconds;
                }
            }

            return baseline;
        }

        public SkillSpec Resolve(TowerInstance tower, List<ISkillModifier> scratch)
        {
            if (scratch == null)
                throw new ArgumentNullException(nameof(scratch));

            CollectSocketModifiers(tower, scratch);
            return Apply(ResolveBaseline(tower), scratch);
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
                if (gem.IsEmpty)
                    continue;

                into.Add(new GemDefinitionModifier(gem));
            }
        }

        public static void CollectEffectPayloads(
            TowerInstance tower,
            List<EffectPayloadDefinition> into)
        {
            if (into == null)
                throw new ArgumentNullException(nameof(into));

            into.Clear();
            if (tower == null || tower.Def == null)
                return;

            AppendPayloads(into, tower.Def.GetEffectPayloads());
            var sockets = tower.Sockets;
            for (var i = 0; i < sockets.Length; i++)
            {
                var gem = sockets[i];
                if (gem.IsEmpty || gem.Def.EffectPayloads == null)
                    continue;

                AppendPayloads(into, gem.Def.EffectPayloads);
            }
        }

        static void AppendPayloads(
            List<EffectPayloadDefinition> into,
            IReadOnlyList<EffectPayloadDefinition> payloads)
        {
            if (payloads == null)
                return;

            for (var i = 0; i < payloads.Count; i++)
            {
                if (payloads[i] != null)
                    into.Add(payloads[i]);
            }
        }
    }
}
