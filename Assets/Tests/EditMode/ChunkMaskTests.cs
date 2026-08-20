using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Map;

namespace GemTD.Tests.EditMode
{
    public class ChunkMaskTests
    {
        const int Size = ChunkMask.Size;
        const int Mid = ChunkMask.Mid;
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
            var mask = new ChunkMask(Set((Mid, Size - 1)));
            Assert.AreEqual(EdgeFlags.North, mask.OpenEdges);
            Assert.AreEqual(ChunkType.DeadEnd, mask.Type);
        }

        [Test] public void EdgeMidWorldCell_NorthPortalOfChunk45()
        {
            var cell = ChunkMask.EdgeMidWorldCell(new Vector2Int(4, 5), EdgeFlags.North);
            Assert.AreEqual(new Vector2Int(4 * Size + Mid, 5 * Size + Size - 1), cell);
        }

        [Test] public void EdgeMidWorldCell_SouthPortalOfChunk45()
        {
            var cell = ChunkMask.EdgeMidWorldCell(new Vector2Int(4, 5), EdgeFlags.South);
            Assert.AreEqual(new Vector2Int(4 * Size + Mid, 5 * Size), cell);
        }

        [Test] public void AdjacentExpandCell_NorthOpening_IsOneCellBeyondPortal()
        {
            var occupied = new Vector2Int(4, 5);
            var portal = ChunkMask.EdgeMidWorldCell(occupied, EdgeFlags.North);
            var adjacent = ChunkMask.AdjacentExpandCell(occupied, EdgeFlags.North);
            Assert.AreEqual(new Vector2Int(4 * Size + Mid, 5 * Size + Size - 1), portal);
            Assert.AreEqual(new Vector2Int(4 * Size + Mid, 5 * Size + Size), adjacent);
        }

        [Test] public void MiddleNAndS_IsStraight()
        {
            var mask = new ChunkMask(Set((Mid, 0), (Mid, Size - 1)));
            Assert.AreEqual(EdgeFlags.North | EdgeFlags.South, mask.OpenEdges);
            Assert.AreEqual(ChunkType.Straight, mask.Type);
        }

        [Test] public void MiddleNAndE_IsCorner()
        {
            var mask = new ChunkMask(Set((Mid, Size - 1), (Size - 1, Mid)));
            Assert.AreEqual(EdgeFlags.North | EdgeFlags.East, mask.OpenEdges);
            Assert.AreEqual(ChunkType.Corner, mask.Type);
        }

        [Test] public void ThreeOpenings_IsTJunction()
        {
            var mask = new ChunkMask(Set((Mid, Size - 1), (Size - 1, Mid), (Mid, 0)));
            Assert.AreEqual(ChunkType.TJunction, mask.Type);
        }

        [Test] public void FourOpenings_IsCross()
        {
            var mask = new ChunkMask(Set((Mid, Size - 1), (Size - 1, Mid), (Mid, 0), (0, Mid)));
            Assert.AreEqual(ChunkType.Cross, mask.Type);
        }

        [Test] public void NonMiddleEdgePath_NotAnOpening()
        {
            var mask = new ChunkMask(Set((0, Size - 1)));
            Assert.AreEqual(EdgeFlags.None, mask.OpenEdges);
        }

        [Test] public void Rotated90_NorthBecomesEast()
        {
            var mask = new ChunkMask(Set((Mid, Size - 1)));
            Assert.AreEqual(EdgeFlags.East, mask.Rotated(1).OpenEdges);
        }

        [Test] public void ConnectedCorridor_AreOpeningsConnected_True()
        {
            var cells = new (int x, int y)[Size];
            for (var y = 0; y < Size; y++) cells[y] = (Mid, y);
            var mask = new ChunkMask(Set(cells));
            Assert.IsTrue(mask.AreOpeningsConnected());
        }

        [Test] public void DisconnectedOpenings_AreOpeningsConnected_False()
        {
            var mask = new ChunkMask(Set((Mid, 0), (Mid, Size - 1)));
            Assert.IsFalse(mask.AreOpeningsConnected());
        }

        [Test] public void HomeCell_IsForcedPath_AndTypeIsHomebase()
        {
            var home = Mid * Size + Mid;
            var mask = new ChunkMask(Empty(), home);
            Assert.IsTrue(mask.HasHome);
            Assert.AreEqual(new Vector2Int(Mid, Mid), mask.HomeLocal);
            Assert.IsTrue(mask.IsPath(Mid, Mid));
            Assert.AreEqual(ChunkType.Homebase, mask.Type);
        }

        [Test] public void HomeWithEastOpening_IsHomebase_NotDeadEnd()
        {
            var home = Mid * Size + Mid;
            var mask = new ChunkMask(Set((Mid, Mid), (Size - 1, Mid)), home);
            Assert.AreEqual(ChunkType.Homebase, mask.Type);
            Assert.AreEqual(EdgeFlags.East, mask.OpenEdges);
        }

        [Test] public void HomeWithFourOpenings_IsHomebase_NotCross()
        {
            var home = Mid * Size + Mid;
            var mask = new ChunkMask(Set(
                (Mid, Mid),
                (Mid, 0), (Mid, Size - 1), (0, Mid), (Size - 1, Mid)), home);
            Assert.AreEqual(ChunkType.Homebase, mask.Type);
            Assert.AreEqual(
                EdgeFlags.North | EdgeFlags.East | EdgeFlags.South | EdgeFlags.West,
                mask.OpenEdges);
        }

        [Test] public void Rotated90_HomeNorthMid_BecomesEastMid()
        {
            var home = (Size - 1) * Size + Mid;
            var mask = new ChunkMask(Set((Mid, Size - 1)), home);
            var rotated = mask.Rotated(1);
            Assert.IsTrue(rotated.HasHome);
            Assert.AreEqual(new Vector2Int(Size - 1, Mid), rotated.HomeLocal);
        }

        [Test] public void PathAndHome_AreElevationLocked()
        {
            var home = Mid * Size + Mid;
            var mask = new ChunkMask(Set((Mid, Mid), (Size - 1, Mid)), home);
            Assert.IsTrue(mask.IsElevationLocked(Mid, Mid));
            Assert.IsTrue(mask.IsElevationLocked(Size - 1, Mid));
        }

        [Test] public void TowerElevationLock_RotatesWithMask()
        {
            var locks = EmptyBool();
            locks[Mid] = true; // local (Mid, 0) south mid
            var mask = new ChunkMask(Empty(), -1, locks);
            Assert.IsTrue(mask.IsElevationLocked(Mid, 0));
            var rotated = mask.Rotated(1);
            // (Mid,0) -> (0, Size-1-Mid) = (0, Mid) west mid
            Assert.IsTrue(rotated.IsElevationLocked(0, Mid));
            Assert.IsFalse(rotated.IsElevationLocked(Mid, 0));
        }

        static bool[] EmptyBool() => new bool[Size * Size];
    }
}
