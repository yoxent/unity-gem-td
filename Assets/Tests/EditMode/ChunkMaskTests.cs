using NUnit.Framework;
using GemTD.Gameplay.Map;

namespace GemTD.Tests.EditMode
{
    public class ChunkMaskTests
    {
        const int Size = 5;
        static bool[] Empty() => new bool[Size * Size];
        static bool[] Set(params (int x, int y)[] path)
        {
            var m = Empty();
            for (var i = 0; i < path.Length; i++)
                m[path[i].y * Size + path[i].x] = true;
            return m;
        }

        [Test] public void AllTower_IsLand_NoOpenings()
        {
            var mask = new ChunkMask(Empty());
            Assert.AreEqual(ChunkType.Land, mask.Type);
            Assert.AreEqual(EdgeFlags.None, mask.OpenEdges);
        }

        [Test] public void MiddleNorthOpen_IsDeadEnd()
        {
            var mask = new ChunkMask(Set((2, 4)));
            Assert.AreEqual(EdgeFlags.North, mask.OpenEdges);
            Assert.AreEqual(ChunkType.DeadEnd, mask.Type);
        }

        [Test] public void MiddleNAndS_IsStraight()
        {
            var mask = new ChunkMask(Set((2, 0), (2, 4)));
            Assert.AreEqual(EdgeFlags.North | EdgeFlags.South, mask.OpenEdges);
            Assert.AreEqual(ChunkType.Straight, mask.Type);
        }

        [Test] public void MiddleNAndE_IsCorner()
        {
            var mask = new ChunkMask(Set((2, 4), (4, 2)));
            Assert.AreEqual(EdgeFlags.North | EdgeFlags.East, mask.OpenEdges);
            Assert.AreEqual(ChunkType.Corner, mask.Type);
        }

        [Test] public void ThreeOpenings_IsTJunction()
        {
            var mask = new ChunkMask(Set((2, 4), (4, 2), (2, 0)));
            Assert.AreEqual(ChunkType.TJunction, mask.Type);
        }

        [Test] public void FourOpenings_IsCross()
        {
            var mask = new ChunkMask(Set((2, 4), (4, 2), (2, 0), (0, 2)));
            Assert.AreEqual(ChunkType.Cross, mask.Type);
        }

        [Test] public void NonMiddleEdgePath_NotAnOpening()
        {
            var mask = new ChunkMask(Set((0, 4)));
            Assert.AreEqual(EdgeFlags.None, mask.OpenEdges);
        }

        [Test] public void Rotated90_NorthBecomesEast()
        {
            var mask = new ChunkMask(Set((2, 4)));
            Assert.AreEqual(EdgeFlags.East, mask.Rotated(1).OpenEdges);
        }

        [Test] public void ConnectedCorridor_AreOpeningsConnected_True()
        {
            var mask = new ChunkMask(Set((2, 0), (2, 1), (2, 2), (2, 3), (2, 4)));
            Assert.IsTrue(mask.AreOpeningsConnected());
        }

        [Test] public void DisconnectedOpenings_AreOpeningsConnected_False()
        {
            var mask = new ChunkMask(Set((2, 0), (2, 4)));
            Assert.IsFalse(mask.AreOpeningsConnected());
        }
    }
}
