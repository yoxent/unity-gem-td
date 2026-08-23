using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Run
{
    public enum DraftMixKind
    {
        FourTowers = 0,
        TwoGemsOneTowerContested = 1
    }

    /// <summary>Parent draft table: mix recipe plus child gem/tower pools.</summary>
    [CreateAssetMenu(menuName = "Gem TD/Draft Catalog", fileName = "DraftCatalog")]
    public sealed class DraftCatalog : ScriptableObject
    {
        public DraftMixKind Mix = DraftMixKind.TwoGemsOneTowerContested;
        public DraftPoolCatalog GemPool;
        public TowerCatalog TowerPool;
    }
}
