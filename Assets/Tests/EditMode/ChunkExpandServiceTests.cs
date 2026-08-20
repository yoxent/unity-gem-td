using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Grid;
using GemTD.Gameplay.Map;

namespace GemTD.Tests.EditMode
{
    public class ChunkExpandServiceTests
    {
        ChunkGrid _grid;
        PathGraph _path;
        GridBoard _board;
        ChunkStampService _stamp;
        FakeCatalog _catalog;
        System.Random _rng;
        ChunkExpandService _expand;

        sealed class FakeCatalog : IChunkCatalog
        {
            public readonly List<MapChunkStamp> Stamps = new List<MapChunkStamp>();
            public void CopyAll(List<MapChunkStamp> into)
            {
                into.Clear();
                for (var i = 0; i < Stamps.Count; i++) into.Add(Stamps[i]);
            }

            public void CopyType(ChunkType type, List<MapChunkStamp> into)
            {
                into.Clear();
                for (var i = 0; i < Stamps.Count; i++)
                {
                    var s = Stamps[i];
                    if (s != null && s.GetMask().Type == type) into.Add(s);
                }
            }
        }

        static MapChunkStamp MakeCross()
        {
            var go = new GameObject("cross");
            var s = go.AddComponent<MapChunkStamp>();
            var m = new bool[ChunkMask.CellCount];
            var mid = ChunkMask.Mid;
            for (var i = 0; i < ChunkMask.Size; i++)
            {
                m[i * ChunkMask.Size + mid] = true;
                m[mid * ChunkMask.Size + i] = true;
            }
            s.ApplyMask(new ChunkMask(m));
            return s;
        }

        static MapChunkStamp MakeStraight()
        {
            var go = new GameObject("straight");
            var s = go.AddComponent<MapChunkStamp>();
            var m = new bool[ChunkMask.CellCount];
            for (var y = 0; y < ChunkMask.Size; y++) m[y * ChunkMask.Size + ChunkMask.Mid] = true;
            s.ApplyMask(new ChunkMask(m));
            return s;
        }

        static MapChunkStamp MakeDeadEnd()
        {
            var go = new GameObject("deadend");
            var s = go.AddComponent<MapChunkStamp>();
            var m = new bool[ChunkMask.CellCount];
            var mid = ChunkMask.Mid;
            // North opening down to center spawn tip
            for (var y = mid; y < ChunkMask.Size; y++)
                m[y * ChunkMask.Size + mid] = true;
            s.ApplyMask(new ChunkMask(m));
            return s;
        }

        static MapChunkStamp MakeCorner()
        {
            var go = new GameObject("corner");
            var s = go.AddComponent<MapChunkStamp>();
            var m = new bool[ChunkMask.CellCount];
            var mid = ChunkMask.Mid;
            for (var y = mid; y < ChunkMask.Size; y++) m[y * ChunkMask.Size + mid] = true;
            for (var x = 0; x <= mid; x++) m[mid * ChunkMask.Size + x] = true;
            s.ApplyMask(new ChunkMask(m));
            return s;
        }

        // T: East + West + South (closed North) — fills a 3-opening pocket with no outward arm.
        static MapChunkStamp MakeTJunction()
        {
            var go = new GameObject("t");
            var s = go.AddComponent<MapChunkStamp>();
            var m = new bool[ChunkMask.CellCount];
            var mid = ChunkMask.Mid;
            for (var x = 0; x < ChunkMask.Size; x++) m[mid * ChunkMask.Size + x] = true;
            for (var y = 0; y <= mid; y++) m[y * ChunkMask.Size + mid] = true;
            s.ApplyMask(new ChunkMask(m));
            return s;
        }

        // Unconnectable prefab: a single path cell at the center. Rotation-invariant,
        // so OpenEdges=None for every yaw.
        static MapChunkStamp MakeDisconnected()
        {
            var go = new GameObject("disc");
            var s = go.AddComponent<MapChunkStamp>();
            var m = new bool[ChunkMask.CellCount];
            m[ChunkMask.Mid * ChunkMask.Size + ChunkMask.Mid] = true;
            s.ApplyMask(new ChunkMask(m));
            return s;
        }

        [SetUp]
        public void SetUp()
        {
            var cells = 9 * ChunkMask.Size;
            _board = new GridBoard(cells, cells);
            _path = new PathGraph(cells, cells);
            _path.BindBoard(_board);
            _grid = new ChunkGrid(9, 9);
            _stamp = new ChunkStampService();
            _catalog = new FakeCatalog();
            _rng = new System.Random(0);
            _expand = new ChunkExpandService(_grid, _path, _board, _stamp, _catalog, _rng);
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _catalog.Stamps.Count; i++)
                if (_catalog.Stamps[i] != null)
                    Object.DestroyImmediate(_catalog.Stamps[i].gameObject);
        }

