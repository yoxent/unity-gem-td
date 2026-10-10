using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using GemTD.Grass;

namespace GemTD.GrassRenderer.Tests.EditMode
{
    public class GrassChunkRendererTests
    {
        const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        const BindingFlags InstanceAny = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Test]
        public void Bind_GroupsInstancesByVariant_AndBuildsMatrices()
        {
            var go = new GameObject("GrassRendererTest");
            var component = go.AddComponent<GrassChunkRenderer>();
            var meshA = CreateMesh("GrassA", 1f);
            var meshB = CreateMesh("GrassB", 1.5f);
            var material = CreateMaterial();
            var style = CreateStyle(new[] { meshA, meshB }, new[] { 1f, 2f }, material);

            try
            {
                go.transform.position = new Vector3(3f, 0.5f, -2f);
                go.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
                var instances = new[]
                {
                    new GrassInstance(new Vector3(1f, 0f, 2f), 15f, 0.8f, 0),
                    new GrassInstance(new Vector3(-2f, 0.25f, 0.5f), 120f, 1.2f, 1),
                    new GrassInstance(new Vector3(0f, 0f, -1f), 270f, 1f, 0)
                };

                component.Bind(style, instances, new Bounds(Vector3.zero, Vector3.one * 4f));

                Assert.AreEqual(3, component.InstanceCount);
                Assert.AreEqual(2, component.DrawGroupCount);
                Assert.AreEqual(2, component.DrawBatchCount);

                AssertMatrixApproximately(
                    GetGroupMatrix(component, 0, 0),
                    go.transform.localToWorldMatrix *
                    Matrix4x4.TRS(instances[0].LocalPosition, Quaternion.Euler(0f, instances[0].YawDegrees, 0f), Vector3.one * instances[0].UniformScale));
                AssertMatrixApproximately(
                    GetGroupMatrix(component, 0, 1),
                    go.transform.localToWorldMatrix *
                    Matrix4x4.TRS(instances[2].LocalPosition, Quaternion.Euler(0f, instances[2].YawDegrees, 0f), Vector3.one * instances[2].UniformScale));
                AssertMatrixApproximately(
                    GetGroupMatrix(component, 1, 0),
                    go.transform.localToWorldMatrix *
                    Matrix4x4.TRS(instances[1].LocalPosition, Quaternion.Euler(0f, instances[1].YawDegrees, 0f), Vector3.one * instances[1].UniformScale));
            }
            finally
            {
                DestroyTestObjects(go, style, material, meshA, meshB);
            }
        }

        [Test]
        public void Clear_ReleasesAllCachedInstanceState()
        {
            var go = new GameObject("GrassRendererTest");
            var component = go.AddComponent<GrassChunkRenderer>();
            var mesh = CreateMesh("Grass", 1f);
            var material = CreateMaterial();
            var style = CreateStyle(new[] { mesh }, new[] { 1f }, material);

            try
            {
                component.Bind(
                    style,
                    new[] { new GrassInstance(Vector3.zero, 0f, 1f, 0) },
                    new Bounds(Vector3.zero, Vector3.one));

                component.Clear();

                Assert.AreEqual(0, component.InstanceCount);
                Assert.AreEqual(0, component.DrawGroupCount);
                Assert.AreEqual(0, component.DrawBatchCount);
                Assert.IsNull(GetPrivateField(component, "_groups"));
            }
            finally
            {
                DestroyTestObjects(go, style, material, mesh);
            }
        }

