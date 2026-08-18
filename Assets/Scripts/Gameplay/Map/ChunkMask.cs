using System.Collections.Generic;

namespace GemTD.Gameplay.Map
{
    public readonly struct ChunkMask
    {
        public const int Size = 5;
        public const int CellCount = Size * Size;
        readonly bool[] _isPath;

        public ChunkMask(bool[] isPath)
        {
            _isPath = new bool[CellCount];
            for (var i = 0; i < CellCount && i < isPath.Length; i++)
                _isPath[i] = isPath[i];
        }

        public bool IsPath(int x, int y) =>
            x >= 0 && x < Size && y >= 0 && y < Size && _isPath[y * Size + x];

        public EdgeFlags OpenEdges
        {
            get
            {
                var e = EdgeFlags.None;
                if (IsPath(2, 0)) e |= EdgeFlags.South;
                if (IsPath(2, 4)) e |= EdgeFlags.North;
                if (IsPath(0, 2)) e |= EdgeFlags.West;
                if (IsPath(4, 2)) e |= EdgeFlags.East;
                return e;
            }
        }

        public ChunkType Type
        {
            get
            {
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
            var src = _isPath;
            for (var t = 0; t < turns; t++)
            {
                var dst = new bool[CellCount];
                for (var y = 0; y < Size; y++)
                    for (var x = 0; x < Size; x++)
                        dst[(Size - 1 - x) * Size + y] = src[y * Size + x];
                src = dst;
            }
            return new ChunkMask(src);
        }

        public bool AreOpeningsConnected()
        {
            var edges = OpenEdges;
            if (edges == EdgeFlags.None) return true;

            var starts = new List<int>(4);
            if ((edges & EdgeFlags.North) != 0) starts.Add(Idx(2, 4));
            if ((edges & EdgeFlags.South) != 0) starts.Add(Idx(2, 0));
            if ((edges & EdgeFlags.East)  != 0) starts.Add(Idx(4, 2));
            if ((edges & EdgeFlags.West)  != 0) starts.Add(Idx(0, 2));
            if (starts.Count < 2) return true;

            var visited = new bool[CellCount];
            var q = new Queue<int>(CellCount);
            var isPath = _isPath;
            q.Enqueue(starts[0]); visited[starts[0]] = true;
            while (q.Count > 0)
            {
                var i = q.Dequeue();
                var x = i % Size; var y = i / Size;
                TryVisit(x + 1, y); TryVisit(x - 1, y); TryVisit(x, y + 1); TryVisit(x, y - 1);
            }
            for (var i = 1; i < starts.Count; i++)
                if (!visited[starts[i]]) return false;
            return true;

            void TryVisit(int x, int y)
            {
                if (x < 0 || x >= Size || y < 0 || y >= Size) return;
                var j = Idx(x, y);
                if (visited[j] || !isPath[j]) return;
                visited[j] = true; q.Enqueue(j);
            }
        }

        static int Idx(int x, int y) => y * Size + x;
    }
}
