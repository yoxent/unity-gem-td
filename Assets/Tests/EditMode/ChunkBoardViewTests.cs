using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Map;

namespace GemTD.Tests.EditMode
{
    public sealed class ChunkBoardViewTests
    {
        const float Cell = 1f;

        static Vector3 TileCenter(int lx, int ly) =>
            new Vector3(lx * Cell + Cell * 0.5f, 0f, ly * Cell + Cell * 0.5f);

        [Test]
        public void ChunkInstanceLocalPosition_Yaw0_IsChunkOrigin()
        {
            var pos = ChunkBoardView.ChunkInstanceLocalPosition(new Vector2Int(4, 5), 0, Cell);
            Assert.AreEqual(new Vector3(20f, 0f, 25f), pos);
        }

        [Test]
        public void TileWorldAfterYaw_Yaw1_NorthOpeningLandsOnEast()
        {
            // Painter North = (2,4). ChunkMask.Rotated(1) sends North -> East (4,2).
            var world = ChunkBoardView.TileWorldAfterYaw(Vector2Int.zero, 1, 2, 4, Cell);
            var expected = TileCenter(4, 2);
            Assert.AreEqual(expected.x, world.x, 0.001f);
            Assert.AreEqual(expected.z, world.z, 0.001f);
        }

        [Test]
        public void TileWorldAfterYaw_Yaw1_WestOpeningLandsOnNorth()
        {
            var world = ChunkBoardView.TileWorldAfterYaw(Vector2Int.zero, 1, 0, 2, Cell);
            var expected = TileCenter(2, 4);
            Assert.AreEqual(expected.x, world.x, 0.001f);
            Assert.AreEqual(expected.z, world.z, 0.001f);
        }

        [Test]
        public void TileWorldAfterYaw_Yaw2_NorthOpeningLandsOnSouth()
        {
            var world = ChunkBoardView.TileWorldAfterYaw(Vector2Int.zero, 2, 2, 4, Cell);
            var expected = TileCenter(2, 0);
            Assert.AreEqual(expected.x, world.x, 0.001f);
            Assert.AreEqual(expected.z, world.z, 0.001f);
        }

        [Test]
        public void TileWorldAfterYaw_NeighborChunks_SharedEdgeMiddlesCoincide()
        {
            // StraightNS at yaw 1 opens E/W. Authored North (2,4) lands on East;
            // authored South (2,0) lands on West. Adjacent chunks must meet at x=4.5 / 5.5, z=2.5.
            var westEastOpening = ChunkBoardView.TileWorldAfterYaw(new Vector2Int(0, 0), 1, 2, 4, Cell);
            var eastWestOpening = ChunkBoardView.TileWorldAfterYaw(new Vector2Int(1, 0), 1, 2, 0, Cell);
            Assert.AreEqual(2.5f, westEastOpening.z, 0.001f);
            Assert.AreEqual(2.5f, eastWestOpening.z, 0.001f);
            Assert.AreEqual(4.5f, westEastOpening.x, 0.001f);
            Assert.AreEqual(5.5f, eastWestOpening.x, 0.001f);
        }
    }
}
