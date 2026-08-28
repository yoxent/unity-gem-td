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
        readonly bool[] _prevPath = new bool[ChunkMask.CellCount];
        readonly bool[] _prevBuildable = new bool[ChunkMask.CellCount];
        readonly TileHeightMap _heights;
        readonly System.Random _rng;

        public HeightInfluenceWeights Weights { get; set; }

        public ChunkStampService(
            TileHeightMap heights = null,
            System.Random rng = null,
            HeightInfluenceWeights weights = default)
        {
            _heights = heights;
            _rng = rng;
            Weights = weights.IsUnset ? HeightInfluenceWeights.Default : weights;
        }

        /// <summary>
        /// One tentative stamp at a time — rollback buffers are reused.
        /// Call <see cref="Rollback"/> (or <see cref="Commit"/>) before the next stamp.
        /// </summary>
        public StampResult StampTentative(Vector2Int coord, MapChunkStamp prefab, int yaw, PathGraph path, GridBoard board)
        {
            var mask = prefab.GetRotatedMask(yaw);
            for (var ly = 0; ly < ChunkMask.Size; ly++)
                for (var lx = 0; lx < ChunkMask.Size; lx++)
                {
                    var wx = coord.x * ChunkMask.Size + lx;
                    var wy = coord.y * ChunkMask.Size + ly;
                    var i = ly * ChunkMask.Size + lx;
                    _prevPath[i] = path.IsPath(wx, wy);
                    _prevBuildable[i] = board.IsBuildable(wx, wy);
                    path.SetPathTile(wx, wy, mask.IsPath(lx, ly));
                }
            return new StampResult(_prevPath, _prevBuildable, mask);
        }

        public void Commit(Vector2Int coord, MapChunkStamp prefab, int yaw, ChunkMask mask, ChunkGrid grid)
        {
            grid.Place(coord, new ChunkSlot(prefab, yaw, mask));
            if (_heights != null && _rng != null)
                TileHeightAssigner.AssignChunk(_heights, mask, coord, _rng, Weights);
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
