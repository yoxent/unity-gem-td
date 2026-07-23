using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Grid;
using GemTD.Gameplay.Map;

namespace GemTD.Tests.EditMode
{
    public sealed class MapExpandServiceTests
    {
        [Test]
        public void CollectLegal_IncludesCellAdjacentToPath()
        {
            var board = CreateBound(out var graph);
            var svc = new MapExpandService(graph, board);
            var cells = new List<Vector2Int>();
            var n = svc.CollectLegalExpands(cells);
            Assert.Greater(n, 0);
            Assert.Contains(new Vector2Int(3, 4), cells);
        }

        [Test]
        public void TryExpand_ConvertsToPathAndKeepsConnectivity()
        {
            var board = CreateBound(out var graph);
            var svc = new MapExpandService(graph, board);
            Assert.IsTrue(svc.TryExpand(new Vector2Int(3, 4)));
            Assert.IsTrue(graph.IsPath(3, 4));
            Assert.IsFalse(board.IsBuildable(3, 4));
            Assert.IsTrue(graph.AllTipsReachHome());
        }

        [Test]
        public void CollectLegal_RejectsOccupiedCell()
        {
            var board = CreateBound(out var graph);
            var svc = new MapExpandService(graph, board);
            svc.SetOccupied(3, 4, true);

            var cells = new List<Vector2Int>();
            svc.CollectLegalExpands(cells);

            Assert.IsFalse(cells.Contains(new Vector2Int(3, 4)));
        }

        [Test]
        public void TryExpand_RejectsOccupiedCell_LeavesGraphUnchanged()
        {
            var board = CreateBound(out var graph);
            var svc = new MapExpandService(graph, board);
            svc.SetOccupied(3, 4, true);

            Assert.IsFalse(svc.TryExpand(new Vector2Int(3, 4)));
            Assert.IsFalse(graph.IsPath(3, 4));
            Assert.IsTrue(board.IsBuildable(3, 4));
            Assert.IsTrue(graph.AllTipsReachHome());
        }

        [Test]
        public void TryExpand_RejectsPathTile_LeavesGraphUnchanged()
        {
            var board = CreateBound(out var graph);
            var svc = new MapExpandService(graph, board);
            var candidate = new Vector2Int(3, 3);

            Assert.IsTrue(graph.IsPath(candidate.x, candidate.y));
            Assert.IsFalse(svc.TryExpand(candidate));
            Assert.IsTrue(graph.AllTipsReachHome());
        }

        [Test]
        public void TryExpand_RejectsNonAdjacentCell_LeavesGraphUnchanged()
        {
            var board = CreateBound(out var graph);
            var svc = new MapExpandService(graph, board);
            var candidate = new Vector2Int(5, 5);

            Assert.IsFalse(graph.IsPath(candidate.x, candidate.y));
            Assert.IsFalse(svc.TryExpand(candidate));
            Assert.IsFalse(graph.IsPath(candidate.x, candidate.y));
            Assert.IsTrue(graph.AllTipsReachHome());
        }

        [Test]
        public void TryExpand_RejectsWhenTipsCannotReachHome()
        {
            var board = CreateDisconnectedTips(out var graph);
            var svc = new MapExpandService(graph, board);
            var candidate = new Vector2Int(5, 3);

            Assert.IsFalse(graph.AllTipsReachHome());
            Assert.IsFalse(svc.TryExpand(candidate));
            Assert.IsFalse(graph.IsPath(candidate.x, candidate.y));
            Assert.IsFalse(graph.AllTipsReachHome());
        }

        [Test]
        public void HasPathGate_UsedByIsLegalExpand_RejectsBrokenCorridorAfterRevert()
        {
            var board = CreateBound(out var graph);
            var svc = new MapExpandService(graph, board);
            Assert.IsTrue(svc.TryExpand(new Vector2Int(3, 4)));

            graph.SetPathTile(3, 3, false);
            Assert.IsFalse(graph.AllTipsReachHome());

            var branch = new Vector2Int(4, 2);
            Assert.IsFalse(svc.TryExpand(branch));
            Assert.IsFalse(graph.IsPath(branch.x, branch.y));
        }

        static GridBoard CreateBound(out PathGraph graph)
        {
            var board = new GridBoard(8, 8);
            graph = new PathGraph(8, 8);
            graph.BindBoard(board);
            graph.SetHome(0, 3);
            for (var x = 0; x <= 7; x++)
                graph.SetPathTile(x, 3, true);
            return board;
        }

        static GridBoard CreateDisconnectedTips(out PathGraph graph)
        {
            // Home island 0–2; remote island 6–7. Expanding (5,3) onto the remote island must fail.
            var board = new GridBoard(8, 8);
            graph = new PathGraph(8, 8);
            graph.BindBoard(board);
            graph.SetHome(0, 3);
            for (var x = 0; x <= 2; x++)
                graph.SetPathTile(x, 3, true);
            graph.SetPathTile(6, 3, true);
            graph.SetPathTile(7, 3, true);
            return board;
        }
    }
}
