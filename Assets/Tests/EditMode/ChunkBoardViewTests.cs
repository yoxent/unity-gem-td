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
