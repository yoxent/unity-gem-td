using System;
using NUnit.Framework;
using UnityEngine;
using GemTD.Grass.Editor;

namespace GemTD.GrassRenderer.Tests.EditMode
{
    public class GrassClumpMeshGeneratorTests
    {
        [Test]
        public void CreateClump_FourFiveSegmentBlades_HasExpectedGeometry()
        {
            var recipe = CreateRecipe(
                "Geometry",
                CreateBlade(new Vector3(-0.16f, 0f, -0.08f), 0f, 0.42f, 0.07f, 0.09f, -0.03f),
                CreateBlade(new Vector3(0.11f, 0f, -0.05f), 38f, 0.39f, 0.06f, 0.05f, 0.04f),
                CreateBlade(new Vector3(-0.04f, 0f, 0.12f), 106f, 0.45f, 0.08f, 0.11f, -0.02f),
                CreateBlade(new Vector3(0.15f, 0f, 0.09f), 224f, 0.41f, 0.065f, 0.07f, 0.05f));

            using var meshScope = new MeshScope(CreateClump(recipe));
            Assert.AreEqual(48, meshScope.Mesh.vertexCount);
            Assert.AreEqual(120, meshScope.Mesh.triangles.Length);
        }

        [Test]
        public void CreateClump_RootUvsAreZero_AndTipsAreOne()
        {
            var recipe = CreateRecipe(
                "Uvs",
                CreateBlade(Vector3.zero, 0f, 0.4f, 0.08f, 0.08f, 0.02f),
                CreateBlade(new Vector3(0.1f, 0f, 0.08f), 80f, 0.36f, 0.06f, 0.05f, -0.04f));

            using var meshScope = new MeshScope(CreateClump(recipe));
            var uvs = meshScope.Mesh.uv;

            Assert.AreEqual(24, uvs.Length);
            for (var bladeIndex = 0; bladeIndex < 2; bladeIndex++)
            {
                var bladeOffset = bladeIndex * 12;
                AssertUv(uvs[bladeOffset + 0], 0f, 0f);
                AssertUv(uvs[bladeOffset + 1], 1f, 0f);
                AssertUv(uvs[bladeOffset + 10], 0f, 1f);
                AssertUv(uvs[bladeOffset + 11], 1f, 1f);
            }
        }

        [Test]
        public void CreateClump_RepeatedRecipe_ProducesIdenticalVertices()
        {
            var recipe = CreateRecipe(
                "Deterministic",
                CreateBlade(new Vector3(-0.08f, 0f, 0.02f), 15f, 0.38f, 0.07f, 0.08f, 0.01f),
                CreateBlade(new Vector3(0.06f, 0f, -0.04f), 110f, 0.44f, 0.09f, 0.12f, -0.03f),
                CreateBlade(new Vector3(0.12f, 0f, 0.1f), 245f, 0.41f, 0.065f, 0.09f, 0.04f));

            using var firstMeshScope = new MeshScope(CreateClump(recipe));
            using var secondMeshScope = new MeshScope(CreateClump(recipe));
            var first = firstMeshScope.Mesh;
            var second = secondMeshScope.Mesh;

            Assert.AreEqual(first.vertexCount, second.vertexCount);
            Assert.AreEqual(first.uv.Length, second.uv.Length);
            Assert.AreEqual(first.triangles.Length, second.triangles.Length);

            for (var i = 0; i < first.vertexCount; i++)
                Assert.AreEqual(first.vertices[i], second.vertices[i], $"Vertex mismatch at {i}.");

            for (var i = 0; i < first.uv.Length; i++)
                Assert.AreEqual(first.uv[i], second.uv[i], $"UV mismatch at {i}.");

            for (var i = 0; i < first.triangles.Length; i++)
                Assert.AreEqual(first.triangles[i], second.triangles[i], $"Triangle mismatch at {i}.");
        }

        [Test]
        public void CreateClump_AllNormalsAreFiniteAndNonZero()
        {
            var recipe = CreateRecipe(
                "Normals",
                CreateBlade(new Vector3(-0.15f, 0f, -0.06f), 0f, 0.45f, 0.09f, 0.16f, -0.05f),
                CreateBlade(new Vector3(0.05f, 0f, 0.01f), 57f, 0.4f, 0.07f, 0.1f, 0.03f),
                CreateBlade(new Vector3(0.12f, 0f, 0.1f), 149f, 0.37f, 0.06f, 0.07f, 0.05f));

            using var meshScope = new MeshScope(CreateClump(recipe));
            var normals = meshScope.Mesh.normals;

            Assert.AreEqual(meshScope.Mesh.vertexCount, normals.Length);
            for (var i = 0; i < normals.Length; i++)
            {
                AssertFinite(normals[i].x, $"Normal.x at {i}");
                AssertFinite(normals[i].y, $"Normal.y at {i}");
                AssertFinite(normals[i].z, $"Normal.z at {i}");
                Assert.Greater(normals[i].sqrMagnitude, 0.0001f, $"Normal magnitude at {i}");
            }
        }

