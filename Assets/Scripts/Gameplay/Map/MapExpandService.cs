using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Grid;

namespace GemTD.Gameplay.Map
{
    public sealed class MapExpandService
    {
        readonly PathGraph _graph;
        readonly GridBoard _board;
        readonly bool[] _occupied;

        public MapExpandService(PathGraph graph, GridBoard board)
        {
            _graph = graph;
            _board = board;
            _occupied = new bool[board.Width * board.Height];
        }

        public bool IsBlocked(int x, int y) =>
            _board.InBounds(x, y) && _occupied[y * _board.Width + x];

        public void SetOccupied(int x, int y, bool value)
        {
            if (!_board.InBounds(x, y)) return;
            _occupied[y * _board.Width + x] = value;
        }

        public int CollectLegalExpands(List<Vector2Int> into)
        {
            into.Clear();
            for (var y = 0; y < _board.Height; y++)
            {
                for (var x = 0; x < _board.Width; x++)
                {
                    if (IsLegalExpand(x, y))
                        into.Add(new Vector2Int(x, y));
                }
            }
            return into.Count;
        }

        public bool TryExpand(Vector2Int cell)
        {
            if (!IsLegalExpand(cell.x, cell.y))
                return false;
            _graph.SetPathTile(cell.x, cell.y, true);
            return true;
        }

        bool IsLegalExpand(int x, int y)
        {
            if (!_board.InBounds(x, y)) return false;
            if (_graph.IsPath(x, y)) return false;
            if (!_board.IsBuildable(x, y)) return false;
            if (IsBlocked(x, y)) return false;
            if (!IsAdjacentToPath(x, y)) return false;

            _graph.SetPathTile(x, y, true);
            var ok = _graph.AllTipsReachHome();
            _graph.SetPathTile(x, y, false);
            return ok;
        }

        bool IsAdjacentToPath(int x, int y) =>
            _graph.IsPath(x + 1, y) ||
            _graph.IsPath(x - 1, y) ||
            _graph.IsPath(x, y + 1) ||
            _graph.IsPath(x, y - 1);
    }
}
