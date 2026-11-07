using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using GemTD.Grass;

namespace GemTD.Grass.Editor
{
    public static class GrassDefaultAssetGenerator
    {
        const string MenuPath = "Gem TD/Grass/Create Default Package Assets";
        const string PackageRoot = "Assets/Packages/GemTD-Grass-Renderer";
        const string MeshFolder = PackageRoot + "/Meshes";
        const string GeometryFolder = PackageRoot + "/Geometry";
        const string MaterialFolder = PackageRoot + "/Materials";
        const string PresetFolder = PackageRoot + "/Presets";
        const string PrefabFolder = PackageRoot + "/Prefabs";

        const string MeshAPath = MeshFolder + "/GrassClump_A.asset";
        const string MeshBPath = MeshFolder + "/GrassClump_B.asset";
        const string DefinitionAPath = GeometryFolder + "/GrassClump_A_Definition.asset";
        const string DefinitionBPath = GeometryFolder + "/GrassClump_B_Definition.asset";
        const string MaterialPath = MaterialFolder + "/HandPaintedGrass.mat";
        const string StylePath = PresetFolder + "/HandPaintedGrass.asset";
        const string PrefabPath = PrefabFolder + "/GrassChunkRenderer.prefab";

        const string GrassShaderName = "GemTD/Hand Painted Grass";

        static readonly int GrassRootColorId = Shader.PropertyToID("_GrassRootColor");
        static readonly int GrassBodyColorId = Shader.PropertyToID("_GrassBodyColor");
        static readonly int GrassTipColorId = Shader.PropertyToID("_GrassTipColor");

        static readonly Color RootColor = new Color(0.28f, 0.39f, 0.21f, 1f);
        static readonly Color BodyColor = new Color(0.40f, 0.53f, 0.29f, 1f);
        static readonly Color TipColor = new Color(0.55f, 0.66f, 0.38f, 1f);
        static readonly Vector2 WindDirection = new Vector2(1f, 0.35f);

        [MenuItem(MenuPath)]
        public static void CreateDefaultPackageAssets()
        {
            EnsureFolderTree();

            var shader = Shader.Find(GrassShaderName);
            if (shader == null)
            {
                var message = $"Grass default asset generation requires shader '{GrassShaderName}'.";
                Debug.LogError(message);
                throw new InvalidOperationException(message);
            }

            var definitionA = CreateOrUpdateClumpAssets(
                DefinitionAPath,
                MeshAPath,
                GrassClumpMeshGenerator.CreateDefaultA());
            var definitionB = CreateOrUpdateClumpAssets(
                DefinitionBPath,
                MeshBPath,
                GrassClumpMeshGenerator.CreateDefaultB());
            var meshA = definitionA.OutputMesh;
            var meshB = definitionB.OutputMesh;
            var material = CreateOrUpdateAsset(MaterialPath, () => CreateMaterial(shader));
            var style = CreateOrUpdateAsset(StylePath, () => CreateStyle(meshA, meshB, material));

            CreateOrUpdatePrefab(PrefabPath);

            AssetDatabase.SaveAssets();
            ImportGeneratedAssets();

            Selection.activeObject = style;

            Debug.Log(
                "Created or updated grass package assets:\n" +
                $"- {DefinitionAPath}\n" +
                $"- {DefinitionBPath}\n" +
                $"- {MeshAPath}\n" +
                $"- {MeshBPath}\n" +
                $"- {MaterialPath}\n" +
                $"- {StylePath}\n" +
                $"- {PrefabPath}");
        }

        static void EnsureFolderTree()
        {
            EnsureFolder(PackageRoot, "Meshes");
            EnsureFolder(PackageRoot, "Geometry");
            EnsureFolder(PackageRoot, "Materials");
            EnsureFolder(PackageRoot, "Presets");
            EnsureFolder(PackageRoot, "Prefabs");
        }

        static void EnsureFolder(string parentFolder, string childFolderName)
        {
            var childPath = $"{parentFolder}/{childFolderName}";
            if (!AssetDatabase.IsValidFolder(childPath))
                AssetDatabase.CreateFolder(parentFolder, childFolderName);
        }

