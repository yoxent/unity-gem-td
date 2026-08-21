using GemTD.Core;
using GemTD.Gameplay.Grid;
using UnityEngine;

namespace GemTD.Gameplay.Map
{
    public readonly struct StampResult
    {
        public readonly bool[] PrevPath;
        public readonly bool[] PrevBuildable;
        public readonly ChunkMask Mask;

        public StampResult(bool[] prevPath, bool[] prevBuildable, ChunkMask mask)
        {
            PrevPath = prevPath;
            PrevBuildable = prevBuildable;
            Mask = mask;
        }
    }

    public sealed class ChunkStampService
    {
        public StampResult StampTentative(Vector2Int coord, MapChunkStamp prefab, int yaw, PathGraph path, GridBoard board)
        {
            var mask = prefab.GetMask().Rotated(yaw);
            var prevPath = new bool[ChunkMask.CellCount];
            var prevBuildable = new bool[ChunkMask.CellCount];
            for (var ly = 0; ly < ChunkMask.Size; ly++)
                for (var lx = 0; lx < ChunkMask.Size; lx++)
                {
                    var wx = coord.x * ChunkMask.Size + lx;
                    var wy = coord.y * ChunkMask.Size + ly;
                    var i = ly * ChunkMask.Size + lx;
                    prevPath[i] = path.IsPath(wx, wy);
                    prevBuildable[i] = board.IsBuildable(wx, wy);
                    path.SetPathTile(wx, wy, mask.IsPath(lx, ly));
                }
            return new StampResult(prevPath, prevBuildable, mask);
        }

        public void Commit(Vector2Int coord, MapChunkStamp prefab, int yaw, ChunkMask mask, ChunkGrid grid)
        {
            grid.Place(coord, new ChunkSlot(prefab, yaw, mask));
            GameEvents.RaiseChunkPlaced(coord);
        }

        public void Rollback(Vector2Int coord, StampResult result, PathGraph path, GridBoard board)
        {
            for (var ly = 0; ly < ChunkMask.Size; ly++)
                for (var lx = 0; lx < ChunkMask.Size; lx++)
                {
                    var wx = coord.x * ChunkMask.Size + lx;
                    var wy = coord.y * ChunkMask.Size + ly;
                    var i = ly * ChunkMask.Size + lx;
                    path.SetPathTile(wx, wy, result.PrevPath[i]);
                    board.SetBuildable(wx, wy, result.PrevBuildable[i]);
                }
        }
    }
}
