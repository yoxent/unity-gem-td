using UnityEditor;
using UnityEngine;
using GemTD.Core;

namespace GemTD.Editor
{
    [CustomPropertyDrawer(typeof(SfxData))]
    public sealed class SfxDataDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var random = property.FindPropertyRelative("randomPitch").boolValue;
            var lines = random ? 5 : 3;
            var line = EditorGUIUtility.singleLineHeight;
            var gap = EditorGUIUtility.standardVerticalSpacing;
            return lines * (line + gap);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var line = EditorGUIUtility.singleLineHeight;
            var gap = EditorGUIUtility.standardVerticalSpacing;
            var y = position.y;
            var x = position.x;
            var w = position.width;

            Draw(property, "clip", new GUIContent("Clip"), ref y, x, w, line, gap);
            Draw(property, "randomPitch", new GUIContent("Random Pitch"), ref y, x, w, line, gap);

            var random = property.FindPropertyRelative("randomPitch").boolValue;
            if (!random)
            {
                Draw(property, "pitch", new GUIContent("Pitch (fixed)"), ref y, x, w, line, gap);
            }
            else
            {
                var header = new Rect(x, y, w, line);
                EditorGUI.LabelField(header, "Pitch Range", EditorStyles.boldLabel);
                y += line + gap;
                Draw(property, "pitchMin", new GUIContent("Min"), ref y, x, w, line, gap);
                Draw(property, "pitchMax", new GUIContent("Max"), ref y, x, w, line, gap);
            }

            EditorGUI.EndProperty();
        }

        static void Draw(
            SerializedProperty property,
            string field,
            GUIContent label,
            ref float y,
            float x,
            float width,
            float line,
            float gap)
        {
            var relative = property.FindPropertyRelative(field);
            if (relative == null)
                return;
            EditorGUI.PropertyField(new Rect(x, y, width, line), relative, label);
            y += line + gap;
        }
    }
}
