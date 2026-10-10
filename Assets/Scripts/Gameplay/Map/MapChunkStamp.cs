using UnityEngine;

namespace GemTD.Gameplay.Map
{
    /// <summary>Thin serialized wrapper over a ChunkMask. Builds Size² child tile visuals.</summary>
    public sealed class MapChunkStamp : MonoBehaviour
    {
        [SerializeField] bool[] isPath = new bool[ChunkMask.CellCount];
        [SerializeField] bool[] elevationLocked = new bool[ChunkMask.CellCount];
        [SerializeField] int homeIndex = -1;

        ChunkMask _cachedMask;
        bool _hasCachedMask;
        ChunkMask[] _rotatedMasks;

        public ChunkMask GetMask()
        {
            if (!_hasCachedMask)
            {
                _cachedMask = new ChunkMask(isPath, homeIndex, elevationLocked);
                _hasCachedMask = true;
            }
            return _cachedMask;
        }

        public ChunkMask GetRotatedMask(int yaw)
        {
            var turns = ((yaw % 4) + 4) % 4;
            if (_rotatedMasks == null)
            {
                var baseMask = GetMask();
                _rotatedMasks = new ChunkMask[4];
                _rotatedMasks[0] = baseMask;
                for (var t = 1; t < 4; t++)
                    _rotatedMasks[t] = baseMask.Rotated(t);
            }
            return _rotatedMasks[turns];
        }

        public void ApplyMask(ChunkMask mask)
        {
            isPath = new bool[ChunkMask.CellCount];
            elevationLocked = new bool[ChunkMask.CellCount];
            for (var i = 0; i < ChunkMask.CellCount; i++)
                isPath[i] = mask.IsPath(i % ChunkMask.Size, i / ChunkMask.Size);
            mask.CopyElevationLocked(elevationLocked);
            homeIndex = mask.HasHome
                ? mask.HomeLocal.y * ChunkMask.Size + mask.HomeLocal.x
                : -1;
            InvalidateMaskCache();
        }

        void InvalidateMaskCache()
        {
            _hasCachedMask = false;
            _rotatedMasks = null;
        }

#if UNITY_EDITOR
        void OnValidate() => InvalidateMaskCache();
#endif

        public void BuildVisuals(Material pathMat, Material towerMat, float cellSize,
            Material homeMat = null, Material elevationLockMat = null)
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            var half = cellSize * 0.5f;
            for (var y = 0; y < ChunkMask.Size; y++)
            {
                for (var x = 0; x < ChunkMask.Size; x++)
                {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Tile_{x}_{y}";
                    tile.transform.SetParent(transform, false);
                    tile.transform.localPosition = new Vector3(x * cellSize + half, 0f, y * cellSize + half);
                    tile.transform.localScale = new Vector3(cellSize * 0.95f, 0.1f, cellSize * 0.95f);
                    var col = tile.GetComponent<Collider>();
                    if (col != null) DestroyImmediate(col);
                    var r = tile.GetComponent<MeshRenderer>();
                    var idx = y * ChunkMask.Size + x;
                    var isHome = idx == homeIndex;
                    var path = isPath[idx];
                    var locked = elevationLocked != null && idx < elevationLocked.Length && elevationLocked[idx];
                    if (r != null)
                    {
                        if (isHome && homeMat != null) r.sharedMaterial = homeMat;
                        else if (!path && locked && elevationLockMat != null) r.sharedMaterial = elevationLockMat;
                        else r.sharedMaterial = path ? pathMat : towerMat;
                    }
                }
            }
        }
    }
}
