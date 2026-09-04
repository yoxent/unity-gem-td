using System.Collections.Generic;
using UnityEngine;

namespace GemTD.Gameplay.Grid
{
    /// <summary>
    /// Path tiles with a fixed home base. Enemies spawn at tips and march toward home.
    /// Search buffers are reused; visit generation avoids clearing the full board each BFS.
    /// </summary>
    public sealed class PathGraph
    {
        public int Width { get; }
        public int Height { get; }
        public Vector2Int Home { get; private set; }

        readonly bool[] _path;
        readonly List<int> _pathIndices = new List<int>(256);
        readonly int[] _seen;
        readonly int[] _hop;
        readonly int[] _parent;
        readonly int[] _qx;
        readonly int[] _qy;
        readonly List<Vector2Int> _chainScratch = new List<Vector2Int>(64);
        int _searchGen;
        GridBoard _board;

        public PathGraph(int width, int height)
        {
            Width = width > 0 ? width : 1;
            Height = height > 0 ? height : 1;
            var n = Width * Height;
            _path = new bool[n];
            _seen = new int[n];
            _hop = new int[n];
            _parent = new int[n];
            _qx = new int[n];
            _qy = new int[n];
            Home = new Vector2Int(0, 0);
        }

        public void BindBoard(GridBoard board) => _board = board;

        public void SetHome(int x, int y) => Home = new Vector2Int(x, y);

        public bool IsPath(int x, int y) => InBounds(x, y) && _path[Index(x, y)];

        public void SetPathTile(int x, int y, bool isPath)
        {
            if (!InBounds(x, y)) return;
            var i = Index(x, y);
            if (_path[i] != isPath)
            {
                _path[i] = isPath;
                if (isPath)
                    _pathIndices.Add(i);
                else
                    RemovePathIndex(i);
            }
            _board?.SetBuildable(x, y, !isPath);
        }

        public int CollectSpawnTips(List<Vector2Int> into)
        {
            into.Clear();
            for (var n = 0; n < _pathIndices.Count; n++)
            {
                var i = _pathIndices[n];
                var x = i % Width;
                var y = i / Width;
                if (x == Home.x && y == Home.y)
                    continue;
                if (PathNeighborCount(x, y) == 1)
                    into.Add(new Vector2Int(x, y));
            }

            return into.Count;
        }

        public bool AllTipsReachHome()
        {
            if (!IsPath(Home.x, Home.y))
                return false;

            FloodFrom(Home.x, Home.y, writeParent: false, writeHop: false);

            var tipCount = 0;
            for (var n = 0; n < _pathIndices.Count; n++)
            {
                var i = _pathIndices[n];
                var x = i % Width;
                var y = i / Width;
                if (x == Home.x && y == Home.y)
                    continue;
                if (PathNeighborCount(x, y) != 1)
                    continue;
                tipCount++;
                if (_seen[i] != _searchGen)
                    return false;
            }

            return tipCount > 0;
        }

        public bool TryGetWaypointPolyline(Vector2Int tip, List<Vector2Int> into)
        {
            into.Clear();
            if (!IsPath(tip.x, tip.y) || !IsPath(Home.x, Home.y))
                return false;

            FloodFrom(tip.x, tip.y, writeParent: true, writeHop: false);
            if (_seen[Index(Home.x, Home.y)] != _searchGen)
                return false;

            _chainScratch.Clear();
            var cx = Home.x;
            var cy = Home.y;
            while (!(cx == tip.x && cy == tip.y))
            {
                _chainScratch.Add(new Vector2Int(cx, cy));
                var p = _parent[Index(cx, cy)];
                if (p < 0)
                    return false;
                cx = p % Width;
                cy = p / Width;
            }

            _chainScratch.Add(tip);
            for (var i = _chainScratch.Count - 1; i >= 0; i--)
                into.Add(_chainScratch[i]);
            return true;
        }

        /// <summary>Hop distance from home to <paramref name="tip"/> along path tiles (BFS), or -1 if unreachable.</summary>
        public int HopDistanceFromHome(Vector2Int tip)
        {
            if (!InBounds(tip.x, tip.y))
                return -1;

            ComputeHopDistancesFromHome();
            return HopAt(Index(tip.x, tip.y));
        }

