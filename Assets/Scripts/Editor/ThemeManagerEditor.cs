using UnityEditor;
using UnityEngine;
using GemTD.UI;

namespace GemTD.Editor
{
    [CustomEditor(typeof(ThemeManager))]
    public sealed class ThemeManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var theme = (ThemeManager)target;
            var count = theme.DuplicateNoticeCount;
            if (count == 0)
                return;

            EditorGUILayout.Space(6f);
            for (var i = 0; i < count; i++)
            {
                var notice = theme.GetDuplicateNotice(i);
                EditorGUILayout.HelpBox(
                    $"Duplicate id '{notice.fromId}' was renamed to '{notice.toId}'. Rename it if you want a different id.",
                    MessageType.Error);
            }
        }
    }
}
