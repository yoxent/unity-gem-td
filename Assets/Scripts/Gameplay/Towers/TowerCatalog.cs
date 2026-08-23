using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>Full skill-gem tower pool for Skill Lab / Codex / future pick. Not the run build bar.</summary>
    [CreateAssetMenu(menuName = "Gem TD/Tower Catalog", fileName = "TowerCatalog")]
    public sealed class TowerCatalog : ScriptableObject
    {
        public TowerDefinition[] Towers;

        public int Count => Towers != null ? Towers.Length : 0;

        public TowerDefinition[] GetTowersOrEmpty() =>
            Towers != null && Towers.Length > 0 ? Towers : System.Array.Empty<TowerDefinition>();
    }
}
