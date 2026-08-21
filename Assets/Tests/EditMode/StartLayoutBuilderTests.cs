using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Grid;
using GemTD.Gameplay.Map;

namespace GemTD.Tests.EditMode
{
    public class StartLayoutBuilderTests
    {
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

        static MapChunkStamp MakeStraight()
        {
            var go = new GameObject("straight");
            var s = go.AddComponent<MapChunkStamp>();
            var m = new bool[ChunkMask.CellCount];
            for (var y = 0; y < ChunkMask.Size; y++) m[y * ChunkMask.Size + ChunkMask.Mid] = true;
            s.ApplyMask(new ChunkMask(m));
            return s;
        }

        static MapChunkStamp MakeKeep()
        {
            var go = new GameObject("keep");
            var s = go.AddComponent<MapChunkStamp>();
            var m = new bool[ChunkMask.CellCount];
            var mid = ChunkMask.Mid;
            for (var x = mid; x < ChunkMask.Size; x++)
                m[mid * ChunkMask.Size + x] = true;
            var home = mid * ChunkMask.Size + mid;
            s.ApplyMask(new ChunkMask(m, home));
            return s;
        }

        // Painted keeps use the same South-first pose as other painter types.
        static MapChunkStamp MakeKeepSouthOpening() =>
            MakeKeepWithEdges(EdgeFlags.South);

        static MapChunkStamp MakeKeepWithEdges(EdgeFlags edges)
        {
            var go = new GameObject("keep-" + edges);
            var s = go.AddComponent<MapChunkStamp>();
            var m = new bool[ChunkMask.CellCount];
            var mid = ChunkMask.Mid;
            var size = ChunkMask.Size;
            m[mid * size + mid] = true;
            if ((edges & EdgeFlags.South) != 0)
                for (var y = 0; y <= mid; y++) m[y * size + mid] = true;
            if ((edges & EdgeFlags.North) != 0)
                for (var y = mid; y < size; y++) m[y * size + mid] = true;
            if ((edges & EdgeFlags.West) != 0)
                for (var x = 0; x <= mid; x++) m[mid * size + x] = true;
            if ((edges & EdgeFlags.East) != 0)
                for (var x = mid; x < size; x++) m[mid * size + x] = true;
            s.ApplyMask(new ChunkMask(m, mid * size + mid));
            return s;
        }

        ChunkGrid _grid;
        PathGraph _path;
        GridBoard _board;
        ChunkStampService _stamp;
        FakeCatalog _catalog;

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
            _catalog.Stamps.Add(MakeKeep());
            _catalog.Stamps.Add(MakeStraight());
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _catalog.Stamps.Count; i++)
                if (_catalog.Stamps[i] != null) Object.DestroyImmediate(_catalog.Stamps[i].gameObject);
        }

        [Test] public void Build_PlacesFiveChunks_KeepPlusFixedEastArm()
        {
            StartLayoutBuilder.Build(_grid, _stamp, _path, _board, _catalog, new System.Random(1));
            Assert.AreEqual(5, _grid.Count);
            Assert.IsTrue(_grid.IsOccupied(4, 4)); // keep
            Assert.IsTrue(_grid.IsOccupied(5, 4));
            Assert.IsTrue(_grid.IsOccupied(6, 4));
            Assert.IsTrue(_grid.IsOccupied(7, 4));
            Assert.IsTrue(_grid.IsOccupied(8, 4));
            Assert.IsFalse(_grid.IsOccupied(4, 5)); // north empty
            Assert.IsFalse(_grid.IsOccupied(4, 3)); // south empty
            Assert.IsFalse(_grid.IsOccupied(3, 4)); // west empty
        }

        [Test] public void Build_HomeIsPaintedKeepCell()
        {
            StartLayoutBuilder.Build(_grid, _stamp, _path, _board, _catalog, new System.Random(7));
            var keep = new Vector2Int(4, 4);
            var expected = new Vector2Int(
                keep.x * ChunkMask.Size + ChunkMask.Mid,
                keep.y * ChunkMask.Size + ChunkMask.Mid);
            Assert.AreEqual(expected, _path.Home);
            Assert.IsTrue(_path.IsPath(_path.Home.x, _path.Home.y));
        }

        [Test] public void Build_SpawnTipIsOnOuterEdgeOfFourthStraight()
        {
            StartLayoutBuilder.Build(_grid, _stamp, _path, _board, _catalog, new System.Random(2));
            var tips = new List<Vector2Int>();
            Assert.AreEqual(1, _path.CollectSpawnTips(tips));
            // East arm, fourth Straight at (8,4), east-edge middle.
            Assert.AreEqual(
                new Vector2Int(8 * ChunkMask.Size + ChunkMask.Size - 1, 4 * ChunkMask.Size + ChunkMask.Mid),
                tips[0]);
        }

        [Test] public void Build_AllTipsReachHome_True()
        {
            StartLayoutBuilder.Build(_grid, _stamp, _path, _board, _catalog, new System.Random(3));
            Assert.IsTrue(_path.AllTipsReachHome());
        }

        [Test]
        public void Build_RotatesSouthOpeningKeepToFaceEastArm()
        {
            Object.DestroyImmediate(_catalog.Stamps[0].gameObject);
            _catalog.Stamps[0] = MakeKeepSouthOpening();

            StartLayoutBuilder.Build(_grid, _stamp, _path, _board, _catalog, new System.Random(1));

            var keep = new Vector2Int(4, 4);
            Assert.IsTrue(_grid.TryGet(keep.x, keep.y, out var slot));
            Assert.AreNotEqual(0, slot.Mask.OpenEdges & EdgeFlags.East);
            var eastMid = ChunkMask.EdgeMidWorldCell(keep, EdgeFlags.East);
            Assert.IsTrue(_path.IsPath(eastMid.x, eastMid.y));
            Assert.IsTrue(_path.AllTipsReachHome());
        }

        [Test]
        public void Build_PicksOneOpeningKeep_WhenFourOpeningListedFirst()
        {
            var four = MakeKeepWithEdges(
                EdgeFlags.North | EdgeFlags.East | EdgeFlags.South | EdgeFlags.West);
            var one = _catalog.Stamps[0];
            _catalog.Stamps.Insert(0, four);

            StartLayoutBuilder.Build(_grid, _stamp, _path, _board, _catalog, new System.Random(1));

            Assert.IsTrue(_grid.TryGet(4, 4, out var slot));
            Assert.AreSame(one, slot.Prefab);
            Assert.AreEqual(1, slot.Mask.OpenEdges.Count());
        }

        [Test]
        public void Build_OpenArmCountTwo_UsesTwoOpeningKeepAndPlacesEastAndSouthArms()
        {
            var two = MakeKeepWithEdges(EdgeFlags.South | EdgeFlags.East);
            _catalog.Stamps.Add(two);

            StartLayoutBuilder.Build(
                _grid, _stamp, _path, _board, _catalog, new System.Random(1), 2);

            Assert.AreEqual(9, _grid.Count);
            Assert.IsTrue(_grid.TryGet(4, 4, out var slot));
            Assert.AreSame(two, slot.Prefab);
            Assert.AreEqual(EdgeFlags.East | EdgeFlags.South, slot.Mask.OpenEdges);
            Assert.IsTrue(_grid.IsOccupied(8, 4)); // east arm
            Assert.IsTrue(_grid.IsOccupied(4, 0)); // south arm
            Assert.IsFalse(_grid.IsOccupied(4, 5)); // north empty
            Assert.IsFalse(_grid.IsOccupied(3, 4)); // west empty
            Assert.IsTrue(_path.AllTipsReachHome());
        }
    }
}
