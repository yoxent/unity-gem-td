using NUnit.Framework;
using GemTD.Gameplay.Map;

namespace GemTD.Tests.EditMode
{
    public class ChunkMaskIdTests
    {
        const int Size = ChunkMask.Size;
        const int Mid = ChunkMask.Mid;

        static bool[] Empty() => new bool[Size * Size];

        static bool[] CanonicalNeL()
        {
            var m = Empty();
            for (var y = Mid; y < Size; y++)
                m[y * Size + Mid] = true;
            for (var x = Mid; x < Size; x++)
                m[Mid * Size + x] = true;
            return m;
        }

        [Test]
        public void Encode_Is49Chars_PathO_TowerX()
        {
            var id = ChunkMaskId.Encode(new ChunkMask(Empty()));
            Assert.AreEqual(ChunkMask.CellCount, id.Length);
            for (var i = 0; i < id.Length; i++)
                Assert.AreEqual(ChunkMaskId.Tower, id[i]);
        }

        [Test]
        public void Encode_MarksPathCells()
        {
            var m = Empty();
            m[Mid] = true;
            var id = ChunkMaskId.Encode(new ChunkMask(m));
            Assert.AreEqual(ChunkMaskId.Path, id[Mid]);
        }

        [Test]
        public void Encode_LockedTower_DiffersFromUnlocked()
        {
            var path = Empty();
            var locked = Empty();
            locked[0] = true;
            var unlockedId = ChunkMaskId.Encode(new ChunkMask(path));
            var lockedId = ChunkMaskId.Encode(new ChunkMask(path, -1, locked));
            Assert.AreEqual(ChunkMaskId.Tower, unlockedId[0]);
            Assert.AreEqual(ChunkMaskId.LockedTower, lockedId[0]);
            Assert.AreNotEqual(unlockedId, lockedId);
        }

        [Test]
        public void Encode_PathCell_StaysPathEvenIfLocked()
        {
            var path = Empty();
            path[Mid] = true;
            var locked = Empty();
            locked[Mid] = true;
            var id = ChunkMaskId.Encode(new ChunkMask(path, -1, locked));
            Assert.AreEqual(ChunkMaskId.Path, id[Mid]);
        }

        [Test]
        public void Encode_HomeCell_StaysPath()
        {
            var home = Mid * Size + Mid;
            var id = ChunkMaskId.Encode(new ChunkMask(Empty(), home));
            Assert.AreEqual(ChunkMaskId.Path, id[home]);
        }

        [Test]
        public void Canonical_YawVariants_AreEqual()
        {
            var mask = new ChunkMask(CanonicalNeL());
            var a = ChunkMaskId.Canonical(mask);
            Assert.AreNotEqual(ChunkMaskId.Encode(mask), ChunkMaskId.Encode(mask.Rotated(1)));
            Assert.AreEqual(a, ChunkMaskId.Canonical(mask.Rotated(1)));
            Assert.AreEqual(a, ChunkMaskId.Canonical(mask.Rotated(2)));
            Assert.AreEqual(a, ChunkMaskId.Canonical(mask.Rotated(3)));
        }

        [Test]
        public void Canonical_LockedTower_RotatesEqual()
        {
            var path = Empty();
            for (var y = Mid; y < Size; y++)
                path[y * Size + Mid] = true;
            for (var x = Mid; x < Size; x++)
                path[Mid * Size + x] = true;
            var locked = Empty();
            locked[0] = true;
            var mask = new ChunkMask(path, -1, locked);
            Assert.AreEqual(
                ChunkMaskId.Canonical(mask),
                ChunkMaskId.Canonical(mask.Rotated(1)));
        }

        [Test]
        public void Canonical_DifferentInteriors_AreNotEqual()
        {
            var l = new ChunkMask(CanonicalNeL());
            var island = Empty();
            for (var y = 1; y <= 5; y++)
            {
                for (var x = 1; x <= 5; x++)
                {
                    var hole = x >= 2 && x <= 4 && y >= 2 && y <= 4;
                    if (!hole) island[y * Size + x] = true;
                }
            }
            island[(Size - 1) * Size + Mid] = true;
            island[Mid * Size + (Size - 1)] = true;
            Assert.AreNotEqual(
                ChunkMaskId.Canonical(l),
                ChunkMaskId.Canonical(new ChunkMask(island)));
        }
    }
}
