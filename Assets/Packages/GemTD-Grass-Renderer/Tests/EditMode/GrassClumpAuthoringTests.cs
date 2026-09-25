using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using GemTD.Grass;
using GemTD.Grass.Editor;

namespace GemTD.GrassRenderer.Tests.EditMode
{
    public class GrassClumpAuthoringTests
    {
        const string TemporaryFolder = "Assets/Packages/GemTD-Grass-Renderer/Tests/TempAuthoring";

        [TearDown]
        public void TearDown()
        {
            Undo.ClearAll();
            AssetDatabase.DeleteAsset(TemporaryFolder);
        }

        [Test]
        public void CreateClump_ArbitraryProfileRowCount_ChangesGeometryCounts()
        {
            var recipe = new GrassClumpRecipe(
                "ThreeRows",
                new[]
                {
                    new GrassProfileRow(0f, 1f),
                    new GrassProfileRow(0.5f, 0.6f),
                    new GrassProfileRow(1f, 0f)
                },
                new[]
                {
                    CreateBladeRecipe(Vector3.zero),
                    CreateBladeRecipe(new Vector3(0.1f, 0f, 0.1f))
                });

            using var meshScope = new MeshScope(GrassClumpMeshGenerator.CreateClump(recipe));

            Assert.AreEqual(12, meshScope.Mesh.vertexCount);
            Assert.AreEqual(24, meshScope.Mesh.triangles.Length);
        }

        [TestCaseSource(nameof(InvalidProfiles))]
        public void CreateClump_InvalidProfile_ThrowsClearArgumentException(
            GrassProfileRow[] profile,
            string expectedMessage)
        {
            var recipe = new GrassClumpRecipe("Invalid", profile, new[] { CreateBladeRecipe(Vector3.zero) });

            var exception = Assert.Catch<ArgumentException>(() => GrassClumpMeshGenerator.CreateClump(recipe));

            StringAssert.Contains(expectedMessage, exception.Message);
        }

