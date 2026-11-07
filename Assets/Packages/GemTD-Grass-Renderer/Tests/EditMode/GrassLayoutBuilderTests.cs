using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Grass;

namespace GemTD.GrassRenderer.Tests.EditMode
{
    public class GrassLayoutBuilderTests
    {
        static GrassSurfacePatch Patch(
            int stableKey,
            Vector3 center,
            Vector2 halfExtents,
            GrassPatchEdges insetEdges = GrassPatchEdges.None,
            GrassPatchEdges cliffEdges = GrassPatchEdges.None)
        {
            return new GrassSurfacePatch(center, halfExtents, insetEdges, cliffEdges, stableKey);
        }

        static GrassLayoutSettings Settings(
            float density = 1f,
            float minScale = 0.9f,
            float maxScale = 1.1f,
            float edgeInset = 0f,
            float edgeThinChance = 0f,
            float cliffDensityMultiplier = 1f,
            float[] variantWeights = null)
        {
            return new GrassLayoutSettings
            {
                ClumpsPerSquareUnit = density,
                MinScale = minScale,
                MaxScale = maxScale,
                EdgeInset = edgeInset,
                EdgeThinChance = edgeThinChance,
                CliffDensityMultiplier = cliffDensityMultiplier,
                VariantWeights = variantWeights
            };
        }