        static Material CreateMaterial(Shader shader)
        {
            var material = new Material(shader)
            {
                name = "HandPaintedGrass",
                enableInstancing = true
            };

            material.SetColor(GrassRootColorId, RootColor);
            material.SetColor(GrassBodyColorId, BodyColor);
            material.SetColor(GrassTipColorId, TipColor);
            SetFloatIfPresent(material, "_GrassColorNoiseScale", 3f);
            SetFloatIfPresent(material, "_GrassColorNoiseStrength", 0.07f);
            SetFloatIfPresent(material, "_GrassWindScale", 2.5f);
            SetFloatIfPresent(material, "_GrassWindSpeed", 0.22f);
            SetFloatIfPresent(material, "_GrassWindStrength", 0.05f);
            SetFloatIfPresent(material, "_GrassReceiveShadows", 1f);
            SetFloatIfPresent(material, "_Smoothness", 0f);
            SetFloatIfPresent(material, "_Glossiness", 0f);
            SetFloatIfPresent(material, "_Metallic", 0f);
            return material;
        }

        public static GrassClumpDefinition CreateOrUpdateClumpAssets(
            string definitionPath,
            string canonicalMeshPath,
            GrassClumpRecipe defaults)
        {
            if (string.IsNullOrWhiteSpace(definitionPath))
                throw new ArgumentException("Definition path is required.", nameof(definitionPath));
            if (string.IsNullOrWhiteSpace(canonicalMeshPath))
                throw new ArgumentException("Canonical mesh path is required.", nameof(canonicalMeshPath));
            if (defaults == null)
                throw new ArgumentNullException(nameof(defaults));

            var mesh = CreateOrLoadMesh(canonicalMeshPath, defaults.Name);
            var definition = CreateDefinitionIfAbsent(definitionPath, defaults, mesh);
            GrassClumpAssetAuthoring.AssignOutputMesh(
                definition,
                mesh,
                "Restore Canonical Grass Output Mesh");
            GrassClumpMeshGenerator.BakeDefinition(definition);
            return definition;
        }

        static Mesh CreateOrLoadMesh(string assetPath, string meshName)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (mesh != null)
                return mesh;

            mesh = new Mesh { name = meshName };
            AssetDatabase.CreateAsset(mesh, assetPath);
            Undo.RegisterCreatedObjectUndo(mesh, "Create Grass Clump Mesh");
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static GrassClumpDefinition CreateDefinitionIfAbsent(
            string assetPath,
            GrassClumpRecipe defaults,
            Mesh outputMesh)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GrassClumpDefinition>(assetPath);
            if (existing != null)
                return existing;

            var definition = ScriptableObject.CreateInstance<GrassClumpDefinition>();
            definition.name = defaults.Name + "_Definition";
            var serialized = new SerializedObject(definition);
            WriteProfileRows(serialized.FindProperty("profileRows"), defaults.ProfileRows);
            WriteBlades(serialized.FindProperty("blades"), defaults.Blades);
            serialized.FindProperty("outputMesh").objectReferenceValue = outputMesh;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(definition, assetPath);
            Undo.RegisterCreatedObjectUndo(definition, "Create Grass Clump Definition");
            EditorUtility.SetDirty(definition);
            return definition;
        }

        static void WriteProfileRows(
            SerializedProperty property,
            System.Collections.Generic.IReadOnlyList<GrassProfileRow> rows)
        {
            property.arraySize = rows.Count;
            for (var i = 0; i < rows.Count; i++)
            {
                var row = property.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("normalizedHeight").floatValue = rows[i].NormalizedHeight;
                row.FindPropertyRelative("widthFactor").floatValue = rows[i].WidthFactor;
            }
        }

