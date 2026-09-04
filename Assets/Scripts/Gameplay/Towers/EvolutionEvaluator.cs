using GemTD.Gameplay.Gems;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Live evolution checks (Hydra recipe + fire overlay yaw offsets).
    /// </summary>
    public static class EvolutionEvaluator
    {
        static readonly float[] HydraYaws = { -25f, 0f, 25f };
        static readonly float[] HydraLaterals = { -0.4f, 0f, 0.4f };

        public static float[] HydraHeadYawOffsets => HydraYaws;

        /// <summary>World-space lateral spawn offsets (perp to aim) so multi-head stays readable while seeking.</summary>
        public static float[] HydraHeadLateralOffsets => HydraLaterals;

        /// <summary>
        /// Hydra is off for this pass (run and Skill Lab). Recipe math remains below for a later re-enable.
        /// </summary>
        public static readonly bool HydraEnabled = false;

        public static bool IsHydraTower(TowerInstance tower)
        {
            if (!HydraEnabled)
                return false;

            if (tower == null || tower.Def == null)
                return false;

            if (!tower.Def.HasRole<AttackRoleDefinition>())
                return false;

            if (!tower.Def.AllowsHydraEvolution)
                return false;

            var hasMultipleProjectiles = false;
            var hasChain = false;
            var hasFork = false;
            var sockets = tower.Sockets;
            if (sockets == null)
                return false;

            for (var i = 0; i < sockets.Length; i++)
            {
                var gem = sockets[i];
                if (gem.IsEmpty)
                    continue;

                if (gem.Id == GemId.MultipleProjectiles)
                    hasMultipleProjectiles = true;
                else if (gem.Id == GemId.Chain)
                    hasChain = true;
                else if (gem.Id == GemId.Fork)
                    hasFork = true;
            }

            return hasMultipleProjectiles && hasChain && hasFork;
        }
    }
}