        void StampKeep()
        {
            var land = new GameObject("land");
            var s = land.AddComponent<MapChunkStamp>();
            s.ApplyMask(new ChunkMask(new bool[ChunkMask.CellCount]));
            var r = _stamp.StampTentative(new Vector2Int(4, 4), s, 0, _path, _board);
            _stamp.Commit(new Vector2Int(4, 4), s, 0, r.Mask, _grid);
            Object.DestroyImmediate(land);
        }

        void StampLand(Vector2Int coord)
        {
            var land = new GameObject("land");
            var s = land.AddComponent<MapChunkStamp>();
            s.ApplyMask(new ChunkMask(new bool[ChunkMask.CellCount]));
            var r = _stamp.StampTentative(coord, s, 0, _path, _board);
            _stamp.Commit(coord, s, 0, r.Mask, _grid);
            Object.DestroyImmediate(land);
        }

        void StampStraight(Vector2Int coord, int yaw)
        {
            var prefab = MakeStraight();
            _catalog.Stamps.Add(prefab);
            var r = _stamp.StampTentative(coord, prefab, yaw, _path, _board);
            _stamp.Commit(coord, prefab, yaw, r.Mask, _grid);
        }

        // Keep + one Straight corridor north of keep, with Home set to the corridor's
        // inner path cell (south tip) touching the keep.
        void StampCorridorNorthWithHome()
        {
            StampKeep();
            _catalog.Stamps.Add(MakeStraight());
            StampStraight(new Vector2Int(4, 5), yaw: 0); // N|S straight; north tip open
            _path.SetHome(4 * ChunkMask.Size + ChunkMask.Mid, 5 * ChunkMask.Size + 0);
        }

        void StampCrossCorridorWithHome()
        {
            StampKeep();
            var cross = MakeCross();
            var r = _stamp.StampTentative(new Vector2Int(4, 5), cross, 0, _path, _board);
            _stamp.Commit(new Vector2Int(4, 5), cross, 0, r.Mask, _grid);
            Object.DestroyImmediate(cross.gameObject);
            _path.SetHome(4 * ChunkMask.Size + ChunkMask.Mid, 5 * ChunkMask.Size + 0);
        }

        void StampEastWestLoopGap()
        {
            StampCrossCorridorWithHome();
            StampStraight(new Vector2Int(3, 5), 1);
            StampStraight(new Vector2Int(3, 6), 1);
            StampStraight(new Vector2Int(5, 5), 1);
            StampStraight(new Vector2Int(5, 6), 1);
        }

        [Test] public void CollectLegalExpands_EmptyGrid_ReturnsZero()
        {
            var into = new List<Vector2Int>();
            Assert.AreEqual(0, _expand.CollectLegalExpands(into));
        }

        [Test] public void CollectLegalExpands_KeepOnly_ReturnsZero()
        {
            StampKeep();
            var into = new List<Vector2Int>();
            Assert.AreEqual(0, _expand.CollectLegalExpands(into));
        }

        [Test] public void CollectLegalExpands_StraightCorridor_ReturnsOuterTipSlot()
        {
            StampCorridorNorthWithHome();

            var into = new List<Vector2Int>();
            _expand.CollectLegalExpands(into);

            Assert.IsTrue(into.Contains(new Vector2Int(4, 6)));
        }

        [Test] public void CollectLegalExpands_RejectsUnconnectablePrefab()
        {
            StampCorridorNorthWithHome();
            // Now offer ONLY an unconnectable prefab as the candidate -> must be rejected.
            _catalog.Stamps.Clear();
            _catalog.Stamps.Add(MakeDisconnected());

            var into = new List<Vector2Int>();
            _expand.CollectLegalExpands(into);

            Assert.IsFalse(into.Contains(new Vector2Int(4, 6)));
        }

        [Test] public void TryExpand_LegalSlot_PicksPassingComboAndCommits()
        {
            StampCorridorNorthWithHome();

            Assert.IsTrue(_expand.TryExpand(new Vector2Int(4, 6)));
            Assert.IsTrue(_grid.IsOccupied(4, 6));
        }