        static void AssertInstancesEqual(IReadOnlyList<GrassInstance> expected, IReadOnlyList<GrassInstance> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            for (var i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i].LocalPosition, actual[i].LocalPosition, $"Position mismatch at {i}.");
                Assert.AreEqual(expected[i].YawDegrees, actual[i].YawDegrees, $"Yaw mismatch at {i}.");
                Assert.AreEqual(expected[i].UniformScale, actual[i].UniformScale, $"Scale mismatch at {i}.");
                Assert.AreEqual(expected[i].VariantIndex, actual[i].VariantIndex, $"Variant mismatch at {i}.");
            }
        }

        static void AssertFinite(float value)
        {
            Assert.IsFalse(float.IsNaN(value));
            Assert.IsFalse(float.IsInfinity(value));
        }

        [Test]
        public void Build_SameInputAndSeed_ProducesIdenticalInstances()
        {
            var patches = new[]
            {
                Patch(10, new Vector3(2f, 0f, -1f), new Vector2(2f, 2f)),
                Patch(20, new Vector3(-3f, 1f, 4f), new Vector2(1.5f, 1f), cliffEdges: GrassPatchEdges.North)
            };
            var settings = Settings(density: 0.8f, minScale: 0.75f, maxScale: 1.25f, cliffDensityMultiplier: 1.5f, variantWeights: new[] { 1f, 2f, 3f });
            var first = new List<GrassInstance>();
            var second = new List<GrassInstance>();

            GrassLayoutBuilder.Build(patches, null, in settings, 12345, first);
            GrassLayoutBuilder.Build(patches, null, in settings, 12345, second);

            AssertInstancesEqual(first, second);
        }

        [Test]
        public void Build_DifferentSeed_ChangesAtLeastOneTransform()
        {
            var patches = new[] { Patch(10, new Vector3(0f, 0f, 0f), new Vector2(2f, 2f)) };
            var settings = Settings(density: 0.75f, minScale: 0.6f, maxScale: 1.4f);
            var first = new List<GrassInstance>();
            var second = new List<GrassInstance>();

            GrassLayoutBuilder.Build(patches, null, in settings, 1, first);
            GrassLayoutBuilder.Build(patches, null, in settings, 2, second);

            Assert.AreEqual(first.Count, second.Count);

            var changed = false;
            for (var i = 0; i < first.Count; i++)
            {
                if (first[i].LocalPosition != second[i].LocalPosition ||
                    !Mathf.Approximately(first[i].YawDegrees, second[i].YawDegrees) ||
                    !Mathf.Approximately(first[i].UniformScale, second[i].UniformScale))
                {
                    changed = true;
                    break;
                }
            }

            Assert.IsTrue(changed);
        }

        [Test]
        public void Build_PositionAndScale_StayInsidePatchAndConfiguredRange()
        {
            var patch = Patch(10, new Vector3(5f, 2f, -4f), new Vector2(2.5f, 1.5f));
            var settings = Settings(density: 1.5f, minScale: 0.7f, maxScale: 1.3f);
            var output = new List<GrassInstance>();

            GrassLayoutBuilder.Build(new[] { patch }, null, in settings, 9, output);

            Assert.Greater(output.Count, 0);
            for (var i = 0; i < output.Count; i++)
            {
                var instance = output[i];
                Assert.That(instance.LocalPosition.x, Is.InRange(2.5f, 7.5f));
                Assert.That(instance.LocalPosition.y, Is.EqualTo(2f));
                Assert.That(instance.LocalPosition.z, Is.InRange(-5.5f, -2.5f));
                Assert.That(instance.UniformScale, Is.InRange(0.7f, 1.3f));
                Assert.That(instance.YawDegrees, Is.InRange(0f, 360f));
            }
        }

        [Test]
        public void Build_NonSquareCount_SpansPatchBeyondLeadingRows()
        {
            var patch = Patch(10, Vector3.zero, new Vector2(2f, 2.5f));
            var settings = Settings(density: 0.25f);
            var output = new List<GrassInstance>();

            GrassLayoutBuilder.Build(new[] { patch }, null, in settings, 17, output);

            Assert.AreEqual(5, output.Count);

            var minZ = -2.5f;
            var cellSizeZ = 5f / 3f;
            var firstRowMax = minZ + cellSizeZ;
            var lastRowMin = minZ + (2f * cellSizeZ);
            var hasFirstRow = false;
            var hasLastRow = false;

            for (var i = 0; i < output.Count; i++)
            {
                var z = output[i].LocalPosition.z;
                if (z < firstRowMax)
                    hasFirstRow = true;
                if (z >= lastRowMin)
                    hasLastRow = true;
            }

            Assert.IsTrue(hasFirstRow);
            Assert.IsTrue(hasLastRow);
        }

        [Test]
        public void Build_CircleExclusion_RejectsCoveredPositions()
        {
            var patch = Patch(10, Vector3.zero, new Vector2(3f, 3f));
            var exclusions = new[] { GrassExclusion.Circle(Vector2.zero, 0.9f) };
            var settings = Settings(density: 1.2f);
            var output = new List<GrassInstance>();

            GrassLayoutBuilder.Build(new[] { patch }, exclusions, in settings, 77, output);

            Assert.Greater(output.Count, 0);
            for (var i = 0; i < output.Count; i++)
            {
                var xz = new Vector2(output[i].LocalPosition.x, output[i].LocalPosition.z);
                Assert.Greater((xz - Vector2.zero).sqrMagnitude, 0.9f * 0.9f);
            }
        }

        [Test]
        public void Build_BoxExclusion_RejectsCoveredPositions()
        {
            var patch = Patch(10, Vector3.zero, new Vector2(3f, 3f));
            var exclusions = new[] { GrassExclusion.Box(new Vector2(0.5f, -0.25f), new Vector2(0.8f, 0.6f)) };
            var settings = Settings(density: 1.2f);
            var output = new List<GrassInstance>();

            GrassLayoutBuilder.Build(new[] { patch }, exclusions, in settings, 77, output);

            Assert.Greater(output.Count, 0);
            for (var i = 0; i < output.Count; i++)
            {
                var delta = new Vector2(output[i].LocalPosition.x - 0.5f, output[i].LocalPosition.z + 0.25f);
                Assert.IsTrue(Mathf.Abs(delta.x) > 0.8f || Mathf.Abs(delta.y) > 0.6f);
            }
        }

        [Test]
        public void Build_InsetWestEdge_KeepsPositionsPastInset()
        {
            var patch = Patch(10, new Vector3(10f, 0f, 0f), new Vector2(3f, 2f), insetEdges: GrassPatchEdges.West);
            var settings = Settings(density: 1f, edgeInset: 0.75f);
            var output = new List<GrassInstance>();

            GrassLayoutBuilder.Build(new[] { patch }, null, in settings, 91, output);

            Assert.Greater(output.Count, 0);
            for (var i = 0; i < output.Count; i++)
                Assert.GreaterOrEqual(output[i].LocalPosition.x, 10f - 3f + 0.75f);
        }

        [Test]
        public void Build_CliffEdge_IncreasesCountDeterministically()
        {
            var basePatch = Patch(10, Vector3.zero, new Vector2(2f, 2f));
            var cliffPatch = Patch(11, Vector3.zero, new Vector2(2f, 2f), cliffEdges: GrassPatchEdges.North);
            var settings = Settings(density: 0.25f, cliffDensityMultiplier: 2f);
            var baseOutput = new List<GrassInstance>();
            var cliffOutput = new List<GrassInstance>();

            GrassLayoutBuilder.Build(new[] { basePatch }, null, in settings, 5, baseOutput);
            GrassLayoutBuilder.Build(new[] { cliffPatch }, null, in settings, 5, cliffOutput);

            Assert.AreEqual(4, baseOutput.Count);
            Assert.AreEqual(8, cliffOutput.Count);
        }

        [Test]
        public void Build_WeightedVariants_UsesOnlyPositiveWeightVariants()
        {
            var patch = Patch(10, Vector3.zero, new Vector2(4f, 4f));
            var settings = Settings(density: 1f, variantWeights: new[] { 0f, -5f, 2f, 0f, 3f });
            var output = new List<GrassInstance>();

            GrassLayoutBuilder.Build(new[] { patch }, null, in settings, 44, output);

            Assert.Greater(output.Count, 0);
            for (var i = 0; i < output.Count; i++)
                CollectionAssert.Contains(new[] { 2, 4 }, output[i].VariantIndex);
        }

        [Test]
        public void Build_EmptyPatches_ClearsOutput()
        {
            var settings = Settings(density: 1f);
            var output = new List<GrassInstance> { new GrassInstance(Vector3.one, 90f, 2f, 7) };

            GrassLayoutBuilder.Build(null, null, in settings, 1, output);

            Assert.Zero(output.Count);
        }

        [Test]
        public void Build_NullOutput_ThrowsArgumentNullException()
        {
            var settings = Settings(density: 1f);

            Assert.Throws<ArgumentNullException>(() =>
                GrassLayoutBuilder.Build(null, null, in settings, 1, null));
        }

        [Test]
        public void Build_InvalidSettings_SanitizesWithoutNaN()
        {
            var patch = Patch(10, new Vector3(1f, 3f, -2f), new Vector2(2f, 2f), cliffEdges: GrassPatchEdges.East);
            var settings = Settings(
                density: 0.5f,
                minScale: float.NaN,
                maxScale: float.PositiveInfinity,
                edgeInset: float.NaN,
                edgeThinChance: float.NaN,
                cliffDensityMultiplier: float.NegativeInfinity,
                variantWeights: new[] { float.NaN, float.NegativeInfinity, 2f });
            var output = new List<GrassInstance>();
            var sanitized = settings.Sanitized();

            Assert.GreaterOrEqual(sanitized.ClumpsPerSquareUnit, 0f);
            AssertFinite(sanitized.ClumpsPerSquareUnit);
            Assert.Greater(sanitized.MinScale, 0f);
            AssertFinite(sanitized.MinScale);
            Assert.GreaterOrEqual(sanitized.MaxScale, sanitized.MinScale);
            AssertFinite(sanitized.MaxScale);
            Assert.GreaterOrEqual(sanitized.EdgeInset, 0f);
            AssertFinite(sanitized.EdgeInset);
            Assert.That(sanitized.EdgeThinChance, Is.InRange(0f, 1f));
            AssertFinite(sanitized.EdgeThinChance);
            Assert.GreaterOrEqual(sanitized.CliffDensityMultiplier, 1f);
            AssertFinite(sanitized.CliffDensityMultiplier);
            Assert.AreEqual(3, sanitized.VariantWeights.Length);
            Assert.AreEqual(0f, sanitized.VariantWeights[0]);
            Assert.AreEqual(0f, sanitized.VariantWeights[1]);
            Assert.AreEqual(2f, sanitized.VariantWeights[2]);

            GrassLayoutBuilder.Build(new[] { patch }, null, in settings, 3, output);

            Assert.Greater(output.Count, 0);
            for (var i = 0; i < output.Count; i++)
            {
                var instance = output[i];
                AssertFinite(instance.LocalPosition.x);
                AssertFinite(instance.LocalPosition.y);
                AssertFinite(instance.LocalPosition.z);
                AssertFinite(instance.UniformScale);
                AssertFinite(instance.YawDegrees);
                Assert.AreEqual(2, instance.VariantIndex);
            }
        }

        [Test]
        public void Sanitized_DoesNotMutateOrAliasCallerOwnedVariantWeights()
        {
            var weights = new[] { 1f, float.NaN, -3f };
            var settings = Settings(variantWeights: weights);

            var sanitized = settings.Sanitized();

            Assert.AreNotSame(weights, sanitized.VariantWeights);
            Assert.AreEqual(3, weights.Length);
            Assert.AreEqual(1f, weights[0]);
            Assert.IsTrue(float.IsNaN(weights[1]));
            Assert.AreEqual(-3f, weights[2]);

            Assert.AreEqual(3, sanitized.VariantWeights.Length);
            Assert.AreEqual(1f, sanitized.VariantWeights[0]);
            Assert.AreEqual(0f, sanitized.VariantWeights[1]);
            Assert.AreEqual(-3f, sanitized.VariantWeights[2]);
        }

        [Test]
        public void Build_ReusedOutput_DoesNotAppendPreviousLayout()
        {
            var firstPatch = Patch(10, Vector3.zero, new Vector2(2f, 2f));
            var secondPatch = Patch(20, new Vector3(20f, 1f, 20f), new Vector2(1f, 1f));
            var settings = Settings(density: 0.5f);
            var output = new List<GrassInstance>();

            GrassLayoutBuilder.Build(new[] { firstPatch }, null, in settings, 8, output);
            Assert.Greater(output.Count, 0);

            GrassLayoutBuilder.Build(new[] { secondPatch }, null, in settings, 8, output);

            Assert.AreEqual(2, output.Count);
            for (var i = 0; i < output.Count; i++)
            {
                Assert.That(output[i].LocalPosition.x, Is.InRange(19f, 21f));
                Assert.That(output[i].LocalPosition.y, Is.EqualTo(1f));
                Assert.That(output[i].LocalPosition.z, Is.InRange(19f, 21f));
            }
        }
    }
}
