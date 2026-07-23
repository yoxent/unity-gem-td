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

            if (phase != RunStateId.Plan && phase != RunStateId.Combat)
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

            if (phase != RunStateId.Plan)
                return false;

            var gemCount = 0;
            for (var i = 0; i < tower.Sockets.Length; i++)
            {
                if (tower.Sockets[i] != null)
                    gemCount++;
            }

            if (gemCount > inventory.FreeSlotCount)
                return false;

            _economy.AddGold(RunEconomy.ComputeSellRefund(tower.PurchaseCost, tower.UpgradeSpend));

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
