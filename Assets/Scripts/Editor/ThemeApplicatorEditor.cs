using UnityEditor;
using UnityEngine;
using GemTD.UI;

namespace GemTD.Editor
{
    [CustomEditor(typeof(ThemeApplicator))]
    [CanEditMultipleObjects]
    public sealed class ThemeApplicatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(6f);
            if (!GUILayout.Button("Apply"))
                return;

            for (var i = 0; i < targets.Length; i++)
            {
                var applicator = (ThemeApplicator)targets[i];
                if (applicator == null)
                    continue;

                RecordTexts(applicator);
                applicator.Apply();
            }
        }

        static void RecordTexts(ThemeApplicator applicator)
        {
            var serialized = new SerializedObject(applicator);
            var rows = serialized.FindProperty("targets");
            if (rows == null || !rows.isArray)
                return;

            for (var i = 0; i < rows.arraySize; i++)
            {
                var texts = rows.GetArrayElementAtIndex(i).FindPropertyRelative("texts");
                if (texts == null || !texts.isArray)
                    continue;

                for (var t = 0; t < texts.arraySize; t++)
                {
                    var text = texts.GetArrayElementAtIndex(t).objectReferenceValue;
                    if (text != null)
                        Undo.RecordObject(text, "Apply Theme");
                }
            }
        }
    }
}
