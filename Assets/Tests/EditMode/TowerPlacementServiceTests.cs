using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Grid;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class TowerPlacementServiceTests
    {
        GridBoard _board;
        PathGraph _graph;
        RunEconomy _economy;
        TowerPlacementService _placement;
        TowerDefinition _ballista;
        GemDefinition _lmp;

        [SetUp]
        public void SetUp()
        {
            _board = CreateBoard(out _graph);
            _economy = new RunEconomy(100, 20);
            _placement = new TowerPlacementService(_board, _graph, _economy);

            _ballista = ScriptableObject.CreateInstance<TowerDefinition>();
            _ballista.DisplayName = "Ballista";
            _ballista.Cost = 50;
            _ballista.SocketCount = 2;

            _lmp = ScriptableObject.CreateInstance<GemDefinition>();
            _lmp.Id = GemId.MultipleProjectiles;
            _lmp.DisplayName = "LMP";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_ballista);
            Object.DestroyImmediate(_lmp);
        }

        [Test]
        public void CanPlace_True_WhenBuildableAndAffordable()
        {
            var cell = new Vector2Int(3, 4);
            Assert.IsTrue(_placement.CanPlace(_ballista, cell, RunStateId.Plan, _ballista.Cost));
            Assert.AreEqual(100, _economy.Gold);
        }

        [Test]
        public void CanPlace_False_WhenOccupiedOrUnaffordable()
        {
            var cell = new Vector2Int(3, 4);
            Assert.IsTrue(_placement.TryPlace(_ballista, cell, RunStateId.Plan, _ballista.Cost, out _));
            Assert.IsFalse(_placement.CanPlace(_ballista, cell, RunStateId.Plan, _ballista.Cost));

            var other = new Vector2Int(4, 4);
            Assert.IsFalse(_placement.CanPlace(_ballista, other, RunStateId.Plan, 999));
        }

        [Test]
        public void Place_AllowedInCombat()
        {
            var cell = new Vector2Int(3, 4);

            Assert.IsTrue(_placement.TryPlace(_ballista, cell, RunStateId.Combat, _ballista.Cost, out var tower));

            Assert.IsNotNull(tower);
            Assert.AreEqual(cell, tower.Cell);
            Assert.AreSame(_ballista, tower.Def);
            Assert.AreEqual(50, _economy.Gold);
            Assert.IsTrue(_placement.IsOccupied(cell));
            Assert.AreEqual(50, tower.PurchaseCost);
            Assert.AreEqual(0, tower.UpgradeSpend);
        }

        [Test]
        public void Place_AllowedInPlan()
        {
            var cell = new Vector2Int(3, 4);

            Assert.IsTrue(_placement.TryPlace(_ballista, cell, RunStateId.Plan, _ballista.Cost, out var tower));

            Assert.IsNotNull(tower);
            Assert.AreEqual(50, _economy.Gold);
            Assert.IsTrue(_placement.IsOccupied(cell));
        }

        [Test]
        public void TrySell_AllowedInCombat()
        {
            var cell = new Vector2Int(3, 4);
            Assert.IsTrue(_placement.TryPlace(_ballista, cell, RunStateId.Plan, _ballista.Cost, out var tower));
            Assert.AreEqual(50, _economy.Gold);

            Assert.IsTrue(_placement.TrySell(tower, RunStateId.Combat, new GemInventory(6)));
            Assert.AreEqual(100, _economy.Gold);
            Assert.IsFalse(_placement.IsOccupied(cell));
        }

        [Test]
        public void TrySell_InPlan_RefundsFullAndReturnsGems()
        {
            var cell = new Vector2Int(3, 4);
            var inventory = new GemInventory(6);

            Assert.IsTrue(_placement.TryPlace(_ballista, cell, RunStateId.Plan, _ballista.Cost, out var tower));
            Assert.AreEqual(50, _economy.Gold);

            tower.TrySocket(_lmp, 0, allowSocket: true);
            _placement.Selected = tower;

            Assert.IsTrue(_placement.TrySell(tower, RunStateId.Plan, inventory));

            Assert.AreEqual(100, _economy.Gold);
            Assert.IsFalse(_placement.IsOccupied(cell));
            Assert.IsNull(_placement.Selected);
            Assert.IsTrue(ContainsGem(inventory, _lmp));
        }

        [Test]
        public void TrySell_BlockedWhenBagCannotFitReturnedGems()
        {
            var cell = new Vector2Int(3, 4);
            var inventory = new GemInventory(1);
            inventory.TryAdd(_lmp);

            Assert.IsTrue(_placement.TryPlace(_ballista, cell, RunStateId.Plan, _ballista.Cost, out var tower));
            Assert.AreEqual(50, _economy.Gold);

            var socketGem = ScriptableObject.CreateInstance<GemDefinition>();
            socketGem.Id = GemId.Chain;
            socketGem.DisplayName = "Chain";
            try
            {
                tower.TrySocket(socketGem, 0, allowSocket: true);

                Assert.IsFalse(_placement.CanSell(tower, RunStateId.Plan, inventory));
                Assert.IsFalse(_placement.TrySell(tower, RunStateId.Plan, inventory));
                Assert.AreEqual(50, _economy.Gold);
                Assert.IsTrue(_placement.IsOccupied(cell));
                Assert.AreSame(socketGem, tower.Sockets[0]);
            }
            finally
            {
                Object.DestroyImmediate(socketGem);
            }
        }

        [Test]
        public void TrySell_AllSocketsFilled_ReturnsEveryGemWhenBagFits()
        {
            _ballista.SocketCount = 3;
            var cell = new Vector2Int(3, 4);
            var inventory = new GemInventory(3);

            Assert.IsTrue(_placement.TryPlace(_ballista, cell, RunStateId.Plan, _ballista.Cost, out var tower));

            var chain = ScriptableObject.CreateInstance<GemDefinition>();
            chain.Id = GemId.Chain;
            var fork = ScriptableObject.CreateInstance<GemDefinition>();
            fork.Id = GemId.Fork;
            try
            {
                Assert.IsTrue(tower.TrySocket(_lmp, 0, allowSocket: true));
                Assert.IsTrue(tower.TrySocket(chain, 1, allowSocket: true));
                Assert.IsTrue(tower.TrySocket(fork, 2, allowSocket: true));
                Assert.AreEqual(3, tower.Sockets.Length);

                Assert.IsTrue(_placement.CanSell(tower, RunStateId.Plan, inventory));
                Assert.IsTrue(_placement.TrySell(tower, RunStateId.Plan, inventory));
                Assert.AreEqual(100, _economy.Gold);
                Assert.IsFalse(_placement.IsOccupied(cell));
                Assert.IsTrue(ContainsGem(inventory, _lmp));
                Assert.IsTrue(ContainsGem(inventory, chain));
                Assert.IsTrue(ContainsGem(inventory, fork));
                Assert.AreEqual(3, inventory.OccupiedCount);
            }
            finally
            {
                Object.DestroyImmediate(chain);
                Object.DestroyImmediate(fork);
            }
        }

        [Test]
        public void TrySell_AllSocketsFilled_BlockedWhenBagHasTooFewFreeSlots()
        {
            _ballista.SocketCount = 3;
            var cell = new Vector2Int(3, 4);
            var inventory = new GemInventory(4);
            inventory.TryAdd(_lmp);
            inventory.TryAdd(_lmp);

            Assert.IsTrue(_placement.TryPlace(_ballista, cell, RunStateId.Plan, _ballista.Cost, out var tower));

            var chain = ScriptableObject.CreateInstance<GemDefinition>();
            chain.Id = GemId.Chain;
            var fork = ScriptableObject.CreateInstance<GemDefinition>();
            fork.Id = GemId.Fork;
            var pierce = ScriptableObject.CreateInstance<GemDefinition>();
            pierce.Id = GemId.Pierce;
            try
            {
                Assert.IsTrue(tower.TrySocket(chain, 0, allowSocket: true));
                Assert.IsTrue(tower.TrySocket(fork, 1, allowSocket: true));
                Assert.IsTrue(tower.TrySocket(pierce, 2, allowSocket: true));

                Assert.IsFalse(_placement.CanSell(tower, RunStateId.Plan, inventory));
                Assert.IsFalse(_placement.TrySell(tower, RunStateId.Plan, inventory));
                Assert.AreEqual(50, _economy.Gold);
                Assert.IsTrue(_placement.IsOccupied(cell));
                Assert.AreSame(chain, tower.Sockets[0]);
                Assert.AreSame(fork, tower.Sockets[1]);
                Assert.AreSame(pierce, tower.Sockets[2]);
                Assert.AreEqual(2, inventory.OccupiedCount);
            }
            finally
            {
                Object.DestroyImmediate(chain);
                Object.DestroyImmediate(fork);
                Object.DestroyImmediate(pierce);
            }
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
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    if (y == 3)
                        graph.SetPathTile(x, 3, true);
                    else
                        board.SetBuildable(x, y, true);
                }
            }
            return board;
        }
    }
}
