using System.Collections.Generic;
using NUnit.Framework;
using GemTD.Gameplay.Map;

namespace GemTD.Tests.EditMode
{
    public class ChunkMaskGeneratorTests
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
        public void CanonicalNeL_IsLegalCorner()
        {
            Assert.IsTrue(ChunkPathRules.IsLegalCorner(new ChunkMask(CanonicalNeL())));
        }

        [Test]
        public void TwoByTwoBlock_IsNotLegalCorner()
        {
            var m = CanonicalNeL();
            m[(Mid + 1) * Size + (Mid + 1)] = true;
            Assert.IsFalse(ChunkPathRules.IsLegalCorner(new ChunkMask(m)));
        }

        [Test]
        public void BalloonLoopOnL_IsNotLegalCorner()
        {
            var m = CanonicalNeL();
            // 1x1 island at (2,2) whose ring attaches only at the L corner (3,3).
            m[1 * Size + 1] = true;
            m[1 * Size + 2] = true;
            m[1 * Size + 3] = true;
            m[2 * Size + 1] = true;
            m[3 * Size + 1] = true;
            m[3 * Size + 2] = true;
            Assert.IsFalse(ChunkPathRules.IsLegalCorner(new ChunkMask(m)));
        }

        [Test]
        public void EditorLockedEdges_Corner_IsSouthEast()
        {
            Assert.AreEqual(EdgeFlags.South | EdgeFlags.East,
                ChunkPathRules.EditorLockedEdges(ChunkType.Corner));
        }

        [Test]
        public void CenterIslandRing_SeStems_IsLegalCornerAndEditorLocked()
        {
            var m = Empty();
            for (var y = 1; y <= 5; y++)
            {
                for (var x = 1; x <= 5; x++)
                {
                    var island = x >= 2 && x <= 4 && y >= 2 && y <= 4;
                    if (!island) m[y * Size + x] = true;
                }
            }
            m[Mid] = true;
            m[Mid * Size + (Size - 1)] = true;
            var mask = new ChunkMask(m);
            Assert.IsTrue(ChunkPathRules.IsLegalCorner(mask));
            Assert.IsTrue(ChunkPathRules.HasEditorLockedEdges(mask));
        }

        [Test]
        public void GenerateCorner_ManySeeds_AllLegalSouthEast()
        {
            for (var seed = 1; seed <= 40; seed++)
            {
                Assert.IsTrue(
                    ChunkMaskGenerator.TryGenerate(ChunkType.Corner, new System.Random(seed), out var mask),
                    "seed " + seed);
                Assert.IsTrue(ChunkPathRules.IsLegalGenerated(mask, ChunkType.Corner), "seed " + seed);
                Assert.AreEqual(EdgeFlags.South | EdgeFlags.East, mask.OpenEdges, "seed " + seed);
            }
        }

        [Test]
        public void GenerateCorner_ExcludeEncode_DoesNotReturnBannedLayout()
        {
            var se = Empty();
            for (var y = 0; y <= Mid; y++)
                se[y * Size + Mid] = true;
            for (var x = Mid; x < Size; x++)
                se[Mid * Size + x] = true;
            var banned = ChunkMaskId.Canonical(new ChunkMask(se));
            var exclude = new HashSet<string> { banned };
            var any = false;
            for (var seed = 1; seed <= 20; seed++)
            {
                if (!ChunkMaskGenerator.TryGenerate(ChunkType.Corner, new System.Random(seed), out var mask, exclude))
                    continue;
                any = true;
                Assert.AreNotEqual(banned, ChunkMaskId.Canonical(mask), "seed " + seed);
            }
            Assert.IsTrue(any, "expected at least one unique corner besides the locked L");
        }

        [Test]
        public void GenerateStraight_ManySeeds_AllLegalNorthSouth()
        {
            AssertMany(ChunkType.Straight, EdgeFlags.North | EdgeFlags.South);
        }

        [Test]
        public void GenerateTJunction_ManySeeds_AllLegalSouthEastWest()
        {
            AssertMany(ChunkType.TJunction, EdgeFlags.South | EdgeFlags.East | EdgeFlags.West);
        }

        [Test]
        public void GenerateCross_ManySeeds_AllLegalFourOpenings()
        {
            AssertMany(ChunkType.Cross,
                EdgeFlags.North | EdgeFlags.East | EdgeFlags.South | EdgeFlags.West);
        }

        static void AssertMany(ChunkType type, EdgeFlags edges)
        {
            for (var seed = 1; seed <= 40; seed++)
            {
                Assert.IsTrue(
                    ChunkMaskGenerator.TryGenerate(type, new System.Random(seed), out var mask),
                    type + " seed " + seed);
                Assert.IsTrue(ChunkPathRules.IsLegalGenerated(mask, type), type + " seed " + seed);
                Assert.AreEqual(edges, mask.OpenEdges, type + " seed " + seed);
            }
        }
    }
}
