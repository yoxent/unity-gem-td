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
        public void TryGetWaypointPolyline_RepeatedCalls_SameResult()
        {
            var graph = BuildCorridor();
            var tip = new Vector2Int(7, 3);
            var first = new List<Vector2Int>();
            var second = new List<Vector2Int>();
            Assert.IsTrue(graph.TryGetWaypointPolyline(tip, first));
            Assert.IsTrue(graph.TryGetWaypointPolyline(tip, second));
            Assert.AreEqual(first, second);
        }

        [Test]
        public void AllTipsReachHome_UnreachableTip_False()
        {
            var graph = BuildCorridor();
            graph.SetPathTile(0, 0, true);
            graph.SetPathTile(0, 1, true);
            Assert.IsFalse(graph.AllTipsReachHome());
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

        [Test]
        public void HopDistanceFromHome_StraightCorridor_EqualsCellDistance()
        {
            var graph = BuildCorridor();
            Assert.AreEqual(0, graph.HopDistanceFromHome(graph.Home));
            Assert.AreEqual(7, graph.HopDistanceFromHome(new Vector2Int(7, 3)));
        }

        [Test]
        public void HopDistanceFromHome_Unreachable_ReturnsNegativeOne()
        {
            var graph = BuildCorridor();
            Assert.AreEqual(-1, graph.HopDistanceFromHome(new Vector2Int(0, 0)));
        }

        [Test]
        public void RankTipsByHopDescending_FurthestTipFirst()
        {
            var graph = BuildCorridor();
            graph.SetPathTile(2, 4, true);
            graph.SetPathTile(2, 5, true);

            var tips = new List<Vector2Int>();
            graph.CollectSpawnTips(tips);
            Assert.AreEqual(2, tips.Count);

            var ranked = new List<Vector2Int>();
            graph.RankTipsByHopDescending(tips, ranked);

            Assert.AreEqual(2, ranked.Count);
            // (7,3) is 7 hops from home; (2,5) is 2 (to x=2,y=3) + 2 (up to y=5) = 4 hops.
            Assert.AreEqual(new Vector2Int(7, 3), ranked[0]);
            Assert.AreEqual(new Vector2Int(2, 5), ranked[1]);
        }

        [Test]
        public void RankTipsByHopDescending_EqualHop_TiebreaksByXThenYDescending()
        {
            // Two branches at equal hop distance from home; only coord tiebreak differs.
            var board = new GridBoard(8, 8);
            var graph = new PathGraph(8, 8);
            graph.BindBoard(board);
            graph.SetHome(0, 0);
            graph.SetPathTile(0, 0, true);
            graph.SetPathTile(1, 0, true);
            graph.SetPathTile(1, 1, true);
            graph.SetPathTile(0, 1, true);

            var tips = new List<Vector2Int> { new Vector2Int(1, 1), new Vector2Int(0, 1) };
            var ranked = new List<Vector2Int>();
            graph.RankTipsByHopDescending(tips, ranked);

            Assert.AreEqual(2, graph.HopDistanceFromHome(new Vector2Int(1, 1)));
            Assert.AreEqual(1, graph.HopDistanceFromHome(new Vector2Int(0, 1)));
            // Different hop distances here, so higher-hop tip still wins first regardless of coord.
            Assert.AreEqual(new Vector2Int(1, 1), ranked[0]);

            // Now build an explicit equal-hop tie to check the x-desc, y-desc tiebreak in isolation.
            var equalHopTips = new List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(1, 0) };
            var equalHopRanked = new List<Vector2Int>();
            graph.RankTipsByHopDescending(equalHopTips, equalHopRanked);
            Assert.AreEqual(1, graph.HopDistanceFromHome(new Vector2Int(0, 1)));
            Assert.AreEqual(1, graph.HopDistanceFromHome(new Vector2Int(1, 0)));
            Assert.AreEqual(new Vector2Int(1, 0), equalHopRanked[0], "Equal hop — higher x wins.");
            Assert.AreEqual(new Vector2Int(0, 1), equalHopRanked[1]);
        }

        [Test]
        public void RankTipsByHopDescending_DoesNotMutateInputList()
        {
            var graph = BuildCorridor();
            var tips = new List<Vector2Int> { new Vector2Int(7, 3) };
            var snapshot = new List<Vector2Int>(tips);
            var ranked = new List<Vector2Int>();

            graph.RankTipsByHopDescending(tips, ranked);

            Assert.AreEqual(snapshot, tips);
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
