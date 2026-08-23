using System.Collections.Generic;
using GemTD.Gameplay.Grid;
using GemTD.Gameplay.Run;
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
        readonly List<int> _lengthen = new List<int>(16);
        readonly List<int> _splitT = new List<int>(16);
        readonly List<int> _splitCross = new List<int>(16);
        readonly EdgeFlags[] _dirs = { EdgeFlags.North, EdgeFlags.East, EdgeFlags.South, EdgeFlags.West };

        public RunConfig Config { get; set; }
        public int UpcomingWaveNumber { get; set; } = 1;

        public ChunkExpandService(ChunkGrid grid, PathGraph path, GridBoard board,
            ChunkStampService stamp, IChunkCatalog catalog, System.Random rng,
            RunConfig config = null)
        {
            _grid = grid;
            _path = path;
            _board = board;
            _stamp = stamp;
            _catalog = catalog;
            _rng = rng;
            Config = config;
            WarmMaskCache();
        }

        void WarmMaskCache()
        {
            if (_catalog == null)
                return;
            _catalog.CopyAll(_all);
            for (var i = 0; i < _all.Count; i++)
            {
                var stamp = _all[i];
                if (stamp != null)
                    stamp.GetRotatedMask(0);
            }
        }

        public int CollectLegalExpands(List<Vector2Int> into)
        {
            into.Clear();
            _catalog.CopyAll(_all);
            CollectCandidates();

            for (var i = 0; i < _candidates.Count; i++)
            {
                var coord = _candidates[i];
                if (HasPolicyLegalPick(coord, _all))
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

            ApplyPickFilters(coord, _passing);
            if (_passing.Count == 0) return false;

            var pick = PickPolicyIndex(_passing);
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

        /// <summary>
        /// Force a DeadEnd on the first legal tip expand slot (ignores tip-count lottery /
        /// optional-DeadEnd drop). Used after EndWave clear to cap the last open tip before
        /// Victory (Task 7). Returns false if no DeadEnd combo fits.
        /// </summary>
        public bool TryForceDeadEndCap()
        {
            _catalog.CopyAll(_all);
            CollectCandidates();
            for (var i = 0; i < _candidates.Count; i++)
            {
                var coord = _candidates[i];
                _passing.Clear();
                CollectPassing(coord, _all, _passing);
                KeepOnlyDeadEnds(_passing);
                if (_passing.Count == 0)
                    continue;

                var (prefab, yaw) = _passing[0];
                var res = _stamp.StampTentative(coord, prefab, yaw, _path, _board);
                if (!_path.AllTipsReachHome())
                {
                    _stamp.Rollback(coord, res, _path, _board);
                    continue;
                }

                _stamp.Commit(coord, prefab, yaw, res.Mask, _grid);
                return true;
            }

            return false;
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

        bool HasPolicyLegalPick(Vector2Int coord, List<MapChunkStamp> all)
        {
            _passing.Clear();
            CollectPassing(coord, all, _passing);
            if (_passing.Count == 0) return false;
            ApplyPickFilters(coord, _passing);
            return _passing.Count > 0;
        }

        void CollectPassing(Vector2Int coord, List<MapChunkStamp> all, List<(MapChunkStamp, int)> into)
        {
            for (var i = 0; i < all.Count; i++)
            {
                var prefab = all[i];
                if (prefab == null) continue;
                for (var yaw = 0; yaw < 4; yaw++)
                {
                    var mask = prefab.GetRotatedMask(yaw);
                    if (!EdgesAgreeWithNeighbors(coord, mask)) continue;
                    if (!KeepsTreeSeparation(coord, mask)) continue;
                    if (!TryUniqueIncoming(coord, out var incoming)) continue;
                    if (!mask.AllPathTilesReachEdge(incoming)) continue;
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

        void ApplyPickFilters(Vector2Int coord, List<(MapChunkStamp prefab, int yaw)> passing)
        {
            if (ExpandPickPolicy.IsClosingWindow(
                UpcomingWaveNumber, CurrentTipCount(), ExpandPickPolicy.EndWave(Config)))
            {
                KeepOnlyDeadEnds(passing);
                return;
            }

            DropOptionalDeadEnds(passing);
            DropOptionalSplitTypes(coord, passing);
            ForceCavityT(coord, passing);
        }

        void KeepOnlyDeadEnds(List<(MapChunkStamp prefab, int yaw)> passing)
        {
            var n = 0;
            for (var i = 0; i < passing.Count; i++)
            {
                if (passing[i].prefab.GetMask().Type != ChunkType.DeadEnd)
                    continue;
                passing[n] = passing[i];
                n++;
            }
            if (n == 0)
            {
                passing.Clear();
                return;
            }
            for (var i = passing.Count - 1; i >= n; i--)
                passing.RemoveAt(i);
        }

        int CurrentTipCount() => _path.CollectSpawnTips(_tipScratch);

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

        void DropOptionalSplitTypes(Vector2Int coord, List<(MapChunkStamp prefab, int yaw)> passing)
        {
            var wave = UpcomingWaveNumber;
            var tips = CurrentTipCount();
            var allowsT = ExpandPickPolicy.AllowsTJunction(wave, tips, Config);
            var allowsCross = ExpandPickPolicy.AllowsCross(wave, tips, Config);
            if (allowsT && allowsCross)
                return;

            var n = 0;
            for (var i = 0; i < passing.Count; i++)
            {
                var type = passing[i].prefab.GetMask().Type;
                if (type == ChunkType.TJunction && !allowsT)
                {
                    if (OpensIntoForcedPocket(coord, passing[i]))
                    {
                        passing[n] = passing[i];
                        n++;
                    }
                    continue;
                }
                if (type == ChunkType.Cross && !allowsCross)
                    continue;
                passing[n] = passing[i];
                n++;
            }
            if (n == 0)
                return;

            for (var i = passing.Count - 1; i >= n; i--)
                passing.RemoveAt(i);
        }

        void ForceCavityT(Vector2Int coord, List<(MapChunkStamp prefab, int yaw)> passing)
        {
            var hasCavityT = false;
            for (var i = 0; i < passing.Count; i++)
            {
                if (passing[i].prefab.GetMask().Type != ChunkType.TJunction)
                    continue;
                if (!OpensIntoForcedPocket(coord, passing[i]))
                    continue;
                hasCavityT = true;
                break;
            }
            if (!hasCavityT)
                return;

            var n = 0;
            for (var i = 0; i < passing.Count; i++)
            {
                if (passing[i].prefab.GetMask().Type != ChunkType.TJunction)
                    continue;
                if (!OpensIntoForcedPocket(coord, passing[i]))
                    continue;
                passing[n] = passing[i];
                n++;
            }
            for (var i = passing.Count - 1; i >= n; i--)
                passing.RemoveAt(i);
        }

        bool OpensIntoForcedPocket(Vector2Int coord, (MapChunkStamp prefab, int yaw) pick)
        {
            var mask = pick.prefab.GetRotatedMask(pick.yaw);
            if (!TryUniqueIncoming(coord, out var incoming))
                return false;
            for (var d = 0; d < _dirs.Length; d++)
            {
                var dir = _dirs[d];
                if (dir == incoming)
                    continue;
                if ((mask.OpenEdges & dir) == 0)
                    continue;
                var one = _grid.NeighborCoord(coord, dir);
                if (IsForcedDeadEndPocket(one, coord))
                    return true;
            }
            return false;
        }

        int PickPolicyIndex(List<(MapChunkStamp prefab, int yaw)> passing)
        {
            _lengthen.Clear();
            _splitT.Clear();
            _splitCross.Clear();
            for (var i = 0; i < passing.Count; i++)
            {
                var type = passing[i].prefab.GetMask().Type;
                if (type == ChunkType.Straight || type == ChunkType.Corner)
                    _lengthen.Add(i);
                else if (type == ChunkType.TJunction)
                    _splitT.Add(i);
                else if (type == ChunkType.Cross)
                    _splitCross.Add(i);
            }

            var splitTotal = _splitT.Count + _splitCross.Count;
            if (splitTotal == 0)
                return _lengthen.Count > 0 ? PickUniform(_lengthen) : _rng.Next(passing.Count);
            if (_lengthen.Count == 0)
                return PickSplitIndex();
            if (_rng.NextDouble() < ExpandPickPolicy.SplitP(Config))
                return PickSplitIndex();
            return PickUniform(_lengthen);
        }

        int PickSplitIndex()
        {
            if (_splitCross.Count == 0)
                return PickUniform(_splitT);
            if (_splitT.Count == 0)
                return PickUniform(_splitCross);
            if (_rng.NextDouble() < ExpandPickPolicy.CrossRamp(UpcomingWaveNumber, Config))
                return PickUniform(_splitCross);
            return PickUniform(_splitT);
        }

        int PickUniform(List<int> indices) => indices[_rng.Next(indices.Count)];
    }
}