        [Test] public void TryExpand_UnconnectablePrefab_ReturnsFalse_NoMutation()
        {
            StampCorridorNorthWithHome();
            _catalog.Stamps.Clear();
            _catalog.Stamps.Add(MakeDisconnected());

            var before = _grid.Count;
            Assert.IsFalse(_expand.TryExpand(new Vector2Int(4, 6)));
            Assert.AreEqual(before, _grid.Count);
            Assert.IsFalse(_grid.IsOccupied(4, 6));
        }

        [Test]
        public void CollectLegalExpands_RejectsPrefabThatOpensAgainstClosedNeighbor()
        {
            StampCorridorNorthWithHome();
            StampLand(new Vector2Int(5, 6)); // closed east face of the north expand slot
            _catalog.Stamps.Clear();
            _catalog.Stamps.Add(MakeCross());

            var into = new List<Vector2Int>();
            _expand.CollectLegalExpands(into);

            Assert.IsFalse(into.Contains(new Vector2Int(4, 6)));
        }

        [Test]
        public void TryExpand_RejectsPrefabThatOpensAgainstClosedNeighbor()
        {
            StampCorridorNorthWithHome();
            StampLand(new Vector2Int(5, 6));
            _catalog.Stamps.Clear();
            _catalog.Stamps.Add(MakeCross());

            var before = _grid.Count;
            Assert.IsFalse(_expand.TryExpand(new Vector2Int(4, 6)));
            Assert.AreEqual(before, _grid.Count);
        }

        [Test]
        public void TryExpand_StraightAtTip_EdgesAgreeWithOccupiedNeighbors()
        {
            StampCorridorNorthWithHome();
            _catalog.Stamps.Clear();
            _catalog.Stamps.Add(MakeStraight());

            Assert.IsTrue(_expand.TryExpand(new Vector2Int(4, 6)));
            Assert.IsTrue(EdgesAgree(_grid, new Vector2Int(4, 6)));
        }

        [Test]
        public void CollectLegalExpands_AfterInteriorCross_OffersAllThreeOpenSides()
        {
            StampCorridorNorthWithHome();
            StampCross(new Vector2Int(4, 6), yaw: 0);
            _catalog.Stamps.Add(MakeStraight());

            var into = new List<Vector2Int>();
            _expand.CollectLegalExpands(into);

            Assert.IsTrue(into.Contains(new Vector2Int(4, 7)), "north opening");
            Assert.IsTrue(into.Contains(new Vector2Int(5, 6)), "east opening");
            Assert.IsTrue(into.Contains(new Vector2Int(3, 6)), "west opening");
        }

        [Test]
        public void CollectLegalExpands_RejectsCrossThatOpensOffMap()
        {
            StampKeep();
            StampStraight(new Vector2Int(4, 5), 0);
            StampStraight(new Vector2Int(4, 6), 0);
            StampStraight(new Vector2Int(4, 7), 0);
            _path.SetHome(4 * ChunkMask.Size + ChunkMask.Mid, 5 * ChunkMask.Size + 0);
            _catalog.Stamps.Clear();
            _catalog.Stamps.Add(MakeCross());

            var into = new List<Vector2Int>();
            _expand.CollectLegalExpands(into);

            Assert.IsFalse(into.Contains(new Vector2Int(4, 8)));
        }

        [Test]
        public void CollectLegalExpands_AcceptsCornerOnBorderThatClosesTheRim()
        {
            StampKeep();
            StampStraight(new Vector2Int(4, 5), 0);
            StampStraight(new Vector2Int(4, 6), 0);
            StampStraight(new Vector2Int(4, 7), 0);
            _path.SetHome(4 * ChunkMask.Size + ChunkMask.Mid, 5 * ChunkMask.Size + 0);
            _catalog.Stamps.Clear();
            _catalog.Stamps.Add(MakeCorner()); // N+W; yaw 2 -> S+E, closed on the north rim

            var into = new List<Vector2Int>();
            _expand.CollectLegalExpands(into);

            Assert.IsTrue(into.Contains(new Vector2Int(4, 8)));
        }

        [Test]
        public void CollectLegalExpands_RejectsLoopCloserWithNoOutwardArm()
        {
            StampEastWestLoopGap();
            _catalog.Stamps.Clear();
            _catalog.Stamps.Add(MakeStraight()); // E-W at yaw 1 closes the gap with no outward arm

            var into = new List<Vector2Int>();
            _expand.CollectLegalExpands(into);

            Assert.IsFalse(into.Contains(new Vector2Int(4, 6)));
        }

