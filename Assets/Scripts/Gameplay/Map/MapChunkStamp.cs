using UnityEngine;

namespace GemTD.Gameplay.Map
{
    /// <summary>Thin serialized wrapper over a ChunkMask. Builds 25 child tile visuals.</summary>
    public sealed class MapChunkStamp : MonoBehaviour
    {
        [SerializeField] bool[] isPath = new bool[ChunkMask.CellCount];

        public ChunkMask GetMask() => new ChunkMask(isPath);

        public void ApplyMask(ChunkMask mask)
        {
            isPath = new bool[ChunkMask.CellCount];
            for (var i = 0; i < ChunkMask.CellCount; i++)
                isPath[i] = mask.IsPath(i % ChunkMask.Size, i / ChunkMask.Size);
        }

        public void BuildVisuals(Material pathMat, Material towerMat, float cellSize)
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
                    var path = isPath[y * ChunkMask.Size + x];
                    if (r != null) r.sharedMaterial = path ? pathMat : towerMat;
                }
            }
        }
    }
}
