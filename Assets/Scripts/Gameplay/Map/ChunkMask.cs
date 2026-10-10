using UnityEngine;

namespace GemTD.Gameplay.Map
{
    public readonly struct ChunkMask
    {
        public const int Size = 7;
        public const int Mid = Size / 2;
        public const int CellCount = Size * Size;
        readonly bool[] _isPath;
        readonly bool[] _elevationLocked;
        readonly int _homeIndex;

        public bool HasHome => _homeIndex >= 0 && _homeIndex < CellCount;
        public Vector2Int HomeLocal => HasHome
            ? new Vector2Int(_homeIndex % Size, _homeIndex / Size)
            : new Vector2Int(-1, -1);

        public ChunkMask(bool[] isPath, int homeIndex = -1, bool[] elevationLocked = null)
        {
            _isPath = new bool[CellCount];
            _elevationLocked = new bool[CellCount];
            for (var i = 0; i < CellCount && i < isPath.Length; i++)
                _isPath[i] = isPath[i];
            if (elevationLocked != null)
            {
                for (var i = 0; i < CellCount && i < elevationLocked.Length; i++)
                    _elevationLocked[i] = elevationLocked[i];
            }
            _homeIndex = homeIndex >= 0 && homeIndex < CellCount ? homeIndex : -1;
            if (_homeIndex >= 0)
                _isPath[_homeIndex] = true;
            // Path and home always stay at default height (layer 0).
            for (var i = 0; i < CellCount; i++)
            {
                if (_isPath[i])
                    _elevationLocked[i] = true;
            }
        }

        public bool IsPath(int x, int y) =>
            x >= 0 && x < Size && y >= 0 && y < Size && _isPath[y * Size + x];

        public bool IsElevationLocked(int x, int y) =>
            x >= 0 && x < Size && y >= 0 && y < Size && _elevationLocked[y * Size + x];

        public void CopyElevationLocked(bool[] into)
        {
            if (into == null) return;
            var n = into.Length < CellCount ? into.Length : CellCount;
            for (var i = 0; i < n; i++)
                into[i] = _elevationLocked[i];
        }

        public static Vector2Int EdgeMidWorldCell(Vector2Int chunkCoord, EdgeFlags edge)
        {
            var lx = Mid;
            var ly = Mid;
            if (edge == EdgeFlags.North) ly = Size - 1;
            else if (edge == EdgeFlags.South) ly = 0;
            else if (edge == EdgeFlags.East) lx = Size - 1;
            else if (edge == EdgeFlags.West) lx = 0;
            return new Vector2Int(chunkCoord.x * Size + lx, chunkCoord.y * Size + ly);
        }

        /// <summary>1x1 cell on the empty side of an occupied opening (expand-marker seat).</summary>
        public static Vector2Int AdjacentExpandCell(Vector2Int occupied, EdgeFlags outward)
        {
            var portal = EdgeMidWorldCell(occupied, outward);
            if (outward == EdgeFlags.North) return portal + new Vector2Int(0, 1);
            if (outward == EdgeFlags.South) return portal + new Vector2Int(0, -1);
            if (outward == EdgeFlags.East) return portal + new Vector2Int(1, 0);
            if (outward == EdgeFlags.West) return portal + new Vector2Int(-1, 0);
            return portal;
        }

        public EdgeFlags OpenEdges
        {
            get
            {
                var e = EdgeFlags.None;
                if (IsPath(Mid, 0)) e |= EdgeFlags.South;
                if (IsPath(Mid, Size - 1)) e |= EdgeFlags.North;
                if (IsPath(0, Mid)) e |= EdgeFlags.West;
                if (IsPath(Size - 1, Mid)) e |= EdgeFlags.East;
                return e;
            }
        }

        public ChunkType Type
        {
            get
            {
                // Keep / homebase: openings scale with difficulty (1–4); home cell is the type signal.
                if (HasHome) return ChunkType.Homebase;
                var edges = OpenEdges;
                switch (edges.Count())
                {
                    case 0: return ChunkType.Land;
                    case 1: return ChunkType.DeadEnd;
                    case 2: return HasOppositePair(edges) ? ChunkType.Straight : ChunkType.Corner;
                    case 3: return ChunkType.TJunction;
                    default: return ChunkType.Cross;
                }
            }
        }

        static bool HasOppositePair(EdgeFlags e) =>
            ((e & EdgeFlags.North) != 0 && (e & EdgeFlags.South) != 0) ||
            ((e & EdgeFlags.East)  != 0 && (e & EdgeFlags.West)  != 0);

        public ChunkMask Rotated(int quarterTurnsCW)
        {
            var turns = ((quarterTurnsCW % 4) + 4) % 4;
            var srcPath = _isPath;
            var srcLock = _elevationLocked;
            var home = _homeIndex;
            for (var t = 0; t < turns; t++)
            {
                var dstPath = new bool[CellCount];
                var dstLock = new bool[CellCount];
                for (var y = 0; y < Size; y++)
                    for (var x = 0; x < Size; x++)
                    {
                        var di = (Size - 1 - x) * Size + y;
                        dstPath[di] = srcPath[y * Size + x];
                        dstLock[di] = srcLock[y * Size + x];
                    }
                if (home >= 0)
                {
                    var hx = home % Size;
                    var hy = home / Size;
                    home = hy + (Size - 1 - hx) * Size;
                }
                srcPath = dstPath;
                srcLock = dstLock;
            }
            return new ChunkMask(srcPath, home, srcLock);
        }

        public Vector2Int HomeWorldCell(Vector2Int chunkCoord)
        {
            var local = HomeLocal;
            return new Vector2Int(chunkCoord.x * Size + local.x, chunkCoord.y * Size + local.y);
        }

        /// <summary>
        /// True when every path tile is reachable from the given opening (edge mid).
        /// Expand legality uses this instead of stamping + full-board BFS per prefab×yaw.
        /// </summary>
        public bool AllPathTilesReachEdge(EdgeFlags edge)
        {
            var start = EdgeMidIndex(edge);
            if (start < 0 || _isPath == null || !_isPath[start])
                return false;

            var pathCount = 0;
            for (var i = 0; i < CellCount; i++)
            {
                if (_isPath[i])
                    pathCount++;
            }
            if (pathCount <= 0)
                return false;

            ClearVisitScratch();
            var head = 0;
            var tail = 0;
            VisitScratch[start] = true;
            QueueScratch[tail++] = start;
            var reached = 1;

            while (head < tail)
            {
                var i = QueueScratch[head++];
                var x = i % Size;
                var y = i / Size;
                reached += TryVisitPath(x + 1, y, ref tail);
                reached += TryVisitPath(x - 1, y, ref tail);
                reached += TryVisitPath(x, y + 1, ref tail);
                reached += TryVisitPath(x, y - 1, ref tail);
            }

            return reached == pathCount;
        }

        public bool AreOpeningsConnected()
        {
            var edges = OpenEdges;
            if (edges == EdgeFlags.None) return true;

            var n = 0;
            if ((edges & EdgeFlags.North) != 0) OpeningScratch[n++] = Idx(Mid, Size - 1);
            if ((edges & EdgeFlags.South) != 0) OpeningScratch[n++] = Idx(Mid, 0);
            if ((edges & EdgeFlags.East)  != 0) OpeningScratch[n++] = Idx(Size - 1, Mid);
            if ((edges & EdgeFlags.West)  != 0) OpeningScratch[n++] = Idx(0, Mid);
            if (n < 2) return true;

            ClearVisitScratch();
            var head = 0;
            var tail = 0;
            VisitScratch[OpeningScratch[0]] = true;
            QueueScratch[tail++] = OpeningScratch[0];
            while (head < tail)
            {
                var i = QueueScratch[head++];
                var x = i % Size;
                var y = i / Size;
                TryVisitPath(x + 1, y, ref tail);
                TryVisitPath(x - 1, y, ref tail);
                TryVisitPath(x, y + 1, ref tail);
                TryVisitPath(x, y - 1, ref tail);
            }

            for (var i = 1; i < n; i++)
            {
                if (!VisitScratch[OpeningScratch[i]])
                    return false;
            }
            return true;
        }

        static readonly bool[] VisitScratch = new bool[CellCount];
        static readonly int[] QueueScratch = new int[CellCount];
        static readonly int[] OpeningScratch = new int[4];

        static void ClearVisitScratch()
        {
            for (var i = 0; i < CellCount; i++)
                VisitScratch[i] = false;
        }

        int TryVisitPath(int x, int y, ref int tail)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size) return 0;
            var j = Idx(x, y);
            if (VisitScratch[j] || !_isPath[j]) return 0;
            VisitScratch[j] = true;
            QueueScratch[tail++] = j;
            return 1;
        }

        static int EdgeMidIndex(EdgeFlags edge)
        {
            if (edge == EdgeFlags.North) return Idx(Mid, Size - 1);
            if (edge == EdgeFlags.South) return Idx(Mid, 0);
            if (edge == EdgeFlags.East) return Idx(Size - 1, Mid);
            if (edge == EdgeFlags.West) return Idx(0, Mid);
            return -1;
        }

        static int Idx(int x, int y) => y * Size + x;
    }
}
