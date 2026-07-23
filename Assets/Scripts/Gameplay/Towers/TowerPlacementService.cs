using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Grid;
using GemTD.Gameplay.Map;
using GemTD.Gameplay.Run;

namespace GemTD.Gameplay.Towers
{
    public sealed class TowerPlacementService
    {
        readonly GridBoard _board;
        readonly PathGraph _graph;
        readonly MapExpandService _map;
        readonly RunEconomy _economy;

        public TowerRuntime Selected { get; set; }

        public TowerPlacementService(
            GridBoard board,
            PathGraph graph,
            MapExpandService map,
            RunEconomy economy)
        {
            _board = board;
            _graph = graph;
            _map = map;
            _economy = economy;
        }

        public bool TryPlace(TowerDefinition def, Vector2Int cell, RunStateId phase, out TowerRuntime tower)
        {
            tower = null;

            if (def == null)
                return false;

            if (phase != RunStateId.Build && phase != RunStateId.Combat)
                return false;

            if (!_board.IsBuildable(cell.x, cell.y))
                return false;

            if (_graph.IsPath(cell.x, cell.y))
                return false;

            if (_map.IsBlocked(cell.x, cell.y))
                return false;

            if (!_economy.TrySpend(def.Cost))
                return false;

            tower = new TowerRuntime(cell, def);
            _map.SetOccupied(cell.x, cell.y, true);
            return true;
        }

        public bool TrySell(TowerRuntime tower, RunStateId phase, GemInventory inventory)
        {
            if (tower == null || inventory == null)
                return false;

            if (phase != RunStateId.Build)
                return false;

            _economy.RefundFull(tower.Def.Cost);

            for (var i = 0; i < tower.Sockets.Length; i++)
            {
                if (tower.TryUnsocket(i, out var gem, allowSocket: true))
                    inventory.TryAdd(gem);
            }

            var cell = tower.Cell;
            _map.SetOccupied(cell.x, cell.y, false);

            if (Selected == tower)
                Selected = null;

            return true;
        }
    }
}
