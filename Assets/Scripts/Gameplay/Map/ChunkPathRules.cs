namespace GemTD.Gameplay.Map
{
    /// <summary>
    /// Shared legality for generated 1-wide corner masks: exact corner openings,
    /// no 2x2 path, rim path only at openings, no spurs or balloon loops.
    /// </summary>
    public static class ChunkPathRules
    {
        const int Size = ChunkMask.Size;
        const int Mid = ChunkMask.Mid;
        const int CellCount = ChunkMask.CellCount;

        public static bool IsLegalCorner(ChunkMask mask) =>
            IsLegalGenerated(mask, ChunkType.Corner, requireEditorLock: false);

        public static bool IsLegalGenerated(ChunkMask mask, ChunkType expected) =>
            IsLegalGenerated(mask, expected, requireEditorLock: true);

        static bool IsLegalGenerated(ChunkMask mask, ChunkType expected, bool requireEditorLock)
        {
            if (mask.Type != expected) return false;
            if (!mask.AreOpeningsConnected()) return false;
            if (HasTwoByTwoPath(mask)) return false;
            if (HasExtraRimPath(mask)) return false;
            if (!AllPathCellsOnSomeTerminalPath(mask)) return false;
            if (requireEditorLock && !HasEditorLockedEdges(mask)) return false;
            return true;
        }

        /// <summary>
        /// Painter/generate pose only. Runtime expand still yaws 0/90/180/270.
        /// </summary>
        public static EdgeFlags EditorLockedEdges(ChunkType type)
        {
            switch (type)
            {
                case ChunkType.DeadEnd: return EdgeFlags.South;
                case ChunkType.Straight: return EdgeFlags.North | EdgeFlags.South;
                case ChunkType.Corner: return EdgeFlags.South | EdgeFlags.East;
                case ChunkType.TJunction: return EdgeFlags.South | EdgeFlags.East | EdgeFlags.West;
                case ChunkType.Cross:
                    return EdgeFlags.North | EdgeFlags.East | EdgeFlags.South | EdgeFlags.West;
                case ChunkType.Homebase:
                    // Openings vary with difficulty (1–4). No single painter lock.
                    return EdgeFlags.None;
                default: return EdgeFlags.None;
            }
        }

        public static bool HasEditorLockedEdges(ChunkMask mask)
        {
            if (mask.Type == ChunkType.Homebase) return true;
            return mask.OpenEdges == EditorLockedEdges(mask.Type);
        }

        public static bool HasTwoByTwoPath(ChunkMask mask)
        {
            for (var y = 0; y < Size - 1; y++)
            {
                for (var x = 0; x < Size - 1; x++)
                {
                    if (mask.IsPath(x, y)
                        && mask.IsPath(x + 1, y)
                        && mask.IsPath(x, y + 1)
                        && mask.IsPath(x + 1, y + 1))
                        return true;
                }
            }
            return false;
        }

        public static bool WouldCreateTwoByTwo(bool[] cells, int x, int y)
        {
            for (var dy = -1; dy <= 0; dy++)
            {
                for (var dx = -1; dx <= 0; dx++)
                {
                    var x0 = x + dx;
                    var y0 = y + dy;
                    if (x0 < 0 || y0 < 0 || x0 + 1 >= Size || y0 + 1 >= Size)
                        continue;
                    var n = 0;
                    for (var j = 0; j < 2; j++)
                    {
                        for (var i = 0; i < 2; i++)
                        {
                            var cx = x0 + i;
                            var cy = y0 + j;
                            if (cx == x && cy == y) { n++; continue; }
                            if (cells[cy * Size + cx]) n++;
                        }
                    }
                    if (n == 4) return true;
                }
            }
            return false;
        }

        public static bool HasExtraRimPath(ChunkMask mask)
        {
            var edges = mask.OpenEdges;
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    if (x != 0 && x != Size - 1 && y != 0 && y != Size - 1)
                        continue;
                    if (!mask.IsPath(x, y)) continue;
                    if (IsOpeningCell(x, y, edges)) continue;
                    return true;
                }
            }
            return false;
        }

        public static bool AllPathCellsOnSomeTerminalPath(ChunkMask mask)
        {
            var edges = mask.OpenEdges;
            var n = edges.Count();
            if (n < 2) return true;
            var openings = new int[4];
            var count = 0;
            if ((edges & EdgeFlags.North) != 0)
                openings[count++] = (Size - 1) * Size + Mid;
            if ((edges & EdgeFlags.East) != 0)
                openings[count++] = Mid * Size + (Size - 1);
            if ((edges & EdgeFlags.South) != 0)
                openings[count++] = Mid;
            if ((edges & EdgeFlags.West) != 0)
                openings[count++] = Mid * Size;
            for (var i = 0; i < count; i++)
            {
                var x = openings[i] % Size;
                var y = openings[i] / Size;
                if (!mask.IsPath(x, y)) return false;
            }

            var onAny = new bool[CellCount];
            for (var i = 0; i < count; i++)
                for (var j = i + 1; j < count; j++)
                    MarkSimplePaths(mask, openings[i], openings[j], onAny);

            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    if (mask.IsPath(x, y) && !onAny[y * Size + x])
                        return false;
                }
            }
            return true;
        }

        static void MarkSimplePaths(ChunkMask mask, int from, int to, bool[] onAny)
        {
            var vis = new bool[CellCount];
            vis[from] = true;
            Dfs(from);

            void Dfs(int i)
            {
                if (i == to)
                {
                    for (var k = 0; k < CellCount; k++)
                        if (vis[k]) onAny[k] = true;
                    return;
                }
                var x = i % Size;
                var y = i / Size;
                Step(x + 1, y);
                Step(x - 1, y);
                Step(x, y + 1);
                Step(x, y - 1);
            }

            void Step(int x, int y)
            {
                if (x < 0 || x >= Size || y < 0 || y >= Size) return;
                var j = y * Size + x;
                if (vis[j] || !mask.IsPath(x, y)) return;
                vis[j] = true;
                Dfs(j);
                vis[j] = false;
            }
        }

        public static bool IsOpeningCell(int x, int y, EdgeFlags edges)
        {
            if (x == Mid && y == Size - 1 && (edges & EdgeFlags.North) != 0) return true;
            if (x == Mid && y == 0 && (edges & EdgeFlags.South) != 0) return true;
            if (x == Size - 1 && y == Mid && (edges & EdgeFlags.East) != 0) return true;
            if (x == 0 && y == Mid && (edges & EdgeFlags.West) != 0) return true;
            return false;
        }
    }
}
