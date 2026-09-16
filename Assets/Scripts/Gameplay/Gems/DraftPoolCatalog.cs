using UnityEngine;

namespace GemTD.Gameplay.Gems
{
    /// <summary>Ordered draft-offer gem pool for a run. Static authoring asset.</summary>
    [CreateAssetMenu(menuName = "Gem TD/Draft Pool Catalog", fileName = "DraftPoolCatalog")]
    public sealed class DraftPoolCatalog : ScriptableObject
    {
        public GemDefinition[] Gems;

        public int Count => Gems != null ? Gems.Length : 0;

        public GemDefinition[] GetGemsOrEmpty() =>
            Gems != null && Gems.Length > 0 ? Gems : System.Array.Empty<GemDefinition>();
    }
}
