using System.Collections.Generic;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    public static class TargetingService
    {
        public static void Apply(
            TargetingMode mode,
            TargetingApplyScope scope,
            TowerRuntime selected,
            List<TowerRuntime> allTowers)
        {
            if (selected == null || allTowers == null)
                return;

            switch (scope)
            {
                case TargetingApplyScope.ThisTower:
                    selected.TargetingMode = mode;
                    break;

                case TargetingApplyScope.ThisType:
                    var selectedDef = selected.Def;
                    for (var i = 0; i < allTowers.Count; i++)
                    {
                        var tower = allTowers[i];
                        if (tower != null && ReferenceEquals(tower.Def, selectedDef))
                            tower.TargetingMode = mode;
                    }
                    break;

                case TargetingApplyScope.AllTowers:
                    for (var i = 0; i < allTowers.Count; i++)
                    {
                        var tower = allTowers[i];
                        if (tower != null)
                            tower.TargetingMode = mode;
                    }
                    break;
            }
        }
    }
}
