using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using GemTD.Grass;

namespace GemTD.Grass.Editor
{
    public static class GrassClumpAssetAuthoring
    {
        public static Mesh EnsureOutputMesh(GrassClumpDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (definition.OutputMesh != null)
                return definition.OutputMesh;

            var definitionPath = AssetDatabase.GetAssetPath(definition);
            if (string.IsNullOrEmpty(definitionPath))
                throw new InvalidOperationException(
                    "Save the grass clump definition as an asset before baking.");

            var directory = Path.GetDirectoryName(definitionPath)?.Replace('\\', '/');
            var baseName = definition.name.EndsWith("_Definition", StringComparison.Ordinal)
                ? definition.name.Substring(0, definition.name.Length - "_Definition".Length)
                : definition.name + "_Mesh";
            var meshPath = $"{directory}/{baseName}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(meshPath) != null)
                    throw new InvalidOperationException(
                        $"Cannot create grass output Mesh because '{meshPath}' is occupied by another asset type.");

                mesh = new Mesh { name = baseName };
                AssetDatabase.CreateAsset(mesh, meshPath);
                Undo.RegisterCreatedObjectUndo(mesh, "Create Grass Clump Output Mesh");
            }

            AssignOutputMesh(definition, mesh, "Assign Grass Clump Output Mesh");
            return mesh;
        }

        public static void AssignOutputMesh(
            GrassClumpDefinition definition,
            Mesh outputMesh,
            string undoName)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (definition.OutputMesh == outputMesh)
                return;

            Undo.RecordObject(definition, undoName);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("outputMesh").objectReferenceValue = outputMesh;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }
    }
}
