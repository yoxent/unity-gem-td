using System.Collections.Generic;
using UnityEngine;

namespace GemTD.Gameplay.Map
{
    public readonly struct ChunkSlot
    {
        public readonly MapChunkStamp Prefab;
        public readonly int Yaw;
        public readonly ChunkMask Mask;

        public ChunkSlot(MapChunkStamp prefab, int yaw, ChunkMask mask)
        {
            Prefab = prefab;
            Yaw = yaw;
            Mask = mask;
        }
    }

    public sealed class ChunkGrid
    {
        public int ChunksW { get; }
        public int ChunksH { get; }
        public int Count => _slots.Count;

        readonly Dictionary<Vector2Int, ChunkSlot> _slots = new Dictionary<Vector2Int, ChunkSlot>(32);

        public ChunkGrid(int chunksW, int chunksH)
        {
            ChunksW = chunksW > 0 ? chunksW : 1;
            ChunksH = chunksH > 0 ? chunksH : 1;
        }

        public bool InBounds(int cx, int cy) =>
            cx >= 0 && cy >= 0 && cx < ChunksW && cy < ChunksH;

        public bool IsOccupied(int cx, int cy) =>
            _slots.ContainsKey(new Vector2Int(cx, cy));

        public bool TryGet(int cx, int cy, out ChunkSlot slot) =>
            _slots.TryGetValue(new Vector2Int(cx, cy), out slot);

        public void Place(Vector2Int coord, ChunkSlot slot) => _slots[coord] = slot;

        public bool OpenEdgeAt(int cx, int cy, EdgeFlags dir)
        {
            if (!TryGet(cx, cy, out var slot)) return false;
            return (slot.Mask.OpenEdges & dir) != 0;
        }

        public Vector2Int NeighborCoord(Vector2Int coord, EdgeFlags dir)
        {
            switch (dir)
            {
                case EdgeFlags.North: return new Vector2Int(coord.x, coord.y + 1);
                case EdgeFlags.South: return new Vector2Int(coord.x, coord.y - 1);
                case EdgeFlags.East:  return new Vector2Int(coord.x + 1, coord.y);
                case EdgeFlags.West:  return new Vector2Int(coord.x - 1, coord.y);
                default: return coord;
            }
        }
    }
}
