using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GemTD.Gameplay;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;

namespace GemTD.Editor
{
    /// <summary>Wires PR4 draft pool + 6-wave campaign onto Run.unity composition root.</summary>
    public static class Phase2Pr4WireScene
    {
        const string RunScenePath = "Assets/Scenes/Run.unity";

        [MenuItem("Gem TD/Phase 2 PR4 Wire Draft Pool + Waves")]
        public static void Wire()
        {
            var gems = new[]
            {
                AssetDatabase.LoadAssetAtPath<GemDefinition>("Assets/Data/Gems/Gem_LMP.asset"),
                AssetDatabase.LoadAssetAtPath<GemDefinition>("Assets/Data/Gems/Gem_Chain.asset"),
                AssetDatabase.LoadAssetAtPath<GemDefinition>("Assets/Data/Gems/Gem_FasterAttacks.asset"),
                AssetDatabase.LoadAssetAtPath<GemDefinition>("Assets/Data/Gems/Gem_IncreasedAccuracy.asset"),
                AssetDatabase.LoadAssetAtPath<GemDefinition>("Assets/Data/Gems/Gem_SlowerProjectiles.asset"),
                AssetDatabase.LoadAssetAtPath<GemDefinition>("Assets/Data/Gems/Gem_AttackEcho.asset"),
            };

            for (var i = 0; i < gems.Length; i++)
            {
                if (gems[i] == null)
                {
                    Debug.LogError($"[PR4 Wire] Missing gem asset at index {i}");
                    return;
                }
            }

            var waves = new WaveDefinition[6];
            for (var i = 0; i < 6; i++)
            {
                waves[i] = AssetDatabase.LoadAssetAtPath<WaveDefinition>($"Assets/Data/Waves/Wave_0{i + 1}.asset");
                if (waves[i] == null)
                {
                    Debug.LogError($"[PR4 Wire] Missing Wave_0{i + 1}.asset");
                    return;
                }
            }

            var boss = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/Data/Enemies/Enemy_Boss.asset");
            if (boss != null)
            {
                boss.MaxHealth = 400f;
                boss.LeakDamage = 5;
                EditorUtility.SetDirty(boss);
            }

            var cfg = AssetDatabase.LoadAssetAtPath<RunConfig>("Assets/Data/RunConfig_Default.asset");
            if (cfg != null)
            {
                cfg.InventoryCapacity = 10;
                cfg.SeedGems = System.Array.Empty<GemDefinition>();
                EditorUtility.SetDirty(cfg);
            }

            var scene = EditorSceneManager.OpenScene(RunScenePath);
            var root = Object.FindFirstObjectByType<GameCompositionRoot>();
            if (root == null)
            {
                Debug.LogError("[PR4 Wire] GameCompositionRoot not found in Run.unity");
                return;
            }

            var so = new SerializedObject(root);
            var draftProp = so.FindProperty("draftPool");
            var wavesProp = so.FindProperty("waves");
            if (draftProp == null || wavesProp == null)
            {
                Debug.LogError("[PR4 Wire] Serialized properties draftPool/waves not found.");
                return;
            }

            draftProp.arraySize = gems.Length;
            for (var i = 0; i < gems.Length; i++)
                draftProp.GetArrayElementAtIndex(i).objectReferenceValue = gems[i];

            wavesProp.arraySize = waves.Length;
            for (var i = 0; i < waves.Length; i++)
                wavesProp.GetArrayElementAtIndex(i).objectReferenceValue = waves[i];

            so.FindProperty("runConfig").objectReferenceValue = cfg;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError("[PR4 Wire] SaveScene failed.");
                return;
            }

            AssetDatabase.SaveAssets();

            // Verify
            so.Update();
            var nullDraft = 0;
            for (var i = 0; i < draftProp.arraySize; i++)
            {
                if (draftProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    nullDraft++;
            }

            Debug.Log($"[PR4 Wire] draftPool={draftProp.arraySize} (nulls={nullDraft}), waves={wavesProp.arraySize} saved to Run.unity");
            if (nullDraft > 0)
                Debug.LogError("[PR4 Wire] Some draftPool slots still null after save — check gem assets imported.");
        }
    }
}
