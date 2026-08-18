using System.Collections.Generic;
using GemTD.Gameplay.Grid;
using UnityEngine;

namespace GemTD.Gameplay.Map
{
    public sealed class ChunkExpandService
    {
        readonly ChunkGrid _grid;
        readonly PathGraph _path;
        readonly GridBoard _board;
        readonly ChunkStampService _stamp;
        readonly IChunkCatalog _catalog;
        readonly System.Random _rng;

        readonly List<MapChunkStamp> _all = new List<MapChunkStamp>(16);
        readonly HashSet<Vector2Int> _candidateSet = new HashSet<Vector2Int>();
        readonly List<Vector2Int> _candidates = new List<Vector2Int>(8);
        readonly List<(MapChunkStamp prefab, int yaw)> _passing = new List<(MapChunkStamp, int)>(16);
        readonly EdgeFlags[] _dirs = { EdgeFlags.North, EdgeFlags.East, EdgeFlags.South, EdgeFlags.West };

        public ChunkExpandService(ChunkGrid grid, PathGraph path, GridBoard board,
            ChunkStampService stamp, IChunkCatalog catalog, System.Random rng)
        {
            _grid = grid;
            _path = path;
            _board = board;
            _stamp = stamp;
            _catalog = catalog;
            _rng = rng;
        }

        public int CollectLegalExpands(List<Vector2Int> into)
        {
            into.Clear();
            _catalog.CopyAll(_all);
            CollectCandidates();

            for (var i = 0; i < _candidates.Count; i++)
            {
                var coord = _candidates[i];
                if (AnyPassingCombo(coord, _all))
                    into.Add(coord);
            }
            return into.Count;
        }

        public bool TryExpand(Vector2Int coord)
        {
            if (!_grid.InBounds(coord.x, coord.y) || _grid.IsOccupied(coord.x, coord.y))
                return false;

            _catalog.CopyAll(_all);
            _passing.Clear();
            CollectPassing(coord, _all, _passing);
            if (_passing.Count == 0) return false;

            var pick = _rng.Next(_passing.Count);
            var (prefab, yaw) = _passing[pick];
            var res = _stamp.StampTentative(coord, prefab, yaw, _path, _board);
            if (!_path.AllTipsReachHome())
            {
                _stamp.Rollback(coord, res, _path, _board);
                return false;
            }
            _stamp.Commit(coord, prefab, yaw, res.Mask, _grid);
            return true;
        }

        void CollectCandidates()
        {
            _candidateSet.Clear();
            _candidates.Clear();
            for (var cy = 0; cy < _grid.ChunksH; cy++)
                for (var cx = 0; cx < _grid.ChunksW; cx++)
                {
                    if (!_grid.TryGet(cx, cy, out var slot)) continue;
                    for (var d = 0; d < _dirs.Length; d++)
                    {
                        var dir = _dirs[d];
                        if ((slot.Mask.OpenEdges & dir) == 0) continue;
                        var nb = _grid.NeighborCoord(new Vector2Int(cx, cy), dir);
                        if (!_grid.InBounds(nb.x, nb.y)) continue;
                        if (_grid.IsOccupied(nb.x, nb.y)) continue;
                        if (_candidateSet.Add(nb))
                            _candidates.Add(nb);
                    }
                }
        }

        bool AnyPassingCombo(Vector2Int coord, List<MapChunkStamp> all)
        {
            for (var i = 0; i < all.Count; i++)
            {
                var prefab = all[i];
                if (prefab == null) continue;
                var baseMask = prefab.GetMask();
                for (var yaw = 0; yaw < 4; yaw++)
                {
                    var mask = baseMask.Rotated(yaw);
                    if (!EdgesAgreeWithNeighbors(coord, mask)) continue;
                    if (TentativeReachesHome(coord, prefab, yaw))
                        return true;
                }
            }
            return false;
        }

        void CollectPassing(Vector2Int coord, List<MapChunkStamp> all, List<(MapChunkStamp, int)> into)
        {
            for (var i = 0; i < all.Count; i++)
            {
                var prefab = all[i];
                if (prefab == null) continue;
                var baseMask = prefab.GetMask();
                for (var yaw = 0; yaw < 4; yaw++)
                {
                    var mask = baseMask.Rotated(yaw);
                    if (!EdgesAgreeWithNeighbors(coord, mask)) continue;
                    if (TentativeReachesHome(coord, prefab, yaw))
                        into.Add((prefab, yaw));
                }
            }
        }

        bool EdgesAgreeWithNeighbors(Vector2Int coord, ChunkMask mask)
        {
            var anyOpenMatch = false;
            for (var d = 0; d < _dirs.Length; d++)
            {
                var dir = _dirs[d];
                var nb = _grid.NeighborCoord(coord, dir);
                var newOpen = (mask.OpenEdges & dir) != 0;

                // Map rim is a closed wall — openings into the void are illegal
                // (otherwise a Cross on the border leaves a portal with no expand slot).
                if (!_grid.InBounds(nb.x, nb.y))
                {
                    if (newOpen) return false;
                    continue;
                }

                if (!_grid.TryGet(nb.x, nb.y, out var slot)) continue;

                var neighborOpen = (slot.Mask.OpenEdges & dir.Opposite()) != 0;
                if (newOpen != neighborOpen)
                    return false;
                if (newOpen)
                    anyOpenMatch = true;
            }
            return anyOpenMatch;
        }

        bool TentativeReachesHome(Vector2Int coord, MapChunkStamp prefab, int yaw)
        {
            var res = _stamp.StampTentative(coord, prefab, yaw, _path, _board);
            var ok = _path.AllTipsReachHome();
            _stamp.Rollback(coord, res, _path, _board);
            return ok;
        }
    }
}
