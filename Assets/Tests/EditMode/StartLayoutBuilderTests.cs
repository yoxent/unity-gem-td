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
        }

        static MapChunkStamp MakeStraight()
        {
            var go = new GameObject("straight");
            var s = go.AddComponent<MapChunkStamp>();
            var m = new bool[ChunkMask.CellCount];
            for (var y = 0; y < ChunkMask.Size; y++) m[y * ChunkMask.Size + 2] = true;
            s.ApplyMask(new ChunkMask(m));
            return s;
        }

        static MapChunkStamp MakeLand()
        {
            var go = new GameObject("land");
            var s = go.AddComponent<MapChunkStamp>();
            s.ApplyMask(new ChunkMask(new bool[ChunkMask.CellCount]));
            return s;
        }

        ChunkGrid _grid;
        PathGraph _path;
        GridBoard _board;
        ChunkStampService _stamp;
        FakeCatalog _catalog;
        MapChunkStamp _land;

        [SetUp]
        public void SetUp()
        {
            _board = new GridBoard(45, 45);
            _path = new PathGraph(45, 45);
            _path.BindBoard(_board);
            _grid = new ChunkGrid(9, 9);
            _stamp = new ChunkStampService();
            _catalog = new FakeCatalog();
            _catalog.Stamps.Add(MakeStraight());
            _land = MakeLand();
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _catalog.Stamps.Count; i++)
                if (_catalog.Stamps[i] != null) Object.DestroyImmediate(_catalog.Stamps[i].gameObject);
            if (_land != null) Object.DestroyImmediate(_land.gameObject);
        }

        [Test] public void Build_PlacesFiveChunks_KeepPlusFixedEastArm()
        {
            StartLayoutBuilder.Build(_grid, _stamp, _path, _board, _catalog, _land, new System.Random(1));
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

        [Test] public void Build_HomeIsGateSinkCell_FirstStraightInnerMiddle()
        {
            StartLayoutBuilder.Build(_grid, _stamp, _path, _board, _catalog, _land, new System.Random(7));
            // East arm, first Straight at (5,4), west-edge middle.
            Assert.AreEqual(new Vector2Int(25, 22), _path.Home);
            Assert.IsTrue(_path.IsPath(_path.Home.x, _path.Home.y));
        }

        [Test] public void Build_SpawnTipIsOnOuterEdgeOfFourthStraight()
        {
            StartLayoutBuilder.Build(_grid, _stamp, _path, _board, _catalog, _land, new System.Random(2));
            var tips = new List<Vector2Int>();
            Assert.AreEqual(1, _path.CollectSpawnTips(tips));
            // East arm, fourth Straight at (8,4), east-edge middle.
            Assert.AreEqual(new Vector2Int(44, 22), tips[0]);
        }

        [Test] public void Build_AllTipsReachHome_True()
        {
            StartLayoutBuilder.Build(_grid, _stamp, _path, _board, _catalog, _land, new System.Random(3));
            Assert.IsTrue(_path.AllTipsReachHome());
        }

        [Test]
        public void Build_OpenArmCountTwo_StillPlacesOnlyEastArm()
        {
            StartLayoutBuilder.Build(
                _grid, _stamp, _path, _board, _catalog, _land, new System.Random(1), 2);
            Assert.AreEqual(5, _grid.Count);
            Assert.IsTrue(_grid.IsOccupied(5, 4));
            Assert.IsTrue(_grid.IsOccupied(8, 4));
            Assert.IsFalse(_grid.IsOccupied(4, 3)); // south must stay empty this pass
            Assert.AreEqual(new Vector2Int(25, 22), _path.Home);
        }
    }
}
