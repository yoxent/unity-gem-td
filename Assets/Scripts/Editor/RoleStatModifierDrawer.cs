using UnityEditor;
using UnityEngine;
using GemTD.Gameplay.Towers;

namespace GemTD.Editor
{
    [CustomPropertyDrawer(typeof(RoleStatModifier))]
    public sealed class RoleStatModifierDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var kind = (RoleStatValueKind)property.FindPropertyRelative("ValueKind").enumValueIndex;
            var lines = kind == RoleStatValueKind.Range ? 5 : 4;
            return lines * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var line = EditorGUIUtility.singleLineHeight;
            var gap = EditorGUIUtility.standardVerticalSpacing;
            var y = position.y;

            RoleModifierDrawerUtil.Draw(property, "Stat", ref y, position.x, position.width, line, gap);
            RoleModifierDrawerUtil.Draw(property, "Operation", ref y, position.x, position.width, line, gap);
            RoleModifierDrawerUtil.Draw(property, "ValueKind", ref y, position.x, position.width, line, gap);

            var kind = (RoleStatValueKind)property.FindPropertyRelative("ValueKind").enumValueIndex;
            if (kind == RoleStatValueKind.Range)
            {
                RoleModifierDrawerUtil.Draw(property, "Min", ref y, position.x, position.width, line, gap);
                RoleModifierDrawerUtil.Draw(property, "Max", ref y, position.x, position.width, line, gap);
            }
            else
            {
                RoleModifierDrawerUtil.Draw(property, "Value", ref y, position.x, position.width, line, gap);
            }

            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(RoleEffectModifier))]
    public sealed class RoleEffectModifierDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var kind = (RoleStatValueKind)property.FindPropertyRelative("ValueKind").enumValueIndex;
            var lines = kind == RoleStatValueKind.Range ? 5 : 4;
            return lines * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var line = EditorGUIUtility.singleLineHeight;
            var gap = EditorGUIUtility.standardVerticalSpacing;
            var y = position.y;

            RoleModifierDrawerUtil.Draw(property, "Kind", ref y, position.x, position.width, line, gap);
            RoleModifierDrawerUtil.Draw(property, "Operation", ref y, position.x, position.width, line, gap);
            RoleModifierDrawerUtil.Draw(property, "ValueKind", ref y, position.x, position.width, line, gap);

            var kind = (RoleStatValueKind)property.FindPropertyRelative("ValueKind").enumValueIndex;
            if (kind == RoleStatValueKind.Range)
            {
                RoleModifierDrawerUtil.Draw(property, "Min", ref y, position.x, position.width, line, gap);
                RoleModifierDrawerUtil.Draw(property, "Max", ref y, position.x, position.width, line, gap);
            }
            else
            {
                RoleModifierDrawerUtil.Draw(property, "Value", ref y, position.x, position.width, line, gap);
            }

            EditorGUI.EndProperty();
        }
    }

    static class RoleModifierDrawerUtil
    {
        public static void Draw(
            SerializedProperty property,
            string field,
            ref float y,
            float x,
            float width,
            float line,
            float gap)
        {
            var rect = new Rect(x, y, width, line);
            EditorGUI.PropertyField(rect, property.FindPropertyRelative(field));
            y += line + gap;
        }
    }
}
