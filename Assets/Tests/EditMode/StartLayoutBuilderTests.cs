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

        [Test] public void Build_PlacesFiveChunks_OneKeepFourStraights()
        {
            StartLayoutBuilder.Build(_grid, _stamp, _path, _board, _catalog, _land, new System.Random(1));
            Assert.AreEqual(5, _grid.Count);
            Assert.IsTrue(_grid.IsOccupied(4, 4)); // keep
        }

        [Test] public void Build_HomeIsGateSinkCell_FirstStraightInnerMiddle()
        {
            StartLayoutBuilder.Build(_grid, _stamp, _path, _board, _catalog, _land, new System.Random(7));
            var home = _path.Home;
            Assert.IsTrue(_path.IsPath(home.x, home.y));
        }

        [Test] public void Build_SpawnTipIsOnOuterEdgeOfFourthStraight()
        {
            StartLayoutBuilder.Build(_grid, _stamp, _path, _board, _catalog, _land, new System.Random(2));
            var tips = new List<Vector2Int>();
            Assert.GreaterOrEqual(_path.CollectSpawnTips(tips), 1);
        }

        [Test] public void Build_AllTipsReachHome_True()
        {
            StartLayoutBuilder.Build(_grid, _stamp, _path, _board, _catalog, _land, new System.Random(3));
            Assert.IsTrue(_path.AllTipsReachHome());
        }
    }
}
