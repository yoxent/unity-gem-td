using UnityEditor;
using UnityEngine;
using GemTD.Gameplay.Map;

namespace GemTD.Editor
{
    [CustomEditor(typeof(PathTileSet))]
    public sealed class PathTileSetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Apply writes this set into every chunk in the catalog. " +
                "Path cells get the matching piece. Tower pads get the cliff blocks " +
                "(height 1 stays visible; 2 and 3 turn on when the chunk is placed).",
                MessageType.Info);

            if (GUILayout.Button("Apply to catalog", GUILayout.Height(28)))
                PathTileBaker.ApplyToCatalog((PathTileSet)target);
        }
    }
}