        [Test]
        public void CreateClump_VariedYawProducesNonCoplanarClump()
        {
            var recipe = GrassClumpMeshGenerator.CreateDefaultB();

            using var meshScope = new MeshScope(CreateClump(recipe));
            var vertices = meshScope.Mesh.vertices;
            Assert.Greater(vertices.Length, 3);

            var plane = new Plane(vertices[0], vertices[1], vertices[2]);
            var foundOffPlaneVertex = false;
            for (var i = 3; i < vertices.Length; i++)
            {
                if (Mathf.Abs(plane.GetDistanceToPoint(vertices[i])) > 0.001f)
                {
                    foundOffPlaneVertex = true;
                    break;
                }
            }

            Assert.IsTrue(foundOffPlaneVertex, "Expected a varied-yaw clump to include vertices off the initial plane.");
        }

        [Test]
        public void CreateClump_InvalidRecipe_ThrowsClearArgumentException()
        {
            var nullException = Assert.Throws<ArgumentNullException>(
                () => GrassClumpMeshGenerator.CreateClump((GrassClumpRecipe)null));
            Assert.That(nullException.ParamName, Is.EqualTo("recipe"));

            var emptyException = Assert.Throws<ArgumentException>(() => GrassClumpMeshGenerator.CreateClump(CreateRecipe("Empty")));
            StringAssert.Contains("blade", emptyException.Message);

            var invalidHeightException = Assert.Throws<ArgumentOutOfRangeException>(() =>
                GrassClumpMeshGenerator.CreateClump(CreateRecipe("BadHeight", CreateBlade(Vector3.zero, 0f, 0f, 0.05f, 0f, 0f))));
            StringAssert.Contains("Height", invalidHeightException.Message);

            var invalidWidthException = Assert.Throws<ArgumentOutOfRangeException>(() =>
                GrassClumpMeshGenerator.CreateClump(CreateRecipe("BadWidth", CreateBlade(Vector3.zero, 0f, 0.2f, -0.01f, 0f, 0f))));
            StringAssert.Contains("Width", invalidWidthException.Message);

            var invalidFiniteException = Assert.Throws<ArgumentException>(() =>
                GrassClumpMeshGenerator.CreateClump(CreateRecipe("BadFinite", CreateBlade(Vector3.zero, float.NaN, 0.2f, 0.05f, 0f, 0f))));
            StringAssert.Contains("finite", invalidFiniteException.Message);
        }

        [Test]
        public void DefaultRecipes_MatchApprovedBladeCountsAndHeightRange()
        {
            var defaultA = GrassClumpMeshGenerator.CreateDefaultA();
            var defaultB = GrassClumpMeshGenerator.CreateDefaultB();

            var bladesA = defaultA.Blades;
            var bladesB = defaultB.Blades;

            Assert.AreEqual(7, bladesA.Count);
            Assert.AreEqual(9, bladesB.Count);

            for (var i = 0; i < bladesA.Count; i++)
                Assert.That(bladesA[i].Height, Is.InRange(0.38f, 0.48f), $"Default A blade {i} height");

            for (var i = 0; i < bladesB.Count; i++)
                Assert.That(bladesB[i].Height, Is.InRange(0.28f, 0.40f), $"Default B blade {i} height");
        }

        [Test]
        public void DefaultA_GeneratedHorizontalFootprint_DoesNotExceedApprovedMaximum()
        {
            using var meshScope = new MeshScope(CreateClump(GrassClumpMeshGenerator.CreateDefaultA()));
            var size = meshScope.Mesh.bounds.size;

            Assert.That(size.x, Is.LessThanOrEqualTo(0.44f), "Default A X footprint");
            Assert.That(size.z, Is.LessThanOrEqualTo(0.44f), "Default A Z footprint");
        }

        static Mesh CreateClump(GrassClumpRecipe recipe)
        {
            var mesh = GrassClumpMeshGenerator.CreateClump(recipe);
            Assert.IsNotNull(mesh, "Expected CreateClump to return a Mesh.");
            return mesh;
        }

        static GrassClumpRecipe CreateRecipe(string name, params GrassBladeRecipe[] blades)
        {
            return new GrassClumpRecipe(name, blades);
        }

        static GrassBladeRecipe CreateBlade(Vector3 basePosition, float yawDegrees, float height, float width, float forwardBend, float sideBend)
        {
            return new GrassBladeRecipe(basePosition, yawDegrees, height, width, forwardBend, sideBend);
        }

        static void AssertUv(Vector2 actual, float expectedX, float expectedY)
        {
            Assert.That(actual.x, Is.EqualTo(expectedX).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expectedY).Within(0.0001f));
        }

        static void AssertFinite(float value, string label)
        {
            Assert.IsFalse(float.IsNaN(value), $"{label} should not be NaN.");
            Assert.IsFalse(float.IsInfinity(value), $"{label} should not be Infinity.");
        }

        readonly struct MeshScope : IDisposable
        {
            public Mesh Mesh { get; }

            public MeshScope(Mesh mesh)
            {
                Mesh = mesh;
            }

            public void Dispose()
            {
                if (Mesh != null)
                    UnityEngine.Object.DestroyImmediate(Mesh);
            }
        }
    }
}