        static void WriteBlades(
            SerializedProperty property,
            System.Collections.Generic.IReadOnlyList<GrassBladeRecipe> blades)
        {
            property.arraySize = blades.Count;
            for (var i = 0; i < blades.Count; i++)
            {
                var source = blades[i];
                var blade = property.GetArrayElementAtIndex(i);
                blade.FindPropertyRelative("basePosition").vector3Value = source.BasePosition;
                blade.FindPropertyRelative("yawDegrees").floatValue = source.YawDegrees;
                blade.FindPropertyRelative("height").floatValue = source.Height;
                blade.FindPropertyRelative("width").floatValue = source.Width;
                blade.FindPropertyRelative("forwardBend").floatValue = source.ForwardBend;
                blade.FindPropertyRelative("sideBend").floatValue = source.SideBend;
            }
        }

        static GrassStyleDefinition CreateStyle(Mesh meshA, Mesh meshB, Material material)
        {
            var style = ScriptableObject.CreateInstance<GrassStyleDefinition>();
            style.name = "HandPaintedGrass";

            var serializedObject = new SerializedObject(style);
            SetObjectArray(serializedObject.FindProperty("clumpMeshes"), meshA, meshB);
            SetFloatArray(serializedObject.FindProperty("variantWeights"), 1f, 1f);
            serializedObject.FindProperty("material").objectReferenceValue = material;
            serializedObject.FindProperty("clumpsPerSquareUnit").floatValue = 10f;
            serializedObject.FindProperty("scaleRange").vector2Value = new Vector2(0.9f, 1.15f);
            serializedObject.FindProperty("edgeInset").floatValue = 0.08f;
            serializedObject.FindProperty("edgeThinChance").floatValue = 0.35f;
            serializedObject.FindProperty("cliffDensityMultiplier").floatValue = 1.15f;
            serializedObject.FindProperty("towerClearanceRadius").floatValue = 0.38f;
            serializedObject.FindProperty("rootColor").colorValue = RootColor;
            serializedObject.FindProperty("bodyColor").colorValue = BodyColor;
            serializedObject.FindProperty("tipColor").colorValue = TipColor;
            serializedObject.FindProperty("colorNoiseScale").floatValue = 3f;
            serializedObject.FindProperty("colorNoiseStrength").floatValue = 0.07f;
            serializedObject.FindProperty("windDirection").vector2Value = WindDirection;
            serializedObject.FindProperty("windScale").floatValue = 2.5f;
            serializedObject.FindProperty("windSpeed").floatValue = 0.22f;
            serializedObject.FindProperty("windStrength").floatValue = 0.05f;
            serializedObject.FindProperty("shadowCasting").enumValueIndex = (int)ShadowCastingMode.Off;
            serializedObject.FindProperty("receiveShadows").boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return style;
        }

        static T CreateOrUpdateAsset<T>(string assetPath, Func<T> factory)
            where T : UnityEngine.Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing == null)
            {
                var created = factory();
                AssetDatabase.CreateAsset(created, assetPath);
                EditorUtility.SetDirty(created);
                return created;
            }

            var updated = factory();
            EditorUtility.CopySerialized(updated, existing);
            existing.name = updated.name;
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(updated);
            return existing;
        }

        static void CreateOrUpdatePrefab(string assetPath)
        {
            GameObject temporaryRoot = null;
            try
            {
                temporaryRoot = new GameObject("GrassChunkRenderer");
                temporaryRoot.AddComponent<GrassChunkRenderer>();
                PrefabUtility.SaveAsPrefabAsset(temporaryRoot, assetPath);
            }
            finally
            {
                if (temporaryRoot != null)
                    UnityEngine.Object.DestroyImmediate(temporaryRoot);
            }
        }

        static void ImportGeneratedAssets()
        {
            AssetDatabase.ImportAsset(DefinitionAPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(DefinitionBPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(MeshAPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(MeshBPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(MaterialPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(StylePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
        }

        static void SetObjectArray(SerializedProperty property, params UnityEngine.Object[] values)
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static void SetFloatArray(SerializedProperty property, params float[] values)
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).floatValue = values[i];
        }

        static void SetFloatIfPresent(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
                material.SetFloat(propertyName, value);
        }
    }
}