        [Test]
        public void CollectLegalExpands_ThreeNeighborGap_TJunctionIsLegal()
        {
            // Same pocket as the loop-gap: west, east, and south all open into (4,6).
            // A T (E+W+S) matches every opening but has no empty-facing arm.
            StampEastWestLoopGap();
            _catalog.Stamps.Clear();
            _catalog.Stamps.Add(MakeTJunction());

            var into = new List<Vector2Int>();
            _expand.CollectLegalExpands(into);

            Assert.IsTrue(into.Contains(new Vector2Int(4, 6)));
        }

        [Test]
        public void CollectLegalExpands_TwoNeighborMerge_StraightRejected()
        {
            StampKeep();
            StampStraight(new Vector2Int(3, 6), 1);
            StampStraight(new Vector2Int(5, 6), 1);
            _path.SetHome(3 * ChunkMask.Size + 0, 6 * ChunkMask.Size + ChunkMask.Mid);
            _catalog.Stamps.Clear();
            _catalog.Stamps.Add(MakeStraight());

            var into = new List<Vector2Int>();
            _expand.CollectLegalExpands(into);

            Assert.IsFalse(into.Contains(new Vector2Int(4, 6)));
        }

        [Test]
        public void TryExpand_RejectsLoopCloserWithNoOutwardArm()
        {
            StampEastWestLoopGap();
            _catalog.Stamps.Clear();
            _catalog.Stamps.Add(MakeStraight());

            var before = _grid.Count;
            Assert.IsFalse(_expand.TryExpand(new Vector2Int(4, 6)));
            Assert.AreEqual(before, _grid.Count);
        }

        [Test]
        public void CollectLegalExpands_AcceptsDeadEndAtOpenTip()
        {
            StampCorridorNorthWithHome();
            _catalog.Stamps.Clear();
            _catalog.Stamps.Add(MakeDeadEnd());

            var into = new List<Vector2Int>();
            _expand.CollectLegalExpands(into);

            Assert.IsTrue(into.Contains(new Vector2Int(4, 6)));
        }

        [Test]
        public void TryExpand_DeadEndAtTip_CommitsAndCenterIsSpawnTip()
        {
            StampCorridorNorthWithHome();
            _catalog.Stamps.Clear();
            _catalog.Stamps.Add(MakeDeadEnd());

            Assert.IsTrue(_expand.TryExpand(new Vector2Int(4, 6)));
            Assert.IsTrue(_grid.IsOccupied(4, 6));
            Assert.IsTrue(_grid.TryGet(4, 6, out var slot));
            Assert.AreEqual(ChunkType.DeadEnd, slot.Mask.Type);

            var tips = new List<Vector2Int>();
            _path.CollectSpawnTips(tips);
            Assert.Contains(new Vector2Int(4 * ChunkMask.Size + ChunkMask.Mid, 6 * ChunkMask.Size + ChunkMask.Mid), tips);

            var into = new List<Vector2Int>();
            _expand.CollectLegalExpands(into);
            Assert.IsFalse(into.Contains(new Vector2Int(4, 7)));
        }

        [Test]
        public void CollectLegalExpands_AcceptsLoopWhenExpandedChunkKeepsOutwardArm()
        {
            StampEastWestLoopGap();
            _catalog.Stamps.Clear();
            _catalog.Stamps.Add(MakeCross()); // W+E+S connect the loop; north stays open

            var into = new List<Vector2Int>();
            _expand.CollectLegalExpands(into);

            Assert.IsTrue(into.Contains(new Vector2Int(4, 6)));
        }

        void StampCross(Vector2Int coord, int yaw)
        {
            var prefab = MakeCross();
            _catalog.Stamps.Add(prefab);
            var r = _stamp.StampTentative(coord, prefab, yaw, _path, _board);
            _stamp.Commit(coord, prefab, yaw, r.Mask, _grid);
        }

        static bool EdgesAgree(ChunkGrid grid, Vector2Int coord)
        {
            if (!grid.TryGet(coord.x, coord.y, out var slot)) return false;
            var dirs = new[] { EdgeFlags.North, EdgeFlags.East, EdgeFlags.South, EdgeFlags.West };
            for (var d = 0; d < dirs.Length; d++)
            {
                var dir = dirs[d];
                var nb = grid.NeighborCoord(coord, dir);
                if (!grid.TryGet(nb.x, nb.y, out var neighbor)) continue;
                var newOpen = (slot.Mask.OpenEdges & dir) != 0;
                var nbOpen = (neighbor.Mask.OpenEdges & dir.Opposite()) != 0;
                if (newOpen != nbOpen) return false;
            }
            return true;
        }
    }
}
