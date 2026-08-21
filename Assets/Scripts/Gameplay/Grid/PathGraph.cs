using System.Collections.Generic;
using UnityEngine;

namespace GemTD.Gameplay.Grid
{
    /// <summary>
    /// Path tiles with a fixed home base. Enemies spawn at tips and march toward home.
    /// </summary>
    public sealed class PathGraph
    {
        public int Width { get; }
        public int Height { get; }
        public Vector2Int Home { get; private set; }

        readonly bool[] _path;
        GridBoard _board;

        public PathGraph(int width, int height)
        {
            Width = width > 0 ? width : 1;
            Height = height > 0 ? height : 1;
            _path = new bool[Width * Height];
            Home = new Vector2Int(0, 0);
        }

        public void BindBoard(GridBoard board) => _board = board;

        public void SetHome(int x, int y) => Home = new Vector2Int(x, y);

        public bool IsPath(int x, int y) => InBounds(x, y) && _path[Index(x, y)];

        public void SetPathTile(int x, int y, bool isPath)
        {
            if (!InBounds(x, y)) return;
            _path[Index(x, y)] = isPath;
            _board?.SetBuildable(x, y, !isPath);
        }

        public int CollectSpawnTips(List<Vector2Int> into)
        {
            into.Clear();
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    if (!IsPath(x, y))
                        continue;
                    if (x == Home.x && y == Home.y)
                        continue;
                    if (PathNeighborCount(x, y) == 1)
                        into.Add(new Vector2Int(x, y));
                }
            }

            return into.Count;
        }

        public bool AllTipsReachHome()
        {
            if (!IsPath(Home.x, Home.y))
                return false;

            var tips = new List<Vector2Int>(8);
            if (CollectSpawnTips(tips) == 0)
                return false;

            for (var i = 0; i < tips.Count; i++)
            {
                if (!HasPathBetween(tips[i], Home))
                    return false;
            }

            return true;
        }

        public bool TryGetWaypointPolyline(Vector2Int tip, List<Vector2Int> into)
        {
            into.Clear();
            if (!IsPath(tip.x, tip.y) || !IsPath(Home.x, Home.y))
                return false;
            if (!HasPathBetween(tip, Home))
                return false;

            var parent = new int[Width * Height];
            for (var i = 0; i < parent.Length; i++)
                parent[i] = -1;

            var visited = new bool[Width * Height];
            var qx = new int[Width * Height];
            var qy = new int[Width * Height];
            var head = 0;
            var tail = 0;
            qx[tail] = tip.x;
            qy[tail] = tip.y;
            tail++;
            visited[Index(tip.x, tip.y)] = true;

            while (head < tail)
            {
                var x = qx[head];
                var y = qy[head];
                head++;
                if (x == Home.x && y == Home.y)
                    break;

                TryEnqueueParent(x + 1, y, x, y, visited, parent, qx, qy, ref tail);
                TryEnqueueParent(x - 1, y, x, y, visited, parent, qx, qy, ref tail);
                TryEnqueueParent(x, y + 1, x, y, visited, parent, qx, qy, ref tail);
                TryEnqueueParent(x, y - 1, x, y, visited, parent, qx, qy, ref tail);
            }

            var chain = new List<Vector2Int>(Width);
            var cx = Home.x;
            var cy = Home.y;
            while (!(cx == tip.x && cy == tip.y))
            {
                chain.Add(new Vector2Int(cx, cy));
                var p = parent[Index(cx, cy)];
                if (p < 0)
                    return false;
                cx = p % Width;
                cy = p / Width;
            }

            chain.Add(tip);
            for (var i = chain.Count - 1; i >= 0; i--)
                into.Add(chain[i]);
            return true;
        }

        public bool HasPathBetween(Vector2Int from, Vector2Int to)
        {
            if (!IsPath(from.x, from.y) || !IsPath(to.x, to.y))
                return false;

            var visited = new bool[Width * Height];
            var qx = new int[Width * Height];
            var qy = new int[Width * Height];
            var head = 0;
            var tail = 0;
            qx[tail] = from.x;
            qy[tail] = from.y;
            tail++;
            visited[Index(from.x, from.y)] = true;

            while (head < tail)
            {
                var x = qx[head];
                var y = qy[head];
                head++;
                if (x == to.x && y == to.y)
                    return true;

                TryEnqueue(x + 1, y, visited, qx, qy, ref tail);
                TryEnqueue(x - 1, y, visited, qx, qy, ref tail);
                TryEnqueue(x, y + 1, visited, qx, qy, ref tail);
                TryEnqueue(x, y - 1, visited, qx, qy, ref tail);
            }

            return false;
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

        void TryEnqueue(int x, int y, bool[] visited, int[] qx, int[] qy, ref int tail)
        {
            if (!InBounds(x, y) || !IsPath(x, y)) return;
            var i = Index(x, y);
            if (visited[i]) return;
            visited[i] = true;
            qx[tail] = x;
            qy[tail] = y;
            tail++;
        }

        void TryEnqueueParent(int x, int y, int px, int py, bool[] visited, int[] parent, int[] qx, int[] qy, ref int tail)
        {
            if (!InBounds(x, y) || !IsPath(x, y)) return;
            var i = Index(x, y);
            if (visited[i]) return;
            visited[i] = true;
            parent[i] = Index(px, py);
            qx[tail] = x;
            qy[tail] = y;
            tail++;
        }

        bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        int Index(int x, int y) => y * Width + x;
    }
}
