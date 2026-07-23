using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Grid;
using GemTD.Gameplay.Map;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class TowerPlacementServiceTests
    {
        GridBoard _board;
        PathGraph _graph;
        MapExpandService _map;
        RunEconomy _economy;
        TowerPlacementService _placement;
        TowerDefinition _ballista;
        GemDefinition _lmp;

        [SetUp]
        public void SetUp()
        {
            _board = CreateBoard(out _graph);
            _map = new MapExpandService(_graph, _board);
            _economy = new RunEconomy(100, 20);
            _placement = new TowerPlacementService(_board, _graph, _map, _economy);

            _ballista = ScriptableObject.CreateInstance<TowerDefinition>();
            _ballista.DisplayName = "Ballista";
            _ballista.Cost = 50;
            _ballista.SocketCount = 2;

            _lmp = ScriptableObject.CreateInstance<GemDefinition>();
            _lmp.Id = GemId.Lmp;
            _lmp.DisplayName = "LMP";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_ballista);
            Object.DestroyImmediate(_lmp);
        }

        [Test]
        public void Place_AllowedInCombat()
        {
            var cell = new Vector2Int(3, 4);

            Assert.IsTrue(_placement.TryPlace(_ballista, cell, RunStateId.Combat, out var tower));

            Assert.IsNotNull(tower);
            Assert.AreEqual(cell, tower.Cell);
            Assert.AreSame(_ballista, tower.Def);
            Assert.AreEqual(50, _economy.Gold);
            Assert.IsTrue(_map.IsBlocked(cell.x, cell.y));
        }

        [Test]
        public void Sell_BlockedInCombat()
        {
            var cell = new Vector2Int(3, 4);
            Assert.IsTrue(_placement.TryPlace(_ballista, cell, RunStateId.Build, out var tower));
            Assert.AreEqual(50, _economy.Gold);

            Assert.IsFalse(_placement.TrySell(tower, RunStateId.Combat, new GemInventory(6)));
            Assert.AreEqual(50, _economy.Gold);
            Assert.IsTrue(_map.IsBlocked(cell.x, cell.y));
        }

        [Test]
        public void Sell_InBuild_RefundsAndReturnsGems()
        {
            var cell = new Vector2Int(3, 4);
            var inventory = new GemInventory(6);

            Assert.IsTrue(_placement.TryPlace(_ballista, cell, RunStateId.Build, out var tower));
            Assert.AreEqual(50, _economy.Gold);

            tower.TrySocket(_lmp, 0, allowSocket: true);
            _placement.Selected = tower;

            Assert.IsTrue(_placement.TrySell(tower, RunStateId.Build, inventory));

            Assert.AreEqual(100, _economy.Gold);
            Assert.IsFalse(_map.IsBlocked(cell.x, cell.y));
            Assert.IsNull(_placement.Selected);
            Assert.IsTrue(ContainsGem(inventory, _lmp));
        }

        static bool ContainsGem(GemInventory inventory, GemDefinition gem)
        {
            for (var i = 0; i < inventory.Slots.Count; i++)
            {
                if (inventory.Slots[i] == gem)
                    return true;
            }

            return false;
        }

        static GridBoard CreateBoard(out PathGraph graph)
        {
            var board = new GridBoard(8, 8);
            graph = new PathGraph(8, 8);
            graph.BindBoard(board);
            graph.SetHome(0, 3);
            for (var x = 0; x <= 7; x++)
                graph.SetPathTile(x, 3, true);
            return board;
        }
    }
}
