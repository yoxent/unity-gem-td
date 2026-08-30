using UnityEditor;
using UnityEngine;
using GemTD.Gameplay.Enemies;

namespace GemTD.Editor
{
    [CustomEditor(typeof(EnemyDefinition))]
    public sealed class EnemyDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var locomotion = serializedObject.FindProperty("Locomotion");
            var hop = locomotion != null
                && (LocomotionStyle)locomotion.enumValueIndex == LocomotionStyle.Hop;

            var iterator = serializedObject.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(iterator, true);
                    continue;
                }

                if (!hop && (iterator.name == "HopHeight" || iterator.name == "HopPeriod"))
                    continue;

                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
