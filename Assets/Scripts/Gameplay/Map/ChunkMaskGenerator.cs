using System;
using System.Collections.Generic;

namespace GemTD.Gameplay.Map
{
    /// <summary>
    /// Editor-facing mask generator. Runtime expand still yaws prefabs.
    /// Painter pose is locked per type (Straight N+S, Corner S+E, T S+E+W, Cross all).
    /// </summary>
    public static class ChunkMaskGenerator
    {
        const int Size = ChunkMask.Size;
        const int Mid = ChunkMask.Mid;
        const int CellCount = ChunkMask.CellCount;
        static readonly int[] Dx = { 1, -1, 0, 0 };
        static readonly int[] Dy = { 0, 0, 1, -1 };
        static readonly EdgeFlags[] Cardinals =
        {
            EdgeFlags.North, EdgeFlags.East, EdgeFlags.South, EdgeFlags.West
        };

        public static bool TryGenerateCorner(
            System.Random rng,
            out ChunkMask mask,
            HashSet<string> excludeIds = null) =>
            TryGenerate(ChunkType.Corner, rng, out mask, excludeIds);

        public static bool TryGenerate(
            ChunkType type,
            System.Random rng,
            out ChunkMask mask,
            HashSet<string> excludeIds = null)
        {
            mask = default;
            if (!CanGenerate(type)) return false;
            if (rng == null) rng = new System.Random();
            var locked = ChunkPathRules.EditorLockedEdges(type);
            var cells = new bool[CellCount];
            ChunkMask lastLegal = default;
            var haveLast = false;
            for (var attempt = 0; attempt < 80; attempt++)
            {
                Clear(cells);
                var island = rng.Next(4);
                var ok = island == 0
                    ? TryPaintSimple(cells, rng, locked)
                    : TryPaintIsland(cells, rng, island, locked);
                if (!ok) continue;
                var candidate = new ChunkMask(cells);
                if (!ChunkPathRules.IsLegalGenerated(candidate, type)) continue;
                lastLegal = candidate;
                haveLast = true;
                if (excludeIds != null
                    && excludeIds.Contains(ChunkMaskId.Canonical(candidate)))
                    continue;
                mask = candidate;
                return true;
            }

            if (excludeIds == null)
            {
                Clear(cells);
                PaintFallback(cells, type);
                mask = new ChunkMask(cells);
                return ChunkPathRules.IsLegalGenerated(mask, type);
            }

            if (!haveLast) return false;
            mask = lastLegal;
            return false;
        }

        static bool CanGenerate(ChunkType type) =>
            type == ChunkType.Straight
            || type == ChunkType.Corner
            || type == ChunkType.TJunction
            || type == ChunkType.Cross;

        static bool TryPaintSimple(bool[] cells, System.Random rng, EdgeFlags locked)
        {
            CollectPortals(locked, out var xs, out var ys, out var n);
            if (n < 2) return false;
            for (var i = 1; i < n; i++)
            {
                if (!TryConnect(cells, rng, xs[i], ys[i], xs[0], ys[0], 0, 0, 0, locked))
                    return false;
            }
            return true;
        }

        static bool TryPaintIsland(bool[] cells, System.Random rng, int islandSize, EdgeFlags locked)
        {
            var minO = 2;
            var maxO = 5 - islandSize;
            if (maxO < minO) return false;
            var ox = rng.Next(minO, maxO + 1);
            var oy = rng.Next(minO, maxO + 1);
            PaintRing(cells, ox, oy, islandSize);
            for (var c = 0; c < Cardinals.Length; c++)
            {
                var edge = Cardinals[c];
                if ((locked & edge) == 0) continue;
                Portal(edge, out var fx, out var fy);
                FaceAttach(edge, ox, oy, islandSize, out var tx, out var ty);
                if (!TryConnect(cells, rng, fx, fy, tx, ty, ox, oy, islandSize, locked))
                    return false;
            }
            return true;
        }

        static void CollectPortals(EdgeFlags locked, out int[] xs, out int[] ys, out int n)
        {
            xs = new int[4];
            ys = new int[4];
            n = 0;
            for (var c = 0; c < Cardinals.Length; c++)
            {
                var edge = Cardinals[c];
                if ((locked & edge) == 0) continue;
                Portal(edge, out xs[n], out ys[n]);
                n++;
            }
        }

