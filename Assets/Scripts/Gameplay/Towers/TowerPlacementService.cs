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

        public TowerInstance Selected { get; set; }

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

        public bool CanPlace(TowerDefinition def, Vector2Int cell, RunStateId phase, int placeCost)
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

            if (placeCost > _economy.Gold)
                return false;

            return true;
        }

        public bool TryPlace(TowerDefinition def, Vector2Int cell, RunStateId phase, int placeCost, out TowerInstance tower)
        {
            tower = null;

            if (!CanPlace(def, cell, phase, placeCost))
                return false;

            if (!_economy.TrySpend(placeCost))
                return false;

            tower = new TowerInstance(cell, def, placeCost);
            _occupied.Add(cell);
            return true;
        }

        public bool CanSell(TowerInstance tower, RunStateId phase, GemInventory inventory)
        {
            if (tower == null || inventory == null)
                return false;

            if (phase != RunStateId.Plan && phase != RunStateId.Combat)
                return false;

            return CountSocketedGems(tower) <= inventory.FreeSlotCount;
        }

        public bool TrySell(TowerInstance tower, RunStateId phase, GemInventory inventory)
        {
            if (!CanSell(tower, phase, inventory))
                return false;

            for (var i = 0; i < tower.Sockets.Length; i++)
            {
                if (!tower.TryUnsocket(i, out var gem, allowSocket: true, ignoreHydraLock: true))
                    continue;

                if (inventory.TryAdd(gem))
                    continue;

                tower.TrySocket(gem, i, allowSocket: true);
                return false;
            }

            _economy.AddGold(RunEconomy.ComputeSellRefund(tower.PurchaseCost, tower.UpgradeSpend));

            var cell = tower.Cell;
            _occupied.Remove(cell);

            if (Selected == tower)
                Selected = null;

            return true;
        }

        static int CountSocketedGems(TowerInstance tower)
        {
            var gemCount = 0;
            for (var i = 0; i < tower.Sockets.Length; i++)
            {
                if (tower.Sockets[i] != null)
                    gemCount++;
            }

            return gemCount;
        }
    }
}
