using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>Named tower pool. Full ingest lives on <c>TowerCatalog</c>; the run and Skill Lab use <c>GameplayReadyTowersCatalog</c>.</summary>
    [CreateAssetMenu(menuName = "Gem TD/Tower Catalog", fileName = "TowerCatalog")]
    public sealed class TowerCatalog : ScriptableObject
    {
        public TowerDefinition[] Towers;

        public int Count => Towers != null ? Towers.Length : 0;

        public TowerDefinition[] GetTowersOrEmpty() =>
            Towers != null && Towers.Length > 0 ? Towers : System.Array.Empty<TowerDefinition>();
    }
}
