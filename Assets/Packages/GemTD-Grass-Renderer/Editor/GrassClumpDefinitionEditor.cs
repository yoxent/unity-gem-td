using System;
using UnityEditor;
using UnityEngine;
using GemTD.Grass;

namespace GemTD.Grass.Editor
{
    [CustomEditor(typeof(GrassClumpDefinition))]
    public sealed class GrassClumpDefinitionEditor : UnityEditor.Editor
    {
        const string BakeAllMenuPath = "Gem TD/Grass/Bake All Package Clump Meshes";
        const string PackageRoot = "Assets/Packages/GemTD-Grass-Renderer";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("profileRows"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("blades"), true);
            serializedObject.ApplyModifiedProperties();

            var definition = (GrassClumpDefinition)target;
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField(
                    "Output Mesh",
                    definition.OutputMesh,
                    typeof(Mesh),
                    false);

            var isValid = GrassClumpMeshGenerator.TryValidate(definition, out var error);
            if (!isValid)
                EditorGUILayout.HelpBox(error, MessageType.Error);
            else if (definition.OutputMesh == null)
                EditorGUILayout.HelpBox(
                    "No output Mesh is assigned. Bake Mesh will create a deterministic sibling Mesh asset.",
                    MessageType.Info);
            else
                EditorGUILayout.HelpBox(
                    $"Valid: {definition.Blades.Count} blades, {definition.ProfileRows.Count} profile rows, " +
                    $"{definition.Blades.Count * definition.ProfileRows.Count * 2} vertices, " +
                    $"{definition.Blades.Count * (definition.ProfileRows.Count - 1) * 6} indices.",
                    MessageType.Info);

            using (new EditorGUI.DisabledScope(!isValid))
            {
                if (GUILayout.Button("Bake Mesh"))
                    BakeWithOutputCreation(definition);
            }
        }

        [MenuItem(BakeAllMenuPath)]
        public static void BakeAllPackageClumpMeshes()
        {
            var guids = AssetDatabase.FindAssets("t:GrassClumpDefinition", new[] { PackageRoot });
            var bakedCount = 0;
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var definition = AssetDatabase.LoadAssetAtPath<GrassClumpDefinition>(path);
                if (definition == null)
                    continue;

                BakeWithOutputCreation(definition);
                bakedCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Baked {bakedCount} package grass clump mesh definition(s).");
        }

        static void BakeWithOutputCreation(GrassClumpDefinition definition)
        {
            if (!GrassClumpMeshGenerator.TryValidate(definition, out var error))
                throw new ArgumentException(error, nameof(definition));

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Bake Grass Clump Mesh");
            try
            {
                GrassClumpAssetAuthoring.EnsureOutputMesh(definition);
                GrassClumpMeshGenerator.BakeDefinition(definition);
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(definition.OutputMesh);
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }
    }
}
