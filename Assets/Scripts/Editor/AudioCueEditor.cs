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
                var sfxProperty = serializedObject.FindProperty("sfx");
                EditorGUILayout.PropertyField(sfxProperty, true);
                DrawPlaybackSummary(sfxProperty);
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DrawPlaybackSummary(SerializedProperty sfxProperty)
        {
            var clip = sfxProperty.FindPropertyRelative("clip").objectReferenceValue as AudioClip;
            if (clip == null)
            {
                EditorGUILayout.HelpBox(
                    "No clip assigned. Playback is silent until a clip is set.",
                    MessageType.Warning);
                return;
            }

            var randomPitch = sfxProperty.FindPropertyRelative("randomPitch").boolValue;
            var fixedPitch = sfxProperty.FindPropertyRelative("pitch").floatValue;
            var pitchMin = sfxProperty.FindPropertyRelative("pitchMin").floatValue;
            var pitchMax = sfxProperty.FindPropertyRelative("pitchMax").floatValue;
            if (pitchMin <= 0f) pitchMin = 0.01f;
            if (pitchMax <= 0f) pitchMax = 0.01f;

            var fastest = randomPitch ? pitchMax : fixedPitch;
            var slowest = randomPitch ? pitchMin : fixedPitch;
            if (fastest <= 0f) fastest = 0.01f;
            if (slowest <= 0f) slowest = 0.01f;

            var shortest = clip.length / fastest;
            var longest = clip.length / slowest;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Clip length",
                clip.length.ToString("0.###") + " s" +
                (Mathf.Abs(longest - shortest) > 0.001f
                    ? "  (" + shortest.ToString("0.###") + "\u2013" + longest.ToString("0.###") + " s at pitch range)"
                    : string.Empty));

            var interval = sfxProperty.FindPropertyRelative("minIntervalSeconds").floatValue;
            if (interval <= 0f)
            {
                EditorGUILayout.LabelField("Throttle", "none \u2014 plays on every request");
                return;
            }

            var demanded = Mathf.FloorToInt(1f / interval);
            var sustainable = Mathf.FloorToInt(8f / longest);
            EditorGUILayout.LabelField(
                "Throttle",
                interval.ToString("0.###") + " s  \u2192  max " + demanded + " plays/s demanded");
            EditorGUILayout.LabelField(
                "Pool (8 sources)",
                "~" + sustainable + " concurrent  \u2192  " +
                (sustainable >= demanded
                    ? "no source stealing"
                    : "STEALING \u2014 shorten the clip or raise the interval"));
        }
    }
}
