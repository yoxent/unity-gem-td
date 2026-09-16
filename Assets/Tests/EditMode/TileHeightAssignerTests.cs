using System;
using GemTD.Gameplay.Map;
using NUnit.Framework;
using UnityEngine;

namespace GemTD.Tests.EditMode
{
    public class TileHeightAssignerTests
    {
        static ChunkMask TowerPadOnly()
        {
            var path = new bool[ChunkMask.CellCount];
            path[0] = true;
            return new ChunkMask(path);
        }

        static ChunkMask SingleEligible(int lx, int ly)
        {
            var locked = new bool[ChunkMask.CellCount];
            for (var i = 0; i < locked.Length; i++)
                locked[i] = true;
            locked[ly * ChunkMask.Size + lx] = false;
            return new ChunkMask(new bool[ChunkMask.CellCount], -1, locked);
        }

        [Test]
        public void PathCell_StaysLayerZero()
        {
            var map = new TileHeightMap(21, 21);
            TileHeightAssigner.AssignChunk(map, TowerPadOnly(), Vector2Int.zero, new System.Random(1), HeightInfluenceWeights.Default);
            Assert.AreEqual(0, map.Get(0, 0));
            Assert.IsTrue(map.Has(0, 0));
        }

        [Test]
        public void EligibleCells_OnlyZeroOneOrTwo()
        {
            var map = new TileHeightMap(21, 21);
            TileHeightAssigner.AssignChunk(map, TowerPadOnly(), Vector2Int.zero, new System.Random(7), HeightInfluenceWeights.Default);
            for (var y = 0; y < ChunkMask.Size; y++)
            for (var x = 0; x < ChunkMask.Size; x++)
            {
                if (x == 0 && y == 0) continue;
                var h = map.Get(x, y);
                Assert.GreaterOrEqual(h, 0);
                Assert.Less(h, 3);
                Assert.IsTrue(map.Has(x, y));
            }
        }

        [Test]
        public void SameSeed_SameAssignment()
        {
            var a = new TileHeightMap(21, 21);
            var b = new TileHeightMap(21, 21);
            var mask = TowerPadOnly();
            TileHeightAssigner.AssignChunk(a, mask, Vector2Int.zero, new System.Random(99), HeightInfluenceWeights.Default);
            TileHeightAssigner.AssignChunk(b, mask, Vector2Int.zero, new System.Random(99), HeightInfluenceWeights.Default);
            for (var i = 0; i < ChunkMask.CellCount; i++)
                Assert.AreEqual(a.Get(i % ChunkMask.Size, i / ChunkMask.Size), b.Get(i % ChunkMask.Size, i / ChunkMask.Size));
        }

        [Test]
        public void LockedNeighbors_CountAsLayerZero_WhenSameWeightIsOne()
        {
            var map = new TileHeightMap(21, 21);
            var weights = new HeightInfluenceWeights(1f, 0f, 0f);
            TileHeightAssigner.AssignChunk(map, SingleEligible(3, 3), Vector2Int.zero, new System.Random(3), weights);
            Assert.AreEqual(0, map.Get(3, 3));
        }

        [Test]
        public void NormalizeLegal_Tallest_FoldsUpIntoSame()
        {
            HeightInfluenceWeights.Default.NormalizeLegal(2, out var same, out var up, out var down);
            Assert.AreEqual(0f, up);
            Assert.AreEqual(1f, same + down, 1e-5f);
            Assert.Greater(same, down);
        }

        [Test]
        public void NormalizeLegal_Shortest_FoldsDownIntoSame()
        {
            HeightInfluenceWeights.Default.NormalizeLegal(0, out var same, out var up, out var down);
            Assert.AreEqual(0f, down);
            Assert.AreEqual(1f, same + up, 1e-5f);
            Assert.Greater(same, up);
        }

        [Test]
        public void NormalizeLegal_Mid_KeepsDefaultWeights()
        {
            HeightInfluenceWeights.Default.NormalizeLegal(1, out var same, out var up, out var down);
            Assert.AreEqual(0.56f, same, 1e-5f);
            Assert.AreEqual(0.22f, up, 1e-5f);
            Assert.AreEqual(0.22f, down, 1e-5f);
        }

        [Test]
        public void RangeMultiplier_DefaultThenTwentyThenThirty()
        {
            Assert.AreEqual(1f, TileHeightRules.RangeMultiplier(0), 1e-5f);
            Assert.AreEqual(1.2f, TileHeightRules.RangeMultiplier(1), 1e-5f);
            Assert.AreEqual(1.3f, TileHeightRules.RangeMultiplier(2), 1e-5f);
        }

