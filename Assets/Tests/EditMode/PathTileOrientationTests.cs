using NUnit.Framework;
using GemTD.Gameplay.Map;

namespace GemTD.Tests.EditMode
{
    public sealed class PathTileOrientationTests
    {
        const int Mid = ChunkMask.Mid;
        const int Size = ChunkMask.Size;

        const int Bend = 0;
        const int Cross = 1;
        const int End = 2;
        const int Split = 3;
        const int Straight = 4;

        static bool[] Empty() => new bool[ChunkMask.CellCount];

        static bool[] Set(params (int x, int y)[] path)
        {
            var cells = Empty();
            for (var i = 0; i < path.Length; i++)
                cells[path[i].y * Size + path[i].x] = true;
            return cells;
        }

        static EdgeFlags[] Kenney() => new[]
        {
            EdgeFlags.South | EdgeFlags.East,
            EdgeFlags.North | EdgeFlags.East | EdgeFlags.South | EdgeFlags.West,
            EdgeFlags.South,
            EdgeFlags.West | EdgeFlags.East | EdgeFlags.South,
            EdgeFlags.North | EdgeFlags.South
        };

        [Test]
        public void RotatedCW_Bend_CyclesAdjacentPairs()
        {
            var bend = EdgeFlags.South | EdgeFlags.East;
            Assert.AreEqual(EdgeFlags.South | EdgeFlags.East, bend.RotatedCW(0));
            Assert.AreEqual(EdgeFlags.West | EdgeFlags.South, bend.RotatedCW(1));
            Assert.AreEqual(EdgeFlags.North | EdgeFlags.West, bend.RotatedCW(2));
            Assert.AreEqual(EdgeFlags.East | EdgeFlags.North, bend.RotatedCW(3));
            Assert.AreEqual(bend, bend.RotatedCW(4));
            Assert.AreEqual(bend.RotatedCW(3), bend.RotatedCW(-1));
        }

        [Test]
        public void OpeningsAt_StraightCorridor_IsNorthSouthIncludingRims()
        {
            var cells = Empty();
            for (var y = 0; y < Size; y++)
                cells[y * Size + Mid] = true;
            var mask = new ChunkMask(cells);

            Assert.AreEqual(EdgeFlags.North | EdgeFlags.South, PathTileOrientation.OpeningsAt(mask, Mid, 0));
            Assert.AreEqual(EdgeFlags.North | EdgeFlags.South, PathTileOrientation.OpeningsAt(mask, Mid, Mid));
            Assert.AreEqual(EdgeFlags.North | EdgeFlags.South, PathTileOrientation.OpeningsAt(mask, Mid, Size - 1));
        }

        [Test]
        public void OpeningsAt_SouthCap_IsSouthOnly()
        {
            var mask = new ChunkMask(Set((Mid, 0)));
            Assert.AreEqual(EdgeFlags.South, PathTileOrientation.OpeningsAt(mask, Mid, 0));
        }

        [Test]
        public void OpeningsAt_InteriorDeadEnd_IgnoresRim()
        {
            var mask = new ChunkMask(Set((Mid, 0), (Mid, 1), (Mid, 2)));
            Assert.AreEqual(EdgeFlags.South, PathTileOrientation.OpeningsAt(mask, Mid, 2));
            Assert.AreEqual(EdgeFlags.North | EdgeFlags.South, PathTileOrientation.OpeningsAt(mask, Mid, 0));
        }

        [Test]
        public void OpeningsAt_CornerAndArms()
        {
            var mask = new ChunkMask(Set(
                (Mid, 0), (Mid, 1), (Mid, 2), (Mid, Mid),
                (Mid + 1, Mid), (Mid + 2, Mid), (Size - 1, Mid)));

            Assert.AreEqual(EdgeFlags.South | EdgeFlags.East, PathTileOrientation.OpeningsAt(mask, Mid, Mid));
            Assert.AreEqual(EdgeFlags.East | EdgeFlags.West, PathTileOrientation.OpeningsAt(mask, Size - 1, Mid));
        }

        [Test]
        public void OpeningsAt_SplitAndCrossCenters()
        {
            var split = new ChunkMask(Set(
                (Mid - 1, Mid), (Mid, Mid), (Mid + 1, Mid),
                (Mid, Mid - 1), (Mid, 1), (Mid, 0)));
            Assert.AreEqual(
                EdgeFlags.West | EdgeFlags.East | EdgeFlags.South,
                PathTileOrientation.OpeningsAt(split, Mid, Mid));

            var cells = Empty();
            for (var i = 0; i < Size; i++)
            {
                cells[Mid * Size + i] = true;
                cells[i * Size + Mid] = true;
            }
            var cross = new ChunkMask(cells);
            Assert.AreEqual(
                EdgeFlags.North | EdgeFlags.East | EdgeFlags.South | EdgeFlags.West,
                PathTileOrientation.OpeningsAt(cross, Mid, Mid));
        }

        [Test]
        public void OpeningsAt_TowerCell_IsNone()
        {
            var mask = new ChunkMask(Set((Mid, 0), (Mid, 1)));
            Assert.AreEqual(EdgeFlags.None, PathTileOrientation.OpeningsAt(mask, 0, 0));
        }

        [Test]
        public void TryResolve_KenneyPoses_MatchLowestYaw()
        {
            var kit = Kenney();
            AssertResolve(kit, EdgeFlags.South | EdgeFlags.East, Bend, 0);
            AssertResolve(kit, EdgeFlags.West | EdgeFlags.South, Bend, 1);
            AssertResolve(kit, EdgeFlags.North | EdgeFlags.West, Bend, 2);
            AssertResolve(kit, EdgeFlags.East | EdgeFlags.North, Bend, 3);

            AssertResolve(kit, EdgeFlags.South, End, 0);
            AssertResolve(kit, EdgeFlags.West, End, 1);
            AssertResolve(kit, EdgeFlags.North, End, 2);
            AssertResolve(kit, EdgeFlags.East, End, 3);

            AssertResolve(kit, EdgeFlags.North | EdgeFlags.South, Straight, 0);
            AssertResolve(kit, EdgeFlags.East | EdgeFlags.West, Straight, 1);

            AssertResolve(kit, EdgeFlags.West | EdgeFlags.East | EdgeFlags.South, Split, 0);
            AssertResolve(kit, EdgeFlags.North | EdgeFlags.East | EdgeFlags.South | EdgeFlags.West, Cross, 0);
        }

        [Test]
        public void TryResolve_NoneAndEmpty_Fail()
        {
            Assert.IsFalse(PathTileOrientation.TryResolve(EdgeFlags.None, Kenney(), out _, out _));
            Assert.IsFalse(PathTileOrientation.TryResolve(EdgeFlags.South, null, out _, out _));
            Assert.IsFalse(PathTileOrientation.TryResolve(EdgeFlags.South, new EdgeFlags[5], out _, out _));
        }

        static void AssertResolve(EdgeFlags[] kit, EdgeFlags required, int piece, int yaw)
        {
            Assert.IsTrue(PathTileOrientation.TryResolve(required, kit, out var gotPiece, out var gotYaw));
            Assert.AreEqual(piece, gotPiece);
            Assert.AreEqual(yaw, gotYaw);
        }
    }
}
