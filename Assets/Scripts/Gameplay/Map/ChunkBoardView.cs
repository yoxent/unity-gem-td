using System.Collections.Generic;
using UnityEngine;
using GemTD.Core;

namespace GemTD.Gameplay.Map
{
    /// <summary>Replaces GridBoardView. Listens to ChunkPlaced and instantiates chunk prefabs.</summary>
    public sealed class ChunkBoardView : MonoBehaviour
    {
        [SerializeField] float cellSize = 1f;
        [SerializeField] Transform chunkParent;

        public float CellSize => cellSize;

        ChunkGrid _grid;
        readonly Dictionary<Vector2Int, GameObject> _instances = new Dictionary<Vector2Int, GameObject>(32);

        public void Bind(ChunkGrid grid)
        {
            _grid = grid;
            GameEvents.ChunkPlaced += OnChunkPlaced;
        }

        public void OnChunkPlaced(Vector2Int coord)
        {
            if (_grid == null || !_grid.TryGet(coord.x, coord.y, out var slot)) return;
            if (slot.Prefab == null) return;

            var parent = chunkParent != null ? chunkParent : transform;
            var instance = Instantiate(slot.Prefab, parent);
            instance.transform.localRotation = Quaternion.Euler(0f, slot.Yaw * 90f, 0f);
            instance.transform.localPosition = ChunkInstanceLocalPosition(coord, slot.Yaw, cellSize);
            _instances[coord] = instance.gameObject;
        }

        /// <summary>
        /// Prefab pivot is the SW corner (painter tiles at x+0.5, z+0.5). Mask rotation is
        /// in-place around the chunk center, so the instance must orbit that same center.
        /// </summary>
        public static Vector3 ChunkInstanceLocalPosition(Vector2Int coord, int yaw, float cellSize)
        {
            var size = ChunkMask.Size * cellSize;
            var origin = new Vector3(coord.x * size, 0f, coord.y * size);
            var half = new Vector3(size * 0.5f, 0f, size * 0.5f);
            var rot = Quaternion.Euler(0f, yaw * 90f, 0f);
            return origin + half - rot * half;
        }

        /// <summary>World position of a prefab-local tile center after yaw around the chunk center.</summary>
        public static Vector3 TileWorldAfterYaw(Vector2Int coord, int yaw, int lx, int ly, float cellSize)
        {
            var rot = Quaternion.Euler(0f, yaw * 90f, 0f);
            var local = new Vector3(lx * cellSize + cellSize * 0.5f, 0f, ly * cellSize + cellSize * 0.5f);
            return ChunkInstanceLocalPosition(coord, yaw, cellSize) + rot * local;
        }

        void OnDestroy()
        {
            GameEvents.ChunkPlaced -= OnChunkPlaced;
        }

        public Vector3 CellToWorld(int x, int y) => CellCenterWorld(x, y);
        public Vector3 CellToWorld(Vector2Int cell) => CellCenterWorld(cell.x, cell.y);

        public Vector3 CellCenterWorld(int x, int y)
        {
            var half = cellSize * 0.5f;
            return transform.TransformPoint(new Vector3(x * cellSize + half, 0f, y * cellSize + half));
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            var local = transform.InverseTransformPoint(world);
            var x = Mathf.FloorToInt(local.x / cellSize);
            var y = Mathf.FloorToInt(local.z / cellSize);
            return new Vector2Int(x, y);
        }

        public Vector3 ChunkCenterWorld(Vector2Int coord)
        {
            var halfChunk = ChunkMask.Size * cellSize * 0.5f;
            return transform.TransformPoint(new Vector3(
                coord.x * ChunkMask.Size * cellSize + halfChunk, 0f,
                coord.y * ChunkMask.Size * cellSize + halfChunk));
        }

        public Vector3 ChunkCellWorld(Vector2Int coord, int lx, int ly)
        {
            var wx = coord.x * ChunkMask.Size + lx;
            var wy = coord.y * ChunkMask.Size + ly;
            return CellCenterWorld(wx, wy);
        }

#if UNITY_EDITOR
        void OnValidate() => cellSize = Mathf.Max(0.1f, cellSize);
#endif
    }
}