        [Test]
        public void CreateClump_DefinitionBladeListLength_DrivesBladeCount()
        {
            var definition = CreateDefinition(
                "BladeCount",
                GrassClumpMeshGenerator.ApprovedProfileRows,
                new[]
                {
                    CreateBlade(Vector3.zero),
                    CreateBlade(new Vector3(0.1f, 0f, 0f)),
                    CreateBlade(new Vector3(0f, 0f, 0.1f))
                },
                null);

            try
            {
                using var meshScope = new MeshScope(GrassClumpMeshGenerator.CreateClump(definition));
                Assert.AreEqual(36, meshScope.Mesh.vertexCount);
                Assert.AreEqual(90, meshScope.Mesh.triangles.Length);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void BakeDefinition_UpdatesExistingMeshAssetInPlaceAndPreservesGuid()
        {
            EnsureTemporaryFolder();
            var meshPath = TemporaryFolder + "/BakeTarget.asset";
            var definitionPath = TemporaryFolder + "/BakeDefinition.asset";

            var mesh = new Mesh { name = "BakeTarget" };
            AssetDatabase.CreateAsset(mesh, meshPath);
            var definition = CreateDefinition(
                "BakeDefinition",
                GrassClumpMeshGenerator.ApprovedProfileRows,
                new[] { CreateBlade(Vector3.zero), CreateBlade(new Vector3(0.1f, 0f, 0.1f)) },
                mesh);
            AssetDatabase.CreateAsset(definition, definitionPath);
            var guidBefore = AssetDatabase.AssetPathToGUID(meshPath);

            GrassClumpMeshGenerator.BakeDefinition(definition);

            var reloaded = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            Assert.AreEqual(meshPath, AssetDatabase.GetAssetPath(definition.OutputMesh));
            Assert.AreEqual(guidBefore, AssetDatabase.AssetPathToGUID(meshPath));
            Assert.AreEqual(24, reloaded.vertexCount);
            Assert.AreEqual(60, reloaded.triangles.Length);
        }

        [Test]
        public void BakeDefinition_RegistersMeshMutationForUndo()
        {
            EnsureTemporaryFolder();
            var mesh = new Mesh { name = "UndoTarget" };
            mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            mesh.triangles = new[] { 0, 1, 2 };
            AssetDatabase.CreateAsset(mesh, TemporaryFolder + "/UndoTarget.asset");
            var definition = CreateDefinition(
                "UndoDefinition",
                GrassClumpMeshGenerator.ApprovedProfileRows,
                new[] { CreateBlade(Vector3.zero) },
                mesh);
            AssetDatabase.CreateAsset(definition, TemporaryFolder + "/UndoDefinition.asset");
            Undo.ClearAll();

            GrassClumpMeshGenerator.BakeDefinition(definition);
            Assert.AreEqual(12, mesh.vertexCount);

            Undo.PerformUndo();
            Assert.AreEqual(3, mesh.vertexCount);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, mesh.triangles);
        }

        [Test]
        public void AssignOutputMesh_RegistersDefinitionAssignmentForUndo()
        {
            EnsureTemporaryFolder();
            var mesh = new Mesh { name = "AssignmentTarget" };
            AssetDatabase.CreateAsset(mesh, TemporaryFolder + "/AssignmentTarget.asset");
            var definition = CreateDefinition(
                "AssignmentDefinition",
                GrassClumpMeshGenerator.ApprovedProfileRows,
                new[] { CreateBlade(Vector3.zero) },
                null);
            AssetDatabase.CreateAsset(definition, TemporaryFolder + "/AssignmentDefinition.asset");
            Undo.ClearAll();

            GrassClumpAssetAuthoring.AssignOutputMesh(definition, mesh, "Assign Test Output");
            Assert.AreSame(mesh, definition.OutputMesh);

            Undo.PerformUndo();
            Assert.IsNull(definition.OutputMesh);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CreateOrUpdateClumpAssets_EnforcesCanonicalOutputAndPreservesArtistGeometry(
            bool startWithNullOutput)
        {
            EnsureTemporaryFolder();
            var meshPath = TemporaryFolder + "/Canonical.asset";
            var definitionPath = TemporaryFolder + "/Definition.asset";
            var arbitraryMesh = new Mesh { name = "Arbitrary" };
            arbitraryMesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            arbitraryMesh.triangles = new[] { 0, 1, 2 };
            AssetDatabase.CreateAsset(arbitraryMesh, TemporaryFolder + "/Arbitrary.asset");
            var artistBlade = new GrassBladeDefinition(Vector3.zero, 77.5f, 0.4f, 0.08f, 0.1f, 0.02f);
            var definition = CreateDefinition(
                "Definition",
                GrassClumpMeshGenerator.ApprovedProfileRows,
                new[] { artistBlade },
                startWithNullOutput ? null : arbitraryMesh);
            AssetDatabase.CreateAsset(definition, definitionPath);

            var result = GrassDefaultAssetGenerator.CreateOrUpdateClumpAssets(
                definitionPath,
                meshPath,
                GrassClumpMeshGenerator.CreateDefaultA());

            var canonicalMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            Assert.AreSame(definition, result);
            Assert.AreSame(canonicalMesh, definition.OutputMesh);
            Assert.AreEqual(77.5f, definition.Blades[0].YawDegrees);
            Assert.AreEqual(1, definition.Blades.Count);
            Assert.AreEqual(12, canonicalMesh.vertexCount);
            Assert.AreEqual(30, canonicalMesh.triangles.Length);
            Assert.AreEqual(3, arbitraryMesh.vertexCount, "Arbitrary reassigned output must not be baked.");
        }

        [Test]
        public void DefaultRecipeFactories_ContainApprovedProfileAndBladeRecipes()
        {
            var defaultA = GrassClumpMeshGenerator.CreateDefaultA();
            var defaultB = GrassClumpMeshGenerator.CreateDefaultB();

            AssertApprovedProfile(defaultA);
            AssertApprovedProfile(defaultB);
            Assert.AreEqual(7, defaultA.Blades.Count);
            Assert.AreEqual(9, defaultB.Blades.Count);
            CollectionAssert.AreEqual(
                new[] { 338f, 24f, 92f, 176f, 262f, 54f, 218f },
                GetBladeYaws(defaultA));
            CollectionAssert.AreEqual(
                new[] { 350f, 28f, 74f, 128f, 196f, 252f, 306f, 164f, 320f },
                GetBladeYaws(defaultB));
        }

        static object[] InvalidProfiles =
        {
            new object[] { new[] { new GrassProfileRow(0f, 1f) }, "at least two" },
            new object[] { new[] { new GrassProfileRow(0.1f, 1f), new GrassProfileRow(1f, 0f) }, "first height" },
            new object[] { new[] { new GrassProfileRow(0f, 1f), new GrassProfileRow(0.5f, 0.5f) }, "last height" },
            new object[] { new[] { new GrassProfileRow(0f, 1f), new GrassProfileRow(0f, 0.5f), new GrassProfileRow(1f, 0f) }, "strictly increasing" },
            new object[] { new[] { new GrassProfileRow(0f, -1f), new GrassProfileRow(1f, 0f) }, "nonnegative" },
            new object[] { new[] { new GrassProfileRow(0f, 1f), new GrassProfileRow(1f, float.NaN) }, "finite" }
        };

        static GrassClumpDefinition CreateDefinition(
            string name,
            GrassProfileRow[] profileRows,
            GrassBladeDefinition[] blades,
            Mesh outputMesh)
        {
            var definition = ScriptableObject.CreateInstance<GrassClumpDefinition>();
            definition.name = name;
            var serialized = new SerializedObject(definition);
            WriteProfile(serialized.FindProperty("profileRows"), profileRows);
            WriteBlades(serialized.FindProperty("blades"), blades);
            serialized.FindProperty("outputMesh").objectReferenceValue = outputMesh;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        static void WriteProfile(SerializedProperty property, GrassProfileRow[] rows)
        {
            property.arraySize = rows.Length;
            for (var i = 0; i < rows.Length; i++)
            {
                var row = property.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("normalizedHeight").floatValue = rows[i].NormalizedHeight;
                row.FindPropertyRelative("widthFactor").floatValue = rows[i].WidthFactor;
            }
        }

        static void WriteBlades(SerializedProperty property, GrassBladeDefinition[] blades)
        {
            property.arraySize = blades.Length;
            for (var i = 0; i < blades.Length; i++)
            {
                var blade = property.GetArrayElementAtIndex(i);
                blade.FindPropertyRelative("basePosition").vector3Value = blades[i].BasePosition;
                blade.FindPropertyRelative("yawDegrees").floatValue = blades[i].YawDegrees;
                blade.FindPropertyRelative("height").floatValue = blades[i].Height;
                blade.FindPropertyRelative("width").floatValue = blades[i].Width;
                blade.FindPropertyRelative("forwardBend").floatValue = blades[i].ForwardBend;
                blade.FindPropertyRelative("sideBend").floatValue = blades[i].SideBend;
            }
        }

        static GrassBladeDefinition CreateBlade(Vector3 basePosition)
        {
            return new GrassBladeDefinition(basePosition, 30f, 0.4f, 0.08f, 0.1f, 0.02f);
        }

        static GrassBladeRecipe CreateBladeRecipe(Vector3 basePosition)
        {
            return new GrassBladeRecipe(basePosition, 30f, 0.4f, 0.08f, 0.1f, 0.02f);
        }

        static void AssertApprovedProfile(GrassClumpRecipe recipe)
        {
            var approved = GrassClumpMeshGenerator.ApprovedProfileRows;
            Assert.AreEqual(approved.Length, recipe.ProfileRows.Count);
            for (var i = 0; i < approved.Length; i++)
            {
                Assert.AreEqual(approved[i].NormalizedHeight, recipe.ProfileRows[i].NormalizedHeight);
                Assert.AreEqual(approved[i].WidthFactor, recipe.ProfileRows[i].WidthFactor);
            }
        }

        static float[] GetBladeYaws(GrassClumpRecipe recipe)
        {
            var yaws = new float[recipe.Blades.Count];
            for (var i = 0; i < recipe.Blades.Count; i++)
                yaws[i] = recipe.Blades[i].YawDegrees;
            return yaws;
        }

        static void EnsureTemporaryFolder()
        {
            if (!AssetDatabase.IsValidFolder(TemporaryFolder))
                AssetDatabase.CreateFolder("Assets/Packages/GemTD-Grass-Renderer/Tests", "TempAuthoring");
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
