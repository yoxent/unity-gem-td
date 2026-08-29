using UnityEditor;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Editor
{
    [CustomPropertyDrawer(typeof(GemStatModifier))]
    public sealed class GemStatModifierDrawer : PropertyDrawer
    {
        const float LabelWidth = 56f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var kind = (RoleStatValueKind)property.FindPropertyRelative("ValueKind").enumValueIndex;
            // Stat + Operation + ValueKind + (Min/Max | header+Value row) [+ Falloff row for Chain]
            var lines = 5;
            if (IsChainCount(property) && kind != RoleStatValueKind.Range)
                lines += 1;
            return lines * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var line = EditorGUIUtility.singleLineHeight;
            var gap = EditorGUIUtility.standardVerticalSpacing;
            var y = position.y;
            var x = position.x;
            var width = position.width;

            RoleModifierDrawerUtil.Draw(property, "Stat", ref y, x, width, line, gap);
            RoleModifierDrawerUtil.Draw(property, "Operation", ref y, x, width, line, gap);
            RoleModifierDrawerUtil.Draw(property, "ValueKind", ref y, x, width, line, gap);

            var kind = (RoleStatValueKind)property.FindPropertyRelative("ValueKind").enumValueIndex;
            if (kind == RoleStatValueKind.Range)
            {
                RoleModifierDrawerUtil.Draw(property, "Min", ref y, x, width, line, gap);
                RoleModifierDrawerUtil.Draw(property, "Max", ref y, x, width, line, gap);
            }
            else
            {
                DrawRarityHeader(ref y, x, width, line, gap);
                DrawRarityRow(
                    property,
                    "Value",
                    "Lesser",
                    "Normal",
                    "Greater",
                    ref y,
                    x,
                    width,
                    line,
                    gap);

                if (IsChainCount(property))
                {
                    DrawRarityRow(
                        property,
                        "Falloff",
                        "LesserFalloff",
                        "NormalFalloff",
                        "GreaterFalloff",
                        ref y,
                        x,
                        width,
                        line,
                        gap);
                }
            }

            EditorGUI.EndProperty();
        }

        static void DrawRarityHeader(ref float y, float x, float width, float line, float gap)
        {
            var header = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            var fields = new Rect(x + LabelWidth, y, width - LabelWidth, line);
            var col = fields.width / 3f;
            EditorGUI.LabelField(new Rect(fields.x, fields.y, col, line), "Lesser", header);
            EditorGUI.LabelField(new Rect(fields.x + col, fields.y, col, line), "Normal", header);
            EditorGUI.LabelField(new Rect(fields.x + col * 2f, fields.y, col, line), "Greater", header);
            y += line + gap;
        }

        static void DrawRarityRow(
            SerializedProperty property,
            string rowLabel,
            string lesserField,
            string normalField,
            string greaterField,
            ref float y,
            float x,
            float width,
            float line,
            float gap)
        {
            EditorGUI.LabelField(new Rect(x, y, LabelWidth, line), rowLabel);

            var fields = new Rect(x + LabelWidth, y, width - LabelWidth, line);
            var col = fields.width / 3f;
            const float pad = 2f;

            EditorGUI.PropertyField(
                new Rect(fields.x + pad, fields.y, col - pad * 2f, line),
                property.FindPropertyRelative(lesserField),
                GUIContent.none);
            EditorGUI.PropertyField(
                new Rect(fields.x + col + pad, fields.y, col - pad * 2f, line),
                property.FindPropertyRelative(normalField),
                GUIContent.none);
            EditorGUI.PropertyField(
                new Rect(fields.x + col * 2f + pad, fields.y, col - pad * 2f, line),
                property.FindPropertyRelative(greaterField),
                GUIContent.none);

            y += line + gap;
        }

        static bool IsChainCount(SerializedProperty property)
        {
            return (GemStat)property.FindPropertyRelative("Stat").enumValueIndex == GemStat.ChainCount;
        }
    }
}
