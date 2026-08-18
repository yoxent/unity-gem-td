using System.Collections.Generic;
using GemTD.Gameplay.Grid;
using UnityEngine;

namespace GemTD.Gameplay.Map
{
    public static class StartLayoutBuilder
    {
        const int CorridorLength = 4;

        public static void Build(ChunkGrid grid, ChunkStampService stamp, PathGraph path,
            GridBoard board, IChunkCatalog catalog, MapChunkStamp landPrefab, System.Random rng)
        {
            var keep = new Vector2Int(grid.ChunksW / 2, grid.ChunksH / 2);

            // Keep (Land, yaw 0).
            var keepRes = stamp.StampTentative(keep, landPrefab, 0, path, board);
            stamp.Commit(keep, landPrefab, 0, keepRes.Mask, grid);

            var dir = RandomCardinal(rng);
            var offset = OffsetFor(dir);
            var requiredEdge = dir.Opposite();

            Vector2Int firstCoord = keep;
            for (var i = 1; i <= CorridorLength; i++)
            {
                var coord = new Vector2Int(keep.x + offset.x * i, keep.y + offset.y * i);
                if (!grid.InBounds(coord.x, coord.y)) break;

                var straight = PickStraight(catalog, rng);
                var yaw = PickYawForEdge(straight, requiredEdge, rng);
                var res = stamp.StampTentative(coord, straight, yaw, path, board);
                stamp.Commit(coord, straight, yaw, res.Mask, grid);

                if (i == 1) firstCoord = coord;
            }

            // Home = gate-sink = middle cell of the first Straight's requiredEdge (touching the keep).
            var home = EdgeMiddleCell(firstCoord, requiredEdge);
            path.SetHome(home.x, home.y);
        }

        static EdgeFlags RandomCardinal(System.Random rng)
        {
            switch (rng.Next(4))
            {
                case 0: return EdgeFlags.North;
                case 1: return EdgeFlags.East;
                case 2: return EdgeFlags.South;
                default: return EdgeFlags.West;
            }
        }

        static Vector2Int OffsetFor(EdgeFlags dir)
        {
            switch (dir)
            {
                case EdgeFlags.North: return new Vector2Int(0, 1);
                case EdgeFlags.South: return new Vector2Int(0, -1);
                case EdgeFlags.East:  return new Vector2Int(1, 0);
                case EdgeFlags.West:  return new Vector2Int(-1, 0);
                default: return Vector2Int.zero;
            }
        }

        // Picks a Straight prefab from the catalog's flat snapshot (CopyAll), filtering by
        // ChunkType.Straight. Honors the Task 3 decision that IChunkCatalog
        // exposes only CopyAll (no per-bucket properties on the interface).
        static MapChunkStamp PickStraight(IChunkCatalog catalog, System.Random rng)
        {
            var all = new List<MapChunkStamp>(16);
            catalog.CopyAll(all);
            var straights = new List<MapChunkStamp>(all.Count);
            for (var i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].GetMask().Type == ChunkType.Straight)
                    straights.Add(all[i]);
            var count = straights.Count;
            if (count == 0) return null;
            for (;;)
            {
                var pick = straights[rng.Next(count)];
                if (pick != null) return pick;
            }
        }

        static int PickYawForEdge(MapChunkStamp prefab, EdgeFlags requiredEdge, System.Random rng)
        {
            // Collect valid yaws, pick one at random.
            var baseMask = prefab.GetMask();
            var valid = new int[4];
            var n = 0;
            for (var yaw = 0; yaw < 4; yaw++)
                if ((baseMask.Rotated(yaw).OpenEdges & requiredEdge) != 0)
                    valid[n++] = yaw;
            if (n == 0) return 0; // shouldn't happen for a Straight facing any edge
            return valid[rng.Next(n)];
        }

        static Vector2Int EdgeMiddleCell(Vector2Int chunkCoord, EdgeFlags edge)
        {
            int lx, ly;
            switch (edge)
            {
                case EdgeFlags.North: lx = 2; ly = 4; break;
                case EdgeFlags.South: lx = 2; ly = 0; break;
                case EdgeFlags.West:  lx = 0; ly = 2; break;
                case EdgeFlags.East:  lx = 4; ly = 2; break;
                default: lx = 2; ly = 2; break;
            }
            return new Vector2Int(chunkCoord.x * ChunkMask.Size + lx, chunkCoord.y * ChunkMask.Size + ly);
        }
    }
}
