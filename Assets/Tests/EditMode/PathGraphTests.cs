using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Grid;

namespace GemTD.Tests.EditMode
{
    public sealed class PathGraphTests
    {
        [Test]
        public void StraightCorridor_AllTipsReachHome()
        {
            var graph = BuildCorridor();
            Assert.IsTrue(graph.AllTipsReachHome());
        }

        [Test]
        public void BrokenCorridor_TipsDoNotReachHome()
        {
            var graph = BuildCorridor();
            graph.SetPathTile(3, 3, false);
            Assert.IsFalse(graph.AllTipsReachHome());
        }

        [Test]
        public void CollectSpawnTips_ExcludesHome_OneTipAtFarEnd()
        {
            var graph = BuildCorridor();
            var tips = new List<Vector2Int>();
            Assert.AreEqual(1, graph.CollectSpawnTips(tips));
            Assert.AreEqual(new Vector2Int(7, 3), tips[0]);
            Assert.IsFalse(tips.Contains(graph.Home));
        }

        [Test]
        public void ExpandAlongTip_MovesSpawnTip()
        {
            var board = new GridBoard(8, 8);
            var graph = new PathGraph(8, 8);
            graph.BindBoard(board);
            graph.SetHome(0, 3);
            for (var x = 0; x <= 7; x++)
                graph.SetPathTile(x, 3, true);

            graph.SetPathTile(7, 4, true);
            var tips = new List<Vector2Int>();
            graph.CollectSpawnTips(tips);
            Assert.AreEqual(1, tips.Count);
            Assert.AreEqual(new Vector2Int(7, 4), tips[0]);
            Assert.IsTrue(graph.AllTipsReachHome());
        }

        [Test]
        public void TryGetWaypointPolyline_TipToHome_StartsAtTipEndsAtHome()
        {
            var graph = BuildCorridor();
            var tip = new Vector2Int(7, 3);
            var path = new List<Vector2Int>();
            Assert.IsTrue(graph.TryGetWaypointPolyline(tip, path));
            Assert.AreEqual(tip, path[0]);
            Assert.AreEqual(graph.Home, path[path.Count - 1]);
        }

        [Test]
        public void BranchCreatesSecondTip()
        {
            var graph = BuildCorridor();
            graph.SetPathTile(3, 4, true);
            graph.SetPathTile(3, 5, true);

            var tips = new List<Vector2Int>();
            graph.CollectSpawnTips(tips);
            Assert.AreEqual(2, tips.Count);
            Assert.Contains(new Vector2Int(7, 3), tips);
            Assert.Contains(new Vector2Int(3, 5), tips);
            Assert.IsTrue(graph.AllTipsReachHome());
        }

        static PathGraph BuildCorridor()
        {
            var graph = new PathGraph(8, 8);
            graph.SetHome(0, 3);
            for (var x = 0; x <= 7; x++)
                graph.SetPathTile(x, 3, true);
            return graph;
        }
    }
}
