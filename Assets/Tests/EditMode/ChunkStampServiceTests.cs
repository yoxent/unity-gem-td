using NUnit.Framework;
using UnityEngine;
using GemTD.Core;
using GemTD.Gameplay.Grid;
using GemTD.Gameplay.Map;

namespace GemTD.Tests.EditMode
{
    public class ChunkStampServiceTests
    {
        static ChunkMask StraightNS()
        {
            var m = new bool[ChunkMask.CellCount];
            for (var y = 0; y < ChunkMask.Size; y++) m[y * ChunkMask.Size + ChunkMask.Mid] = true;
            return new ChunkMask(m);
        }

        static ChunkMask Land() => new ChunkMask(new bool[ChunkMask.CellCount]);

        static MapChunkStamp MakeStamp(ChunkMask mask)
        {
            var go = new GameObject("stamp");
            var stamp = go.AddComponent<MapChunkStamp>();
            stamp.ApplyMask(mask);
            return stamp;
        }

        [TearDown]
        public void TearDown() => GameEvents.ClearAll();

        [Test] public void StampTentative_WritesRotatedMaskIntoPath()
        {
            var board = new GridBoard(9 * ChunkMask.Size, 9 * ChunkMask.Size);
            var path = new PathGraph(9 * ChunkMask.Size, 9 * ChunkMask.Size);
            path.BindBoard(board);
            var stamp = new ChunkStampService();
            var prefab = MakeStamp(StraightNS()); // N|S column x=Mid

            var coord = new Vector2Int(4, 4);
            var res = stamp.StampTentative(coord, prefab, yaw: 1, path, board); // yaw1 -> E|W

            for (var lx = 0; lx < ChunkMask.Size; lx++)
                for (var ly = 0; ly < ChunkMask.Size; ly++)
                {
                    var wx = coord.x * ChunkMask.Size + lx;
                    var wy = coord.y * ChunkMask.Size + ly;
                    Assert.AreEqual(res.Mask.IsPath(lx, ly), path.IsPath(wx, wy));
                }
            Object.DestroyImmediate(prefab.gameObject);
        }

        [Test] public void GetRotatedMask_MatchesChunkMaskRotated()
        {
            var prefab = MakeStamp(StraightNS());
            var expected = prefab.GetMask().Rotated(1);
            var cached = prefab.GetRotatedMask(1);
            for (var y = 0; y < ChunkMask.Size; y++)
                for (var x = 0; x < ChunkMask.Size; x++)
                    Assert.AreEqual(expected.IsPath(x, y), cached.IsPath(x, y), $"mismatch at {x},{y}");
            Assert.AreEqual(expected.OpenEdges, cached.OpenEdges);
            Object.DestroyImmediate(prefab.gameObject);
        }

        [Test] public void Rollback_RestoresPriorPathCells()
        {
            var board = new GridBoard(9 * ChunkMask.Size, 9 * ChunkMask.Size);
            var path = new PathGraph(9 * ChunkMask.Size, 9 * ChunkMask.Size);
            path.BindBoard(board);
            var stamp = new ChunkStampService();
            var prefab = MakeStamp(StraightNS());

            var coord = new Vector2Int(4, 4);
            path.SetPathTile(coord.x * ChunkMask.Size + 0, coord.y * ChunkMask.Size + 0, true);
            var res = stamp.StampTentative(coord, prefab, yaw: 0, path, board);

            stamp.Rollback(coord, res, path, board);

            Assert.IsTrue(path.IsPath(coord.x * ChunkMask.Size + 0, coord.y * ChunkMask.Size + 0));
            Object.DestroyImmediate(prefab.gameObject);
        }

        [Test] public void Rollback_RestoresUnbuildableOnVoid()
        {
            var board = new GridBoard(9 * ChunkMask.Size, 9 * ChunkMask.Size);
            var path = new PathGraph(9 * ChunkMask.Size, 9 * ChunkMask.Size);
            path.BindBoard(board);
            var stamp = new ChunkStampService();
            var prefab = MakeStamp(StraightNS());

            var coord = new Vector2Int(4, 4);
            var res = stamp.StampTentative(coord, prefab, yaw: 0, path, board);
            stamp.Rollback(coord, res, path, board);

            for (var lx = 0; lx < ChunkMask.Size; lx++)
                for (var ly = 0; ly < ChunkMask.Size; ly++)
                {
                    var wx = coord.x * ChunkMask.Size + lx;
                    var wy = coord.y * ChunkMask.Size + ly;
                    Assert.IsFalse(path.IsPath(wx, wy));
                    Assert.IsFalse(board.IsBuildable(wx, wy),
                        $"void cell ({wx},{wy}) should stay unbuildable after rollback");
                }
            Object.DestroyImmediate(prefab.gameObject);
        }

        [Test] public void Stamp_LandChunk_AllCellsBuildable()
        {
            var board = new GridBoard(9 * ChunkMask.Size, 9 * ChunkMask.Size);
            var path = new PathGraph(9 * ChunkMask.Size, 9 * ChunkMask.Size);
            path.BindBoard(board);
            var stamp = new ChunkStampService();
            var prefab = MakeStamp(Land());

            var coord = new Vector2Int(4, 4);
            stamp.StampTentative(coord, prefab, yaw: 0, path, board);

            for (var lx = 0; lx < ChunkMask.Size; lx++)
                for (var ly = 0; ly < ChunkMask.Size; ly++)
                {
                    var wx = coord.x * ChunkMask.Size + lx;
                    var wy = coord.y * ChunkMask.Size + ly;
                    Assert.IsFalse(path.IsPath(wx, wy));
                    Assert.IsTrue(board.IsBuildable(wx, wy));
                }
            Object.DestroyImmediate(prefab.gameObject);
        }

