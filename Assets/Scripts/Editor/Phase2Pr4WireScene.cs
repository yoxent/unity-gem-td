using UnityEditor;
using UnityEngine;

namespace GemTD.Editor
{
    /// <summary>Obsolete PR4 wire menu — disabled to prevent draft-pool regression.</summary>
    public static class Phase2Pr4WireScene
    {
        [MenuItem("Gem TD/Phase 2 PR4 Wire Draft Pool + Waves (disabled)")]
        public static void Wire()
        {
            Debug.LogError(
                "[PR4 Wire] Obsolete — do not run. Assign DraftPoolCatalog, BuildBarCatalog, and WaveCatalog on " +
                "GameCompositionRoot in Run.unity. Use Gem TD / Phase 2 PR5 Wire Gems + Hydra Seed instead.");
        }

        [MenuItem("Gem TD/Phase 2 PR4 Wire Draft Pool + Waves (disabled)", true)]
        public static bool WireValidate() => false;
    }
}