        [Test]
        public void Visual_ScaleAndTopY_MatchLayerSteps()
        {
            Assert.AreEqual(0.25f, TileHeightVisual.ScaleY(0), 1e-5f);
            Assert.AreEqual(0.65f, TileHeightVisual.ScaleY(1), 1e-5f);
            Assert.AreEqual(1.05f, TileHeightVisual.ScaleY(2), 1e-5f);
            Assert.AreEqual(0.075f, TileHeightVisual.LocalPosY(0), 1e-5f);
            Assert.AreEqual(0.275f, TileHeightVisual.LocalPosY(1), 1e-5f);
            Assert.AreEqual(0.475f, TileHeightVisual.LocalPosY(2), 1e-5f);
            Assert.AreEqual(0.20f, TileHeightVisual.TopY(0), 1e-5f);
            Assert.AreEqual(0.60f, TileHeightVisual.TopY(1), 1e-5f);
            Assert.AreEqual(1.00f, TileHeightVisual.TopY(2), 1e-5f);
        }

        [Test]
        public void ShortestPad_TopY_IsPathTopPlusLift()
        {
            var pathTop = TileHeightVisual.PathScaleY * 0.5f;
            Assert.AreEqual(0.15f, TileHeightVisual.PadLift, 1e-5f);
            Assert.AreEqual(pathTop + TileHeightVisual.PadLift, TileHeightVisual.TopY(0), 1e-5f);
        }

        [Test]
        public void TileScaleXz_SubtractsSpacingFromCellSize()
        {
            Assert.AreEqual(0.95f, TileHeightVisual.TileScaleXz(1f, 0.05f), 1e-5f);
            Assert.AreEqual(0.85f, TileHeightVisual.TileScaleXz(1f, 0.15f), 1e-5f);
        }

        [Test]
        public void TileScaleXz_ClampsToMinFootprint()
        {
            Assert.AreEqual(TileHeightVisual.MinFootprint, TileHeightVisual.TileScaleXz(1f, 5f), 1e-5f);
        }

        [Test]
        public void ApplyFootprint_SetsXzKeepsY()
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.transform.localScale = new Vector3(1f, 0.4f, 1f);
            TileHeightVisual.ApplyFootprint(tile.transform, 1f, 0.2f);
            Assert.AreEqual(0.8f, tile.transform.localScale.x, 1e-4f);
            Assert.AreEqual(0.4f, tile.transform.localScale.y, 1e-4f);
            Assert.AreEqual(0.8f, tile.transform.localScale.z, 1e-4f);
            UnityEngine.Object.DestroyImmediate(tile);
        }

        [Test]
        public void TryParseTileName_ReadsPrefabLocalCoords()
        {
            Assert.IsTrue(TileHeightVisual.TryParseTileName("Tile_3_4", out var x, out var y));
            Assert.AreEqual(3, x);
            Assert.AreEqual(4, y);
            Assert.IsFalse(TileHeightVisual.TryParseTileName("Cube", out _, out _));
        }

        [Test]
        public void ApplyPad_SetsScaleAndSharedMaterial()
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "Tile_1_2";
            var renderer = tile.GetComponent<MeshRenderer>();
            var replacement = new Material(renderer.sharedMaterial);
            replacement.name = "H2";
            TileHeightVisual.ApplyPad(tile.transform, 2, replacement);
            Assert.AreSame(replacement, renderer.sharedMaterial);
            Assert.AreEqual(TileHeightVisual.ScaleY(2), tile.transform.localScale.y, 1e-4f);
            Assert.AreEqual(TileHeightVisual.LocalPosY(2), tile.transform.localPosition.y, 1e-4f);
            UnityEngine.Object.DestroyImmediate(tile);
            UnityEngine.Object.DestroyImmediate(replacement);
        }

        [Test]
        public void CreateLayerMaterials_H2BrighterThanH0()
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var source = tile.GetComponent<MeshRenderer>().sharedMaterial;
            var mats = TileHeightVisual.CreateLayerMaterials(source);
            Assert.AreEqual(3, mats.Length);
            var c0 = TileHeightVisual.ReadAlbedo(mats[0]);
            var c2 = TileHeightVisual.ReadAlbedo(mats[2]);
            Assert.Greater(c2.r + c2.g + c2.b, c0.r + c0.g + c0.b);
            for (var i = 0; i < mats.Length; i++)
                UnityEngine.Object.DestroyImmediate(mats[i]);
            UnityEngine.Object.DestroyImmediate(tile);
        }
    }
}