        [Test] public void Stamp_PathChunk_PathCellsUnbuildable_TowerCellsBuildable()
        {
            var board = new GridBoard(9 * ChunkMask.Size, 9 * ChunkMask.Size);
            var path = new PathGraph(9 * ChunkMask.Size, 9 * ChunkMask.Size);
            path.BindBoard(board);
            var stamp = new ChunkStampService();
            var prefab = MakeStamp(StraightNS());

            var coord = new Vector2Int(4, 4);
            var res = stamp.StampTentative(coord, prefab, yaw: 0, path, board);

            for (var lx = 0; lx < ChunkMask.Size; lx++)
                for (var ly = 0; ly < ChunkMask.Size; ly++)
                {
                    var wx = coord.x * ChunkMask.Size + lx;
                    var wy = coord.y * ChunkMask.Size + ly;
                    if (res.Mask.IsPath(lx, ly))
                    {
                        Assert.IsTrue(path.IsPath(wx, wy));
                        Assert.IsFalse(board.IsBuildable(wx, wy));
                    }
                    else
                    {
                        Assert.IsFalse(path.IsPath(wx, wy));
                        Assert.IsTrue(board.IsBuildable(wx, wy));
                    }
                }
            Object.DestroyImmediate(prefab.gameObject);
        }

        [Test] public void Commit_RaisesChunkPlacedEvent()
        {
            var board = new GridBoard(9 * ChunkMask.Size, 9 * ChunkMask.Size);
            var path = new PathGraph(9 * ChunkMask.Size, 9 * ChunkMask.Size);
            path.BindBoard(board);
            var grid = new ChunkGrid(9, 9);
            var stamp = new ChunkStampService();
            var prefab = MakeStamp(StraightNS());

            var coord = new Vector2Int(4, 4);
            var res = stamp.StampTentative(coord, prefab, yaw: 0, path, board);

            Vector2Int? raised = null;
            GameEvents.ChunkPlaced += c => raised = c;
            stamp.Commit(coord, prefab, yaw: 0, res.Mask, grid);

            Assert.AreEqual(coord, raised);
            Assert.IsTrue(grid.IsOccupied(coord.x, coord.y));
            Object.DestroyImmediate(prefab.gameObject);
        }

        [Test]
        public void Ctor_UnsetWeights_UseDefaultLookDevSplit()
        {
            var stamp = new ChunkStampService();
            Assert.AreEqual(0.56f, stamp.Weights.Same, 1e-5f);
            Assert.AreEqual(0.22f, stamp.Weights.StepUp, 1e-5f);
            Assert.AreEqual(0.22f, stamp.Weights.StepDown, 1e-5f);
        }

        [Test]
        public void Ctor_ExplicitWeights_AreStored()
        {
            var weights = new HeightInfluenceWeights(1f, 0f, 0f);
            var stamp = new ChunkStampService(null, null, weights);
            Assert.AreEqual(1f, stamp.Weights.Same, 1e-5f);
            Assert.AreEqual(0f, stamp.Weights.StepUp, 1e-5f);
        }

        [Test]
        public void StampTentative_DoesNotWriteHeights()
        {
            var board = new GridBoard(9 * ChunkMask.Size, 9 * ChunkMask.Size);
            var path = new PathGraph(9 * ChunkMask.Size, 9 * ChunkMask.Size);
            path.BindBoard(board);
            var heights = new TileHeightMap(board.Width, board.Height);
            var stamp = new ChunkStampService(heights, new System.Random(1));
            var prefab = MakeStamp(StraightNS());
            var coord = new Vector2Int(4, 4);

            stamp.StampTentative(coord, prefab, yaw: 0, path, board);

            Assert.IsFalse(heights.Has(coord.x * ChunkMask.Size, coord.y * ChunkMask.Size));
            Object.DestroyImmediate(prefab.gameObject);
        }

        [Test]
        public void Commit_AssignsEligibleHeights_PathStaysZero()
        {
            var board = new GridBoard(9 * ChunkMask.Size, 9 * ChunkMask.Size);
            var path = new PathGraph(9 * ChunkMask.Size, 9 * ChunkMask.Size);
            path.BindBoard(board);
            var grid = new ChunkGrid(9, 9);
            var heights = new TileHeightMap(board.Width, board.Height);
            var stamp = new ChunkStampService(heights, new System.Random(1));
            var prefab = MakeStamp(StraightNS());
            var coord = new Vector2Int(4, 4);
            var res = stamp.StampTentative(coord, prefab, yaw: 0, path, board);

            stamp.Commit(coord, prefab, yaw: 0, res.Mask, grid);

            var pathWx = coord.x * ChunkMask.Size + ChunkMask.Mid;
            var pathWy = coord.y * ChunkMask.Size + 0;
            Assert.AreEqual(0, heights.Get(pathWx, pathWy));
            Assert.IsTrue(heights.Has(pathWx, pathWy));

            var padWx = coord.x * ChunkMask.Size + 0;
            var padWy = coord.y * ChunkMask.Size + 0;
            Assert.IsTrue(heights.Has(padWx, padWy));
            Assert.Less(heights.Get(padWx, padWy), 3);
            Object.DestroyImmediate(prefab.gameObject);
        }
    }
}
