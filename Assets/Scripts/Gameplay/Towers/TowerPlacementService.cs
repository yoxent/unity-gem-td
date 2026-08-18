using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Grid;
using GemTD.Gameplay.Run;

namespace GemTD.Gameplay.Towers
{
    public sealed class TowerPlacementService
    {
        readonly GridBoard _board;
        readonly PathGraph _graph;
        readonly HashSet<Vector2Int> _occupied = new HashSet<Vector2Int>();
        readonly RunEconomy _economy;

        public TowerRuntime Selected { get; set; }

        public TowerPlacementService(
            GridBoard board,
            PathGraph graph,
            RunEconomy economy)
        {
            _board = board;
            _graph = graph;
            _economy = economy;
        }

        public bool IsOccupied(Vector2Int cell) => _occupied.Contains(cell);

        public bool CanPlace(TowerDefinition def, Vector2Int cell, RunStateId phase)
        {
            if (def == null)
                return false;

            if (phase != RunStateId.Plan && phase != RunStateId.Combat)
                return false;

            if (!_board.IsBuildable(cell.x, cell.y))
                return false;

            if (_graph.IsPath(cell.x, cell.y))
                return false;

            if (_occupied.Contains(cell))
                return false;

            if (def.Cost > _economy.Gold)
                return false;

            return true;
        }

        public bool TryPlace(TowerDefinition def, Vector2Int cell, RunStateId phase, out TowerRuntime tower)
        {
            tower = null;

            if (!CanPlace(def, cell, phase))
                return false;

            if (!_economy.TrySpend(def.Cost))
                return false;

            tower = new TowerRuntime(cell, def);
            _occupied.Add(cell);
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
            _occupied.Remove(cell);

            if (Selected == tower)
                Selected = null;

            return true;
        }
    }
}
