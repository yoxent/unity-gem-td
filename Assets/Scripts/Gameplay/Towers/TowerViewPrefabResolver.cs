using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Maps a tower definition to the KayKit family prefab used by the run and Skill Lab.
    /// </summary>
    public static class TowerViewPrefabResolver
    {
        public static TowerView Resolve(
            TowerDefinition def,
            TowerView fallback,
            TowerView aura,
            TowerView curse,
            TowerView slam,
            TowerView strike,
            TowerView bow,
            TowerView attack,
            TowerView spell)
        {
            if (def != null && def.ViewPrefab != null)
                return def.ViewPrefab;

            switch (SkillGemTowerMap.ResolveVisualFamily(def))
            {
                case SkillGemTowerMap.TowerVisualFamily.Aura:
                    return First(aura, fallback);
                case SkillGemTowerMap.TowerVisualFamily.Curse:
                    return First(curse, fallback);
                case SkillGemTowerMap.TowerVisualFamily.Slam:
                    return First(slam, fallback);
                case SkillGemTowerMap.TowerVisualFamily.Strike:
                    return First(strike, fallback);
                case SkillGemTowerMap.TowerVisualFamily.Bow:
                    return First(bow, fallback);
                case SkillGemTowerMap.TowerVisualFamily.Attack:
                    return First(attack, fallback);
                case SkillGemTowerMap.TowerVisualFamily.Spell:
                    return First(spell, fallback);
                default:
                    return fallback;
            }
        }

        static TowerView First(TowerView preferred, TowerView fallback)
        {
            return preferred != null ? preferred : fallback;
        }
    }
}
