using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>Ordered build-bar tower types for a run. Static authoring asset.</summary>
    [CreateAssetMenu(menuName = "Gem TD/Build Bar Catalog", fileName = "BuildBarCatalog")]
    public sealed class BuildBarCatalog : ScriptableObject
    {
        public TowerDefinition[] Towers;

        public int Count => Towers != null ? Towers.Length : 0;

        public bool TryGet(int index, out TowerDefinition def)
        {
            def = null;
            if (Towers == null || index < 0 || index >= Towers.Length)
                return false;
            def = Towers[index];
            return def != null;
        }
    }
}
