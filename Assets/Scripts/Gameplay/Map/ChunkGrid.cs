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
        static readonly EdgeFlags[] Cardinals =
        {
            EdgeFlags.North, EdgeFlags.East, EdgeFlags.South, EdgeFlags.West
        };

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

        public bool TryGetOpeningInto(Vector2Int empty, out Vector2Int occupied, out EdgeFlags outward)
        {
            occupied = empty;
            outward = EdgeFlags.None;
            var dirs = Cardinals;
            for (var i = 0; i < dirs.Length; i++)
            {
                var towardOccupied = dirs[i];
                var nb = NeighborCoord(empty, towardOccupied);
                var towardEmpty = towardOccupied.Opposite();
                if (!OpenEdgeAt(nb.x, nb.y, towardEmpty))
                    continue;
                occupied = nb;
                outward = towardEmpty;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Every occupied neighbor that opens into <paramref name="empty"/>.
        /// Marker seating uses this together with legal-slot checks (split
        /// arms that already face the same empty cell are not legal expands).
        /// </summary>
        public int CollectOpeningsInto(Vector2Int empty, List<Vector2Int> occupiedOut, List<EdgeFlags> outwardOut)
        {
            occupiedOut.Clear();
            outwardOut.Clear();
            var dirs = Cardinals;
            for (var i = 0; i < dirs.Length; i++)
            {
                var towardOccupied = dirs[i];
                var nb = NeighborCoord(empty, towardOccupied);
                var towardEmpty = towardOccupied.Opposite();
                if (!OpenEdgeAt(nb.x, nb.y, towardEmpty))
                    continue;
                occupiedOut.Add(nb);
                outwardOut.Add(towardEmpty);
            }
            return occupiedOut.Count;
        }
    }
}
