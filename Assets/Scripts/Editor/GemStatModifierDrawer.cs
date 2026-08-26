using UnityEditor;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Editor
{
    [CustomPropertyDrawer(typeof(GemStatModifier))]
    public sealed class GemStatModifierDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var kind = (RoleStatValueKind)property.FindPropertyRelative("ValueKind").enumValueIndex;
            var lines = kind == RoleStatValueKind.Range ? 5 : 4;
            if (IsChainCount(property))
                lines++;
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

            if (IsChainCount(property))
                RoleModifierDrawerUtil.Draw(property, "Falloff", ref y, position.x, position.width, line, gap);

            EditorGUI.EndProperty();
        }

        static bool IsChainCount(SerializedProperty property)
        {
            return (GemStat)property.FindPropertyRelative("Stat").enumValueIndex == GemStat.ChainCount;
        }
    }
}
