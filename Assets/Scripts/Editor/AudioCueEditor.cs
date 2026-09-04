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
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("eventKey"),
                new GUIContent("Event Key"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bus"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("volume"));

            var bus = (AudioBus)serializedObject.FindProperty("bus").enumValueIndex;
            if (bus == AudioBus.Bgm)
            {
                EditorGUILayout.HelpBox(
                    "This cue defines one BGM track. Assign it to the AudioCueCatalog's active BGM field.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Create one AudioCue per SFX. Assign its clip here, then add the cue to AudioPlayer.prefab's " +
                    "Audio Cue Catalog. AudioPlayer's SFX children are reusable playback channels.",
                    MessageType.Info);
            }

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
