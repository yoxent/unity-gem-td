using System.Collections.Generic;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Linear per-type place cost from tower SO + same-type count on map.
    /// placeCost = baseCost + (buildIncrement × sameTypeCountOnMap)
    /// Sell refunds purchaseCost (100%), lowering the next build tier for that type.
    /// </summary>
    public static class TowerCostCalculator
    {
        public static int ComputePlaceCost(TowerDefinition def, IReadOnlyList<TowerRuntime> towers)
        {
            if (def == null)
                return 0;

            var baseCost = def.Cost;
            if (baseCost <= 0)
                return 0;

            var increment = def.BuildIncrement >= 0 ? def.BuildIncrement : 0;
            var sameTypeOnMap = 0;
            for (var i = 0; i < towers.Count; i++)
            {
                if (towers[i]?.Def == def)
                    sameTypeOnMap++;
            }

            return baseCost + increment * sameTypeOnMap;
        }
    }
}
