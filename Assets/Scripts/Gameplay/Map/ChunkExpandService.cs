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
        readonly List<Vector2Int> _tipScratch = new List<Vector2Int>(8);
        readonly List<int> _pickTipCounts = new List<int>(16);
        readonly List<int> _bestPickIndices = new List<int>(16);
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

            DropOptionalDeadEnds(_passing);
            var pick = PickHighestTipCountIndex(coord, _passing);
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
                    if (!KeepsTreeSeparation(coord, mask)) continue;
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
                    if (!KeepsTreeSeparation(coord, mask)) continue;
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

        /// <summary>
        /// Paths are a permanent tree: once they split they never merge back.
        /// 1-away must be empty; then a cross on that cell: occupied neighbors
        /// that open into it are merges, and occupied neighbors adjacent to the
        /// incoming chunk are "run along own path." Closed walls are allowed
        /// (horseshoe / dead-end channel). Sealed 1-cell cavities stay T→DeadEnd.
        /// Both flanks merge-blocked + free continuation → Straight.
        /// </summary>
        bool KeepsTreeSeparation(Vector2Int coord, ChunkMask mask)
        {
            if (!TryUniqueIncoming(coord, out var incoming))
                return false;
            if ((mask.OpenEdges & incoming) == 0)
                return false;

            for (var d = 0; d < _dirs.Length; d++)
            {
                var dir = _dirs[d];
                if (dir == incoming) continue;
                if ((mask.OpenEdges & dir) == 0) continue;
                if (!CanOpenWithoutMerge(coord, dir, incoming))
                    return false;
            }

            if (!BothFlanksBlocked(coord, incoming))
                return true;

            var continuation = incoming.Opposite();
            if (CanOpenWithoutMerge(coord, continuation, incoming))
                return mask.OpenEdges == (incoming | continuation);

            return (mask.OpenEdges & ~incoming) == EdgeFlags.None;
        }

        bool BothFlanksBlocked(Vector2Int coord, EdgeFlags incoming)
        {
            var continuation = incoming.Opposite();
            var blocked = 0;
            for (var d = 0; d < _dirs.Length; d++)
            {
                var dir = _dirs[d];
                if (dir == incoming || dir == continuation) continue;
                if (!CanOpenWithoutMerge(coord, dir, incoming))
                    blocked++;
            }
            return blocked == 2;
        }

        bool CanOpenWithoutMerge(Vector2Int coord, EdgeFlags dir, EdgeFlags incoming)
        {
            var one = _grid.NeighborCoord(coord, dir);
            if (!_grid.InBounds(one.x, one.y))
                return false;
            if (_grid.IsOccupied(one.x, one.y))
                return false;
            if (IsForcedDeadEndPocket(one, coord))
                return true;

            var from = _grid.NeighborCoord(coord, incoming);
            for (var d = 0; d < _dirs.Length; d++)
            {
                var side = _dirs[d];
                var nb = _grid.NeighborCoord(one, side);
                if (nb == coord) continue;
                if (!_grid.InBounds(nb.x, nb.y) || !_grid.IsOccupied(nb.x, nb.y))
                    continue;
                if (_grid.OpenEdgeAt(nb.x, nb.y, side.Opposite()))
                    return false;
                if (IsCardinallyAdjacent(nb, from))
                    return false;
            }

            return true;
        }

        bool IsCardinallyAdjacent(Vector2Int a, Vector2Int b)
        {
            for (var d = 0; d < _dirs.Length; d++)
            {
                if (_grid.NeighborCoord(a, _dirs[d]) == b)
                    return true;
            }
            return false;
        }

        bool IsForcedDeadEndPocket(Vector2Int hole, Vector2Int expandCoord)
        {
            for (var d = 0; d < _dirs.Length; d++)
            {
                var dir = _dirs[d];
                var nb = _grid.NeighborCoord(hole, dir);
                if (nb == expandCoord) continue;
                if (!_grid.InBounds(nb.x, nb.y)) continue;
                if (!_grid.IsOccupied(nb.x, nb.y)) return false;
                if (_grid.OpenEdgeAt(nb.x, nb.y, dir.Opposite())) return false;
            }
            return true;
        }

        bool TryUniqueIncoming(Vector2Int coord, out EdgeFlags incoming)
        {
            incoming = EdgeFlags.None;
            var count = 0;
            for (var d = 0; d < _dirs.Length; d++)
            {
                var dir = _dirs[d];
                var nb = _grid.NeighborCoord(coord, dir);
                if (!_grid.OpenEdgeAt(nb.x, nb.y, dir.Opposite()))
                    continue;
                incoming = dir;
                count++;
            }
            return count == 1;
        }

        bool TentativeReachesHome(Vector2Int coord, MapChunkStamp prefab, int yaw)
        {
            var res = _stamp.StampTentative(coord, prefab, yaw, _path, _board);
            var ok = _path.AllTipsReachHome();
            _stamp.Rollback(coord, res, _path, _board);
            return ok;
        }

        /// <summary>
        /// DeadEnds never win a random tip-count tie. Drop them whenever any
        /// other type is legal. Forced DeadEnds (only remaining type: sealed
        /// hole, rim cap) still pick.
        /// </summary>
        void DropOptionalDeadEnds(List<(MapChunkStamp prefab, int yaw)> passing)
        {
            var hasOther = false;
            for (var i = 0; i < passing.Count; i++)
            {
                if (passing[i].prefab.GetMask().Type != ChunkType.DeadEnd)
                {
                    hasOther = true;
                    break;
                }
            }
            if (!hasOther) return;

            var n = 0;
            for (var i = 0; i < passing.Count; i++)
            {
                if (passing[i].prefab.GetMask().Type == ChunkType.DeadEnd)
                    continue;
                passing[n] = passing[i];
                n++;
            }
            for (var i = passing.Count - 1; i >= n; i--)
                passing.RemoveAt(i);
        }

        /// <summary>
        /// Among passing prefab+yaws, prefer the most spawn tips after stamp (keep branches open).
        /// Ties broken uniformly at random.
        /// </summary>
        int PickHighestTipCountIndex(Vector2Int coord, List<(MapChunkStamp prefab, int yaw)> passing)
        {
            _pickTipCounts.Clear();
            var maxTips = -1;
            for (var i = 0; i < passing.Count; i++)
            {
                var (prefab, yaw) = passing[i];
                var res = _stamp.StampTentative(coord, prefab, yaw, _path, _board);
                var tips = _path.CollectSpawnTips(_tipScratch);
                _stamp.Rollback(coord, res, _path, _board);
                _pickTipCounts.Add(tips);
                if (tips > maxTips)
                    maxTips = tips;
            }

            _bestPickIndices.Clear();
            for (var i = 0; i < passing.Count; i++)
            {
                if (_pickTipCounts[i] == maxTips)
                    _bestPickIndices.Add(i);
            }

            return _bestPickIndices[_rng.Next(_bestPickIndices.Count)];
        }
    }
}
