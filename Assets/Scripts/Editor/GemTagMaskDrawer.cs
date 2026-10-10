using System;
using UnityEditor;
using UnityEngine;
using GemTD.Gameplay.Gems;

namespace GemTD.Editor
{
    [CustomPropertyDrawer(typeof(GemTagMaskAttribute))]
    public sealed class GemTagMaskDrawer : PropertyDrawer
    {
        const int Columns = 3;

        static readonly GemTag[] Flags = (GemTag[])Enum.GetValues(typeof(GemTag));

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var line = EditorGUIUtility.singleLineHeight;
            var gap = EditorGUIUtility.standardVerticalSpacing;
            if (!property.isExpanded)
                return line;

            var count = 0;
            for (var i = 0; i < Flags.Length; i++)
            {
                if (Flags[i] != GemTag.None)
                    count++;
            }

            var rows = (count + Columns - 1) / Columns;
            return line + gap + rows * (line + gap);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var line = EditorGUIUtility.singleLineHeight;
            var gap = EditorGUIUtility.standardVerticalSpacing;
            var tags = (GemTag)property.longValue;
            var summary = tags == GemTag.None ? "None" : GemTags.Format(tags);
            var header = new Rect(position.x, position.y, position.width, line);
            property.isExpanded = EditorGUI.Foldout(
                header,
                property.isExpanded,
                new GUIContent(label.text, summary),
                true);
            var summaryWidth = position.width - EditorGUIUtility.labelWidth;
            if (summaryWidth > 8f)
            {
                EditorGUI.LabelField(
                    new Rect(
                        position.x + EditorGUIUtility.labelWidth,
                        position.y,
                        summaryWidth,
                        line),
                    summary);
            }

            if (!property.isExpanded)
                return;

            EditorGUI.BeginChangeCheck();
            var y = position.y + line + gap;
            var colWidth = position.width / Columns;
            var col = 0;
            for (var i = 0; i < Flags.Length; i++)
            {
                var flag = Flags[i];
                if (flag == GemTag.None)
                    continue;

                var rect = new Rect(position.x + col * colWidth, y, colWidth, line);
                var on = (tags & flag) != 0;
                if (EditorGUI.ToggleLeft(rect, GemTags.Format(flag), on))
                    tags |= flag;
                else
                    tags &= ~flag;

                col++;
                if (col != Columns)
                    continue;
                col = 0;
                y += line + gap;
            }

            if (EditorGUI.EndChangeCheck())
                property.longValue = (long)tags;
        }
    }
}
