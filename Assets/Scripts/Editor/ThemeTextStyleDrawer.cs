using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GemTD.UI;

namespace GemTD.Editor
{
    static class ThemeStructDrawer
    {
        public static void OnGUI(Rect position, SerializedProperty property, string header)
        {
            EditorGUI.BeginProperty(position, new GUIContent(header), property);
            var line = EditorGUIUtility.singleLineHeight;
            var gap = EditorGUIUtility.standardVerticalSpacing;
            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, position.y, position.width, line),
                property.isExpanded,
                header,
                true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                var y = position.y + line + gap;
                var end = property.GetEndProperty();
                var child = property.Copy();
                if (child.NextVisible(true))
                {
                    while (!SerializedProperty.EqualContents(child, end))
                    {
                        var height = EditorGUI.GetPropertyHeight(child, true);
                        EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), child, true);
                        y += height + gap;
                        if (!child.NextVisible(false))
                            break;
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public static float GetPropertyHeight(SerializedProperty property)
        {
            var line = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return line;

            var gap = EditorGUIUtility.standardVerticalSpacing;
            var height = line;
            var end = property.GetEndProperty();
            var child = property.Copy();
            if (!child.NextVisible(true))
                return height;

            while (!SerializedProperty.EqualContents(child, end))
            {
                height += gap + EditorGUI.GetPropertyHeight(child, true);
                if (!child.NextVisible(false))
                    break;
            }

            return height;
        }
    }

    [CustomPropertyDrawer(typeof(ThemeTextStyle))]
    public sealed class ThemeTextStyleDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var id = property.FindPropertyRelative("id");
            var header = id != null && !string.IsNullOrWhiteSpace(id.stringValue)
                ? id.stringValue
                : label.text;
            ThemeStructDrawer.OnGUI(position, property, header);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return ThemeStructDrawer.GetPropertyHeight(property);
        }
    }

    [CustomPropertyDrawer(typeof(ThemeTextTarget))]
    public sealed class ThemeTextTargetDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var styleId = property.FindPropertyRelative("styleId");
            var header = styleId != null && !string.IsNullOrWhiteSpace(styleId.stringValue)
                ? styleId.stringValue
                : label.text;
            ThemeStructDrawer.OnGUI(position, property, header);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return ThemeStructDrawer.GetPropertyHeight(property);
        }
    }

    [CustomPropertyDrawer(typeof(ThemeIdAttribute))]
    public sealed class ThemeIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            var next = EditorGUI.DelayedTextField(position, label, property.stringValue);
            if (EditorGUI.EndChangeCheck())
                property.stringValue = ThemeManager.NormalizeId(next);
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(ThemeStyleIdAttribute))]
    public sealed class ThemeStyleIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var ids = CollectIds(property);
            if (ids == null)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var current = ThemeManager.NormalizeId(property.stringValue);
            var options = new List<string>(ids.Count + 1);
            if (current.Length == 0 || IndexOf(ids, current) < 0)
                options.Add(current);
            options.AddRange(ids);

            var labels = new GUIContent[options.Count];
            for (var i = 0; i < options.Count; i++)
                labels[i] = new GUIContent(string.IsNullOrEmpty(options[i]) ? "None" : options[i]);

            var selected = IndexOf(options, current);
            if (selected < 0)
                selected = 0;

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var next = EditorGUI.Popup(position, label, selected, labels);
            if (EditorGUI.EndChangeCheck() && next >= 0 && next < options.Count)
                property.stringValue = options[next];
            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();
        }

        static List<string> CollectIds(SerializedProperty property)
        {
            var themeProp = property.serializedObject.FindProperty("theme");
            var theme = themeProp != null ? themeProp.objectReferenceValue as ThemeManager : null;
            if (theme == null)
                return null;

            var themeObject = new SerializedObject(theme);
            var styles = themeObject.FindProperty("styles");
            if (styles == null || !styles.isArray || styles.arraySize == 0)
                return null;

            var ids = new List<string>(styles.arraySize);
            for (var i = 0; i < styles.arraySize; i++)
            {
                var id = ThemeManager.NormalizeId(styles.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue);
                if (id.Length == 0 || IndexOf(ids, id) >= 0)
                    continue;
                ids.Add(id);
            }

            ids.Sort(StringComparer.Ordinal);
            return ids.Count == 0 ? null : ids;
        }

        static int IndexOf(List<string> ids, string value)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                if (string.Equals(ids[i], value, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }
    }
}
