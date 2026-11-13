using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Map;

namespace GemTD.Tests.EditMode
{
    public sealed class ChunkBoardViewTests
    {
        const float Cell = 1f;
        const int Mid = ChunkMask.Mid;
        const int Size = ChunkMask.Size;

        static Vector3 TileCenter(int lx, int ly) =>
            new Vector3(lx * Cell + Cell * 0.5f, 0f, ly * Cell + Cell * 0.5f);

        [Test]
        public void OnChunkPlaced_ScalesPadTilesAndLeavesPathTilesFull()
        {
            var stampObject = new GameObject("stamp");
            var viewObject = new GameObject("board");
            try
            {
                var stamp = stampObject.AddComponent<MapChunkStamp>();
                var buildable = new GameObject("Tile_0_0");
                buildable.transform.SetParent(stampObject.transform, false);
                buildable.transform.localScale = Vector3.one;
                new GameObject("PadHeight1").transform.SetParent(buildable.transform, false);

                var path = new GameObject("Tile_1_0");
                path.transform.SetParent(stampObject.transform, false);
                path.transform.localScale = Vector3.one;
                new GameObject("PathTile").transform.SetParent(path.transform, false);

                var isPath = new bool[ChunkMask.CellCount];
                isPath[1] = true;
                var grid = new ChunkGrid(1, 1);
                grid.Place(Vector2Int.zero, new ChunkSlot(stamp, 0, new ChunkMask(isPath)));

                var view = viewObject.AddComponent<ChunkBoardView>();
                view.Bind(grid, null, 0.2f);
                view.OnChunkPlaced(Vector2Int.zero);

                var placed = viewObject.transform.GetChild(0);
                var placedBuildable = placed.Find("Tile_0_0");
                var placedPath = placed.Find("Tile_1_0");
                Assert.AreEqual(0.8f, placedBuildable.localScale.x, 1e-4f);
                Assert.AreEqual(1f, placedBuildable.localScale.y, 1e-4f);
                Assert.AreEqual(0.8f, placedBuildable.localScale.z, 1e-4f);
                Assert.AreEqual(1f, placedPath.localScale.x, 1e-4f);
                Assert.AreEqual(1f, placedPath.localScale.y, 1e-4f);
                Assert.AreEqual(1f, placedPath.localScale.z, 1e-4f);

                view.SetTileSpacing(0.4f);
                Assert.AreEqual(0.6f, placedBuildable.localScale.x, 1e-4f);
                Assert.AreEqual(0.6f, placedBuildable.localScale.z, 1e-4f);
                Assert.AreEqual(1f, placedPath.localScale.x, 1e-4f);
                Assert.AreEqual(1f, placedPath.localScale.z, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(stampObject);
            }
        }

        [Test]
        public void ChunkInstanceLocalPosition_Yaw0_IsChunkOrigin()
        {
            var pos = ChunkBoardView.ChunkInstanceLocalPosition(new Vector2Int(4, 5), 0, Cell);
            Assert.AreEqual(new Vector3(4 * Size, 0f, 5 * Size), pos);
        }

        [Test]
        public void TileWorldAfterYaw_Yaw1_NorthOpeningLandsOnEast()
        {
            // Painter North = (Mid, Size-1). ChunkMask.Rotated(1) sends North -> East.
            var world = ChunkBoardView.TileWorldAfterYaw(Vector2Int.zero, 1, Mid, Size - 1, Cell);
            var expected = TileCenter(Size - 1, Mid);
            Assert.AreEqual(expected.x, world.x, 0.001f);
            Assert.AreEqual(expected.z, world.z, 0.001f);
        }

        [Test]
        public void TileWorldAfterYaw_Yaw1_WestOpeningLandsOnNorth()
        {
            var world = ChunkBoardView.TileWorldAfterYaw(Vector2Int.zero, 1, 0, Mid, Cell);
            var expected = TileCenter(Mid, Size - 1);
            Assert.AreEqual(expected.x, world.x, 0.001f);
            Assert.AreEqual(expected.z, world.z, 0.001f);
        }

        [Test]
        public void TileWorldAfterYaw_Yaw2_NorthOpeningLandsOnSouth()
        {
            var world = ChunkBoardView.TileWorldAfterYaw(Vector2Int.zero, 2, Mid, Size - 1, Cell);
            var expected = TileCenter(Mid, 0);
            Assert.AreEqual(expected.x, world.x, 0.001f);
            Assert.AreEqual(expected.z, world.z, 0.001f);
        }

        [Test]
        public void TileWorldAfterYaw_NeighborChunks_SharedEdgeMiddlesCoincide()
        {
            // StraightNS at yaw 1 opens E/W. Authored North lands on East; South on West.
            var westEastOpening = ChunkBoardView.TileWorldAfterYaw(new Vector2Int(0, 0), 1, Mid, Size - 1, Cell);
            var eastWestOpening = ChunkBoardView.TileWorldAfterYaw(new Vector2Int(1, 0), 1, Mid, 0, Cell);
            Assert.AreEqual(Mid + 0.5f, westEastOpening.z, 0.001f);
            Assert.AreEqual(Mid + 0.5f, eastWestOpening.z, 0.001f);
            Assert.AreEqual(Size - 0.5f, westEastOpening.x, 0.001f);
            Assert.AreEqual(Size + 0.5f, eastWestOpening.x, 0.001f);
        }
    }
}
