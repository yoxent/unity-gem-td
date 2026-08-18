using GemTD.Core;
using GemTD.Gameplay.Grid;
using UnityEngine;

namespace GemTD.Gameplay.Map
{
    public readonly struct StampResult
    {
        public readonly bool[] PrevPath;
        public readonly ChunkMask Mask;

        public StampResult(bool[] prevPath, ChunkMask mask)
        {
            PrevPath = prevPath;
            Mask = mask;
        }
    }

    public sealed class ChunkStampService
    {
        public StampResult StampTentative(Vector2Int coord, MapChunkStamp prefab, int yaw, PathGraph path, GridBoard board)
        {
            var mask = prefab.GetMask().Rotated(yaw);
            var prev = new bool[ChunkMask.CellCount];
            for (var ly = 0; ly < ChunkMask.Size; ly++)
                for (var lx = 0; lx < ChunkMask.Size; lx++)
                {
                    var wx = coord.x * ChunkMask.Size + lx;
                    var wy = coord.y * ChunkMask.Size + ly;
                    prev[ly * ChunkMask.Size + lx] = path.IsPath(wx, wy);
                    path.SetPathTile(wx, wy, mask.IsPath(lx, ly));
                }
            return new StampResult(prev, mask);
        }

        public void Commit(Vector2Int coord, MapChunkStamp prefab, int yaw, ChunkMask mask, ChunkGrid grid)
        {
            grid.Place(coord, new ChunkSlot(prefab, yaw, mask));
            GameEvents.RaiseChunkPlaced(coord);
        }

        public void Rollback(Vector2Int coord, bool[] prevPath, PathGraph path, GridBoard board)
        {
            for (var ly = 0; ly < ChunkMask.Size; ly++)
                for (var lx = 0; lx < ChunkMask.Size; lx++)
                {
                    var wx = coord.x * ChunkMask.Size + lx;
                    var wy = coord.y * ChunkMask.Size + ly;
                    path.SetPathTile(wx, wy, prevPath[ly * ChunkMask.Size + lx]);
                }
        }
    }
}
