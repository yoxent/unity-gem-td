using UnityEditor;
using UnityEngine;
using GemTD.Core;

namespace GemTD.Editor
{
    [CustomEditor(typeof(AudioCue))]
    public sealed class AudioCueEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bus"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("volume"));

            var bus = (AudioBus)serializedObject.FindProperty("bus").enumValueIndex;
            if (bus == AudioBus.Bgm)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bgmClip"), new GUIContent("Clip"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("loop"));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sfx"), true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
