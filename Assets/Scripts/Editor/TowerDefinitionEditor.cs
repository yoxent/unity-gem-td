using GemTD.Gameplay.Towers;
using UnityEditor;
using UnityEngine;

namespace GemTD.Editor
{
    [CustomEditor(typeof(TowerDefinition))]
    public sealed class TowerDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "vfx");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("VFX", EditorStyles.boldLabel);
            var vfx = serializedObject.FindProperty("vfx");
            EditorGUILayout.PropertyField(vfx.FindPropertyRelative("castPrefab"));
            EditorGUILayout.PropertyField(vfx.FindPropertyRelative("flightPrefab"));
            EditorGUILayout.PropertyField(vfx.FindPropertyRelative("impactPrefab"));
            EditorGUILayout.PropertyField(vfx.FindPropertyRelative("chainPrefab"));
            EditorGUILayout.HelpBox(
                "Payload flight and impact prefabs are on the fire role's Effect Payloads.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