        /// <summary>
        /// Ranks <paramref name="tips"/> by hop distance from home (descending), then by
        /// coord (x desc, then y desc) for deterministic tiebreaks. Used to pick boss spawn
        /// tips — furthest tips first. Does not mutate <paramref name="tips"/>.
        /// </summary>
        public void RankTipsByHopDescending(List<Vector2Int> tips, List<Vector2Int> rankedInto)
        {
            rankedInto.Clear();
            if (tips == null || tips.Count == 0)
                return;

            ComputeHopDistancesFromHome();
            rankedInto.AddRange(tips);
            InsertionSortHopDescending(rankedInto);
        }

        void ComputeHopDistancesFromHome()
        {
            if (!IsPath(Home.x, Home.y))
            {
                BeginSearch();
                return;
            }

            FloodFrom(Home.x, Home.y, writeParent: false, writeHop: true);
        }

        int HopAt(int i) => _seen[i] == _searchGen ? _hop[i] : -1;

        void InsertionSortHopDescending(List<Vector2Int> ranked)
        {
            for (var i = 1; i < ranked.Count; i++)
            {
                var key = ranked[i];
                var j = i - 1;
                while (j >= 0 && CompareTip(ranked[j], key) > 0)
                {
                    ranked[j + 1] = ranked[j];
                    j--;
                }
                ranked[j + 1] = key;
            }
        }

        int CompareTip(Vector2Int a, Vector2Int b)
        {
            var ha = HopAt(Index(a.x, a.y));
            var hb = HopAt(Index(b.x, b.y));
            if (ha != hb)
                return hb.CompareTo(ha);
            if (a.x != b.x)
                return b.x.CompareTo(a.x);
            return b.y.CompareTo(a.y);
        }

        public bool HasPathBetween(Vector2Int from, Vector2Int to)
        {
            if (!IsPath(from.x, from.y) || !IsPath(to.x, to.y))
                return false;

            FloodFrom(from.x, from.y, writeParent: false, writeHop: false);
            return _seen[Index(to.x, to.y)] == _searchGen;
        }

        void FloodFrom(int sx, int sy, bool writeParent, bool writeHop)
        {
            BeginSearch();
            var head = 0;
            var tail = 0;
            var start = Index(sx, sy);
            _qx[tail] = sx;
            _qy[tail] = sy;
            tail++;
            _seen[start] = _searchGen;
            if (writeParent)
                _parent[start] = -1;
            if (writeHop)
                _hop[start] = 0;

            while (head < tail)
            {
                var x = _qx[head];
                var y = _qy[head];
                head++;
                var from = Index(x, y);
                var d = writeHop ? _hop[from] : 0;

                TryEnqueueFlood(x + 1, y, from, d, writeParent, writeHop, ref tail);
                TryEnqueueFlood(x - 1, y, from, d, writeParent, writeHop, ref tail);
                TryEnqueueFlood(x, y + 1, from, d, writeParent, writeHop, ref tail);
                TryEnqueueFlood(x, y - 1, from, d, writeParent, writeHop, ref tail);
            }
        }

        void TryEnqueueFlood(int x, int y, int parentIndex, int parentHop, bool writeParent, bool writeHop, ref int tail)
        {
            if (!InBounds(x, y) || !IsPath(x, y)) return;
            var i = Index(x, y);
            if (_seen[i] == _searchGen) return;
            _seen[i] = _searchGen;
            if (writeParent)
                _parent[i] = parentIndex;
            if (writeHop)
                _hop[i] = parentHop + 1;
            _qx[tail] = x;
            _qy[tail] = y;
            tail++;
        }

        void BeginSearch()
        {
            _searchGen++;
            if (_searchGen != int.MaxValue)
                return;

            for (var i = 0; i < _seen.Length; i++)
                _seen[i] = 0;
            _searchGen = 1;
        }

        void RemovePathIndex(int index)
        {
            for (var n = 0; n < _pathIndices.Count; n++)
            {
                if (_pathIndices[n] != index)
                    continue;
                var last = _pathIndices.Count - 1;
                _pathIndices[n] = _pathIndices[last];
                _pathIndices.RemoveAt(last);
                return;
            }
        }

        int PathNeighborCount(int x, int y)
        {
            var n = 0;
            if (IsPath(x + 1, y)) n++;
            if (IsPath(x - 1, y)) n++;
            if (IsPath(x, y + 1)) n++;
            if (IsPath(x, y - 1)) n++;
            return n;
        }

        bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        int Index(int x, int y) => y * Width + x;
    }
}