        static void Portal(EdgeFlags edge, out int x, out int y)
        {
            x = Mid;
            y = Mid;
            if (edge == EdgeFlags.North) y = Size - 1;
            else if (edge == EdgeFlags.South) y = 0;
            else if (edge == EdgeFlags.East) x = Size - 1;
            else if (edge == EdgeFlags.West) x = 0;
        }

        static void FaceAttach(EdgeFlags edge, int ox, int oy, int s, out int x, out int y)
        {
            x = ox + (s - 1) / 2;
            y = oy + (s - 1) / 2;
            if (edge == EdgeFlags.North) y = oy + s;
            else if (edge == EdgeFlags.South) y = oy - 1;
            else if (edge == EdgeFlags.East) x = ox + s;
            else if (edge == EdgeFlags.West) x = ox - 1;
        }

        static void PaintRing(bool[] cells, int ox, int oy, int s)
        {
            for (var y = oy - 1; y <= oy + s; y++)
            {
                for (var x = ox - 1; x <= ox + s; x++)
                {
                    if (x >= ox && x < ox + s && y >= oy && y < oy + s)
                        continue;
                    cells[y * Size + x] = true;
                }
            }
        }

        static bool TryConnect(
            bool[] cells,
            System.Random rng,
            int fx, int fy,
            int tx, int ty,
            int ox, int oy, int islandSize,
            EdgeFlags locked)
        {
            cells[fy * Size + fx] = true;
            if (fx == tx && fy == ty) return true;
            var vis = new bool[CellCount];
            vis[fy * Size + fx] = true;
            return Dfs(fx, fy, 0);

            bool Dfs(int x, int y, int depth)
            {
                if (x == tx && y == ty) return true;
                if (depth > 28) return false;
                var order = new[] { 0, 1, 2, 3 };
                Shuffle(order, rng);
                for (var k = 0; k < 4; k++)
                {
                    var nx = x + Dx[order[k]];
                    var ny = y + Dy[order[k]];
                    if (!IsWalkable(nx, ny, tx, ty, ox, oy, islandSize, locked)) continue;
                    var j = ny * Size + nx;
                    if (vis[j]) continue;
                    var placed = !cells[j];
                    if (placed && ChunkPathRules.WouldCreateTwoByTwo(cells, nx, ny))
                        continue;
                    if (placed) cells[j] = true;
                    vis[j] = true;
                    if (Dfs(nx, ny, depth + 1)) return true;
                    vis[j] = false;
                    if (placed) cells[j] = false;
                }
                return false;
            }
        }

        static bool IsWalkable(
            int x, int y, int tx, int ty,
            int ox, int oy, int islandSize, EdgeFlags locked)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size) return false;
            if (islandSize > 0
                && x >= ox && x < ox + islandSize
                && y >= oy && y < oy + islandSize)
                return false;
            if (x == tx && y == ty) return true;
            if (x == 0 || x == Size - 1 || y == 0 || y == Size - 1)
                return ChunkPathRules.IsOpeningCell(x, y, locked);
            return true;
        }

        static void PaintFallback(bool[] cells, ChunkType type)
        {
            switch (type)
            {
                case ChunkType.Straight:
                    for (var y = 0; y < Size; y++)
                        cells[y * Size + Mid] = true;
                    break;
                case ChunkType.TJunction:
                    for (var x = 0; x < Size; x++)
                        cells[Mid * Size + x] = true;
                    for (var y = 0; y <= Mid; y++)
                        cells[y * Size + Mid] = true;
                    break;
                case ChunkType.Cross:
                    for (var y = 0; y < Size; y++)
                        cells[y * Size + Mid] = true;
                    for (var x = 0; x < Size; x++)
                        cells[Mid * Size + x] = true;
                    break;
                default:
                    for (var y = 0; y <= Mid; y++)
                        cells[y * Size + Mid] = true;
                    for (var x = Mid; x < Size; x++)
                        cells[Mid * Size + x] = true;
                    break;
            }
        }

        static void Clear(bool[] cells)
        {
            for (var i = 0; i < cells.Length; i++)
                cells[i] = false;
        }

        static void Shuffle(int[] a, System.Random rng)
        {
            for (var i = a.Length - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var t = a[i];
                a[i] = a[j];
                a[j] = t;
            }
        }
    }
}
