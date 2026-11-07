using UnityEngine;

namespace GemTD.Grass
{
    public readonly struct GrassSurfacePatch
    {
        public readonly Vector3 LocalCenter;
        public readonly Vector2 HalfExtents;
        public readonly GrassPatchEdges InsetEdges;
        public readonly GrassPatchEdges CliffEdges;
        public readonly int StableKey;

        public GrassSurfacePatch(
            Vector3 localCenter,
            Vector2 halfExtents,
            GrassPatchEdges insetEdges,
            GrassPatchEdges cliffEdges,
            int stableKey)
        {
            LocalCenter = localCenter;
            HalfExtents = halfExtents;
            InsetEdges = insetEdges;
            CliffEdges = cliffEdges;
            StableKey = stableKey;
        }
    }
}
