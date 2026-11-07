using System;
using UnityEngine;

namespace GemTD.Gameplay.Map
{
    public static class TileHeightAssigner
    {
        static readonly int[] NeighborDx = { 0, 1, 0, -1 };
        static readonly int[] NeighborDy = { 1, 0, -1, 0 };

        public static void AssignChunk(
            TileHeightMap map,
            ChunkMask mask,
            Vector2Int chunkCoord,
            System.Random rng,
            HeightInfluenceWeights weights)
        {
            if (map == null || rng == null)
                return;

            var eligible = new Vector2Int[ChunkMask.CellCount];
            var eligibleCount = 0;
            var neighbors = new byte[4];

            for (var ly = 0; ly < ChunkMask.Size; ly++)
            {
                for (var lx = 0; lx < ChunkMask.Size; lx++)
                {
                    var wx = chunkCoord.x * ChunkMask.Size + lx;
                    var wy = chunkCoord.y * ChunkMask.Size + ly;
                    if (mask.IsElevationLocked(lx, ly))
                    {
                        map.Set(wx, wy, 0);
                        continue;
                    }

                    eligible[eligibleCount++] = new Vector2Int(lx, ly);
                }
            }

            for (var i = eligibleCount - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var tmp = eligible[i];
                eligible[i] = eligible[j];
                eligible[j] = tmp;
            }

            for (var e = 0; e < eligibleCount; e++)
            {
                var lx = eligible[e].x;
                var ly = eligible[e].y;
                var wx = chunkCoord.x * ChunkMask.Size + lx;
                var wy = chunkCoord.y * ChunkMask.Size + ly;

                var nCount = 0;
                for (var d = 0; d < 4; d++)
                {
                    var nx = wx + NeighborDx[d];
                    var ny = wy + NeighborDy[d];
                    if (!map.Has(nx, ny))
                        continue;
                    neighbors[nCount++] = map.Get(nx, ny);
                }

                byte layer;
                if (nCount == 0)
                {
                    layer = (byte)rng.Next(TileHeightRules.MaxLayer + 1);
                }
                else
                {
                    var influencer = neighbors[rng.Next(nCount)];
                    weights.NormalizeLegal(influencer, out var same, out var up, out _);
                    var u = rng.NextDouble();
                    if (u < same)
                        layer = influencer;
                    else if (u < same + up)
                        layer = (byte)(influencer + 1);
                    else
                        layer = (byte)(influencer - 1);
                }

                map.Set(wx, wy, layer);
            }
        }
    }
}
