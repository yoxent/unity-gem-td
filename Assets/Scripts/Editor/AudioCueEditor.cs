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

            var pitchesFixed = !sfxProperty.FindPropertyRelative("randomPitch").boolValue;
            var fixedPitch = sfxProperty.FindPropertyRelative("pitch").floatValue;
            var pitchMax = sfxProperty.FindPropertyRelative("pitchMax").floatValue;
            var fastest = pitchesFixed ? fixedPitch : pitchMax;
            var slowest = pitchesFixed ? fixedPitch : sfxProperty.FindPropertyRelative("pitchMin").floatValue;
            if (slowest <= 0f) slowest = 0.01f;
            if (fastest <= 0f) fastest = 0.01f;

            var shortest = AudioMix.AudibleLengthSeconds(clip.length, fastest);
            var longest = AudioMix.AudibleLengthSeconds(clip.length, slowest);

            var cap = sfxProperty.FindPropertyRelative("maxPlaybackSeconds").floatValue;
            var capSeconds = AudioMix.ResolvePlaybackCapSeconds(clip.length, fastest, cap);
            var effectiveLongest = capSeconds > 0f ? capSeconds : longest;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Clip length",
                clip.length.ToString("0.###") + " s" +
                (Mathf.Abs(longest - shortest) > 0.001f
                    ? "  (" + shortest.ToString("0.###") + "\u2013" + longest.ToString("0.###") + " s at pitch range)"
                    : string.Empty));

            if (capSeconds > 0f)
            {
                EditorGUILayout.HelpBox(
                    "Capped at " + capSeconds.ToString("0.###") + " s while the clip itself is " +
                    clip.length.ToString("0.###") + " s. The silent remainder never occupies a source, " +
                    "so a longer candidate clip can be dropped in without trimming it first.",
                    MessageType.Info);
            }
            else if (clip.length > 1f && fastest >= 1f)
            {
                EditorGUILayout.HelpBox(
                    "This clip is " + clip.length.ToString("0.##") + " s. A source stays occupied that whole " +
                    "time. For a high-frequency cue set Max Playback Seconds (for example 0.2) instead of " +
                    "hand-trimming the file.",
                    MessageType.Warning);
            }

            var interval = sfxProperty.FindPropertyRelative("minIntervalSeconds").floatValue;
            if (interval > 0f)
            {
                var sustained = effectiveLongest > 0f ? Mathf.FloorToInt(8f / effectiveLongest) : 8;
                var allowed = Mathf.FloorToInt(1f / interval);
                EditorGUILayout.LabelField(
                    "Throttle",
                    interval.ToString("0.###") + " s  \u2192  max " + allowed + " plays/s demanded");
                EditorGUILayout.LabelField(
                    "Pool (8 sources)",
                    "~" + sustained + " concurrent  \u2192  " +
                    (sustained >= allowed ? "no source stealing" : "STEALING (raise cap or shorten clip)"));
            }
        }
    }
}