        [Test]
        public void Bind_NullStyle_DisablesDrawingAndWarnsOnce()
        {
            var go = new GameObject("GrassRendererTest");
            var component = go.AddComponent<GrassChunkRenderer>();
            var instances = new[] { new GrassInstance(Vector3.zero, 0f, 1f, 0) };

            try
            {
                LogAssert.Expect(
                    LogType.Warning,
                    "GrassChunkRenderer requires a GrassStyleDefinition before binding.");
                component.Bind(null, instances, new Bounds(Vector3.zero, Vector3.one));
                component.Bind(null, instances, new Bounds(Vector3.zero, Vector3.one));

                Assert.AreEqual(0, component.InstanceCount);
                Assert.AreEqual(0, component.DrawGroupCount);
                Assert.AreEqual(0, component.DrawBatchCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Bind_InvalidVariantIndex_SkipsOnlyInvalidInstance()
        {
            var go = new GameObject("GrassRendererTest");
            var component = go.AddComponent<GrassChunkRenderer>();
            var meshA = CreateMesh("GrassA", 1f);
            var meshB = CreateMesh("GrassB", 1f);
            var material = CreateMaterial();
            var style = CreateStyle(new[] { meshA, meshB }, new[] { 1f, 1f }, material);

            try
            {
                var instances = new[]
                {
                    new GrassInstance(Vector3.zero, 0f, 1f, 0),
                    new GrassInstance(Vector3.one, 0f, 1f, -1),
                    new GrassInstance(Vector3.forward, 0f, 1f, 1),
                    new GrassInstance(Vector3.right, 0f, 1f, 4)
                };

                LogAssert.Expect(
                    LogType.Warning,
                    "GrassChunkRenderer skipped an instance with an invalid grass mesh variant.");
                component.Bind(style, instances, new Bounds(Vector3.zero, Vector3.one * 4f));

                Assert.AreEqual(2, component.InstanceCount);
                Assert.AreEqual(2, component.DrawGroupCount);
                Assert.AreEqual(2, component.DrawBatchCount);
            }
            finally
            {
                DestroyTestObjects(go, style, material, meshA, meshB);
            }
        }

        [Test]
        public void Bind_MoreThan1023Instances_CreatesSafeBatches()
        {
            var go = new GameObject("GrassRendererTest");
            var component = go.AddComponent<GrassChunkRenderer>();
            var mesh = CreateMesh("Grass", 1f);
            var material = CreateMaterial();
            var style = CreateStyle(new[] { mesh }, new[] { 1f }, material);

            try
            {
                var instances = new GrassInstance[1024];
                for (var i = 0; i < instances.Length; i++)
                    instances[i] = new GrassInstance(new Vector3(i, 0f, 0f), 0f, 1f, 0);

                component.Bind(style, instances, new Bounds(new Vector3(512f, 0f, 0f), new Vector3(1024f, 1f, 1f)));

                Assert.AreEqual(1024, component.InstanceCount);
                Assert.AreEqual(1, component.DrawGroupCount);
                Assert.AreEqual(2, component.DrawBatchCount);
                Assert.AreEqual(1024, GetGroupMatrixArray(component, 0).Length);
            }
            finally
            {
                DestroyTestObjects(go, style, material, mesh);
            }
        }

        [Test]
        public void Bind_YawedSquareMesh_UsesConservativeWorldBounds()
        {
            var go = new GameObject("GrassRendererTest");
            var component = go.AddComponent<GrassChunkRenderer>();
            var mesh = CreateSquareMesh("SquareGrass");
            var material = CreateMaterial();
            var style = CreateStyle(new[] { mesh }, new[] { 1f }, material);

            try
            {
                go.transform.position = new Vector3(2f, 0f, 3f);
                go.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
                var instance = new GrassInstance(Vector3.zero, 45f, 2f, 0);
                component.Bind(style, new[] { instance }, new Bounds(Vector3.zero, Vector3.zero));

                var worldBounds = GetGroupWorldBounds(component, 0);
                var localRadius = Mathf.Sqrt(2f) * instance.UniformScale;
                var localToWorld = go.transform.localToWorldMatrix;
                var expectedWorldExtentX =
                    localRadius * (Mathf.Abs(localToWorld.m00) + Mathf.Abs(localToWorld.m02)) +
                    style.WindStrength;
                var expectedWorldExtentZ =
                    localRadius * (Mathf.Abs(localToWorld.m20) + Mathf.Abs(localToWorld.m22)) +
                    style.WindStrength;

                Assert.That(worldBounds.extents.x, Is.GreaterThanOrEqualTo(expectedWorldExtentX - 0.0001f));
                Assert.That(worldBounds.extents.z, Is.GreaterThanOrEqualTo(expectedWorldExtentZ - 0.0001f));
            }
            finally
            {
                DestroyTestObjects(go, style, material, mesh);
            }
        }

        [Test]
        public void Bind_ReplacesPreviousGroupsAtomically()
        {
            var go = new GameObject("GrassRendererTest");
            var component = go.AddComponent<GrassChunkRenderer>();
            var meshA = CreateMesh("GrassA", 1f);
            var meshB = CreateMesh("GrassB", 1f);
            var material = CreateMaterial();
            var style = CreateStyle(new[] { meshA, meshB }, new[] { 1f, 1f }, material);

            try
            {
                component.Bind(
                    style,
                    new[]
                    {
                        new GrassInstance(Vector3.zero, 0f, 1f, 0),
                        new GrassInstance(Vector3.one, 0f, 1f, 0)
                    },
                    new Bounds(Vector3.zero, Vector3.one * 4f));

                var replacement = new GrassInstance(new Vector3(7f, 0f, -3f), 45f, 1.25f, 1);
                component.Bind(style, new[] { replacement }, new Bounds(Vector3.one, Vector3.one * 2f));

                Assert.AreEqual(1, component.InstanceCount);
                Assert.AreEqual(1, component.DrawGroupCount);
                Assert.AreEqual(1, component.DrawBatchCount);
                AssertMatrixApproximately(
                    GetGroupMatrix(component, 0, 0),
                    go.transform.localToWorldMatrix *
                    Matrix4x4.TRS(replacement.LocalPosition, Quaternion.Euler(0f, replacement.YawDegrees, 0f), Vector3.one * replacement.UniformScale));
            }
            finally
            {
                DestroyTestObjects(go, style, material, meshA, meshB);
            }
        }

        [Test]
        public void Style_CreateLayoutSettings_DoesNotAliasVariantWeights()
        {
            var mesh = CreateMesh("Grass", 1f);
            var material = CreateMaterial();
            var style = CreateStyle(new[] { mesh }, new[] { 1f, 2f }, material);

            try
            {
                var settings = style.CreateLayoutSettings();
                Assert.AreEqual(2, settings.VariantWeights.Length);

                settings.VariantWeights[0] = 99f;
                Assert.AreEqual(1f, style.GetVariantWeight(0));
            }
            finally
            {
                DestroyTestObjects(style, material, mesh);
            }
        }

        [Test]
        public void Style_NewInstance_UsesApprovedTuningDefaults()
        {
            var style = ScriptableObject.CreateInstance<GrassStyleDefinition>();

            try
            {
                Assert.That(style.ClumpsPerSquareUnit, Is.EqualTo(10f));
                Assert.That(style.ScaleRange, Is.EqualTo(new Vector2(0.9f, 1.15f)));
                Assert.That(style.EdgeInset, Is.EqualTo(0.08f));
                Assert.That(style.EdgeThinChance, Is.EqualTo(0.35f));
                Assert.That(style.CliffDensityMultiplier, Is.EqualTo(1.15f));
                Assert.That(style.TowerClearanceRadius, Is.EqualTo(0.38f));
                Assert.That(style.RootColor, Is.EqualTo(new Color(0.28f, 0.39f, 0.21f, 1f)));
                Assert.That(style.BodyColor, Is.EqualTo(new Color(0.40f, 0.53f, 0.29f, 1f)));
                Assert.That(style.TipColor, Is.EqualTo(new Color(0.55f, 0.66f, 0.38f, 1f)));
                Assert.That(style.ColorNoiseScale, Is.EqualTo(3f));
                Assert.That(style.ColorNoiseStrength, Is.EqualTo(0.07f));
                Assert.That(style.WindDirection, Is.EqualTo(new Vector2(1f, 0.35f)));
                Assert.That(style.WindScale, Is.EqualTo(2.5f));
                Assert.That(style.WindSpeed, Is.EqualTo(0.22f));
                Assert.That(style.WindStrength, Is.EqualTo(0.05f));
                Assert.That(style.ShadowCasting, Is.EqualTo(ShadowCastingMode.Off));
                Assert.IsTrue(style.ReceiveShadows);
            }
            finally
            {
                DestroyTestObjects(style);
            }
        }

        [Test]
        public void Style_NonFiniteValues_AreSanitized()
        {
            var mesh = CreateMesh("Grass", 1f);
            var material = CreateMaterial();
            var style = CreateStyle(new[] { mesh }, new[] { float.NaN, float.PositiveInfinity, 2f }, material);

            try
            {
                var serializedObject = new SerializedObject(style);
                serializedObject.FindProperty("clumpsPerSquareUnit").floatValue = float.NaN;
                serializedObject.FindProperty("scaleRange").vector2Value = new Vector2(float.PositiveInfinity, float.NaN);
                serializedObject.FindProperty("edgeInset").floatValue = float.NaN;
                serializedObject.FindProperty("edgeThinChance").floatValue = float.PositiveInfinity;
                serializedObject.FindProperty("cliffDensityMultiplier").floatValue = float.NegativeInfinity;
                serializedObject.FindProperty("towerClearanceRadius").floatValue = float.NaN;
                serializedObject.FindProperty("colorNoiseScale").floatValue = float.NaN;
                serializedObject.FindProperty("colorNoiseStrength").floatValue = float.PositiveInfinity;
                serializedObject.FindProperty("windDirection").vector2Value = new Vector2(float.NaN, float.PositiveInfinity);
                serializedObject.FindProperty("windScale").floatValue = float.NaN;
                serializedObject.FindProperty("windSpeed").floatValue = float.PositiveInfinity;
                serializedObject.FindProperty("windStrength").floatValue = float.NaN;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                InvokeOnValidate(style);

                var settings = style.CreateLayoutSettings();
                AssertFinite(settings.ClumpsPerSquareUnit);
                Assert.GreaterOrEqual(settings.ClumpsPerSquareUnit, 0f);
                AssertFinite(settings.MinScale);
                AssertFinite(settings.MaxScale);
                Assert.Greater(settings.MinScale, 0f);
                Assert.GreaterOrEqual(settings.MaxScale, settings.MinScale);
                AssertFinite(settings.EdgeInset);
                Assert.GreaterOrEqual(settings.EdgeInset, 0f);
                AssertFinite(settings.EdgeThinChance);
                Assert.That(settings.EdgeThinChance, Is.InRange(0f, 1f));
                AssertFinite(settings.CliffDensityMultiplier);
                Assert.GreaterOrEqual(settings.CliffDensityMultiplier, 1f);
                AssertFinite(style.TowerClearanceRadius);
                Assert.GreaterOrEqual(style.TowerClearanceRadius, 0f);
                AssertFinite(style.ColorNoiseScale);
                Assert.Greater(style.ColorNoiseScale, 0f);
                AssertFinite(style.ColorNoiseStrength);
                Assert.GreaterOrEqual(style.ColorNoiseStrength, 0f);
                AssertFinite(style.WindDirection.x);
                AssertFinite(style.WindDirection.y);
                AssertFinite(style.WindScale);
                Assert.Greater(style.WindScale, 0f);
                AssertFinite(style.WindSpeed);
                Assert.GreaterOrEqual(style.WindSpeed, 0f);
                AssertFinite(style.WindStrength);
                Assert.GreaterOrEqual(style.WindStrength, 0f);
                Assert.AreEqual(3, settings.VariantWeights.Length);
                Assert.AreEqual(0f, settings.VariantWeights[0]);
                Assert.AreEqual(0f, settings.VariantWeights[1]);
                Assert.AreEqual(2f, settings.VariantWeights[2]);
                Assert.AreEqual(0f, style.GetVariantWeight(0));
            }
            finally
            {
                DestroyTestObjects(style, material, mesh);
            }
        }

        static GrassStyleDefinition CreateStyle(Mesh[] meshes, float[] weights, Material material)
        {
            var style = ScriptableObject.CreateInstance<GrassStyleDefinition>();
            var serializedObject = new SerializedObject(style);
            SetObjectArray(serializedObject.FindProperty("clumpMeshes"), meshes);
            SetFloatArray(serializedObject.FindProperty("variantWeights"), weights);
            serializedObject.FindProperty("material").objectReferenceValue = material;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            InvokeOnValidate(style);
            return style;
        }

        static void SetObjectArray(SerializedProperty property, Mesh[] values)
        {
            property.arraySize = values == null ? 0 : values.Length;
            if (values == null)
                return;

            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static void SetFloatArray(SerializedProperty property, float[] values)
        {
            property.arraySize = values == null ? 0 : values.Length;
            if (values == null)
                return;

            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).floatValue = values[i];
        }

        static Material CreateMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Unlit/Color") ??
                         Shader.Find("Standard");
            Assert.IsNotNull(shader, "A test shader is required to create the temporary material.");
            return new Material(shader);
        }

        static Mesh CreateMesh(string name, float height)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = new[]
            {
                new Vector3(-0.2f, 0f, -0.2f),
                new Vector3(0.2f, 0f, -0.2f),
                new Vector3(0f, height, 0.2f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 1f)
            };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh CreateSquareMesh(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = new[]
            {
                new Vector3(-1f, 0f, -1f),
                new Vector3(1f, 0f, -1f),
                new Vector3(1f, 1f, 1f),
                new Vector3(-1f, 1f, 1f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        static Matrix4x4 GetGroupMatrix(GrassChunkRenderer component, int groupIndex, int matrixIndex)
        {
            return GetGroupMatrixArray(component, groupIndex)[matrixIndex];
        }

        static Bounds GetGroupWorldBounds(GrassChunkRenderer component, int groupIndex)
        {
            var groups = (Array)GetPrivateField(component, "_groups");
            Assert.IsNotNull(groups);
            var group = groups.GetValue(groupIndex);
            var renderParamsField = group.GetType().GetField("RenderParams", InstanceAny);
            Assert.IsNotNull(renderParamsField);
            return ((RenderParams)renderParamsField.GetValue(group)).worldBounds;
        }

        static Matrix4x4[] GetGroupMatrixArray(GrassChunkRenderer component, int groupIndex)
        {
            var groups = (Array)GetPrivateField(component, "_groups");
            Assert.IsNotNull(groups);
            var group = groups.GetValue(groupIndex);
            var matricesField = group.GetType().GetField("Matrices", InstanceAny);
            Assert.IsNotNull(matricesField);
            return (Matrix4x4[])matricesField.GetValue(group);
        }

        static object GetPrivateField(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, InstancePrivate);
            Assert.IsNotNull(field, $"Expected private field '{fieldName}'.");
            return field.GetValue(instance);
        }

        static void InvokeOnValidate(GrassStyleDefinition style)
        {
            var method = typeof(GrassStyleDefinition).GetMethod("OnValidate", InstancePrivate);
            Assert.IsNotNull(method);
            method.Invoke(style, null);
        }

        static void AssertMatrixApproximately(Matrix4x4 actual, Matrix4x4 expected)
        {
            for (var row = 0; row < 4; row++)
            {
                for (var column = 0; column < 4; column++)
                    Assert.That(actual[row, column], Is.EqualTo(expected[row, column]).Within(0.0001f));
            }
        }

        static void AssertFinite(float value)
        {
            Assert.IsFalse(float.IsNaN(value));
            Assert.IsFalse(float.IsInfinity(value));
        }

        static void DestroyTestObjects(params UnityEngine.Object[] objects)
        {
            for (var i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                    UnityEngine.Object.DestroyImmediate(objects[i]);
            }
        }
    }
}
