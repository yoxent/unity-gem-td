using GemTD.Gameplay.Gems;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Live evolution checks (Hydra Ballista recipe + fire overlay yaw offsets).
    /// </summary>
    public static class EvolutionEvaluator
    {
        static readonly float[] HydraYaws = { -25f, 0f, 25f };
        static readonly float[] HydraLaterals = { -0.4f, 0f, 0.4f };

        public static float[] HydraHeadYawOffsets => HydraYaws;

        /// <summary>World-space lateral spawn offsets (perp to aim) so multi-head stays readable while seeking.</summary>
        public static float[] HydraHeadLateralOffsets => HydraLaterals;

        public static bool IsHydraBallista(TowerRuntime tower)
        {
            if (tower == null || tower.Def == null)
                return false;

            if (tower.Def.Kind != TowerKind.Projectile)
                return false;

            if (!tower.Def.AllowsHydraEvolution)
                return false;

            var hasLmp = false;
            var hasChain = false;
            var hasFork = false;
            var sockets = tower.Sockets;
            if (sockets == null)
                return false;

            for (var i = 0; i < sockets.Length; i++)
            {
                var gem = sockets[i];
                if (gem == null)
                    continue;

                if (gem.Id == GemId.Lmp)
                    hasLmp = true;
                else if (gem.Id == GemId.Chain)
                    hasChain = true;
                else if (gem.Id == GemId.Fork)
                    hasFork = true;
            }

            return hasLmp && hasChain && hasFork;
        }
    }
}
