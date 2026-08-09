using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GemTD.Gameplay;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;

namespace GemTD.Editor
{
    /// <summary>Creates PR5 gem SOs, Ballista sockets=3, Hydra seed, 9-gem draft pool.</summary>
    public static class Phase2Pr5WireScene
    {
        const string RunScenePath = "Assets/Scenes/Run.unity";
        const string GemsFolder = "Assets/Data/Gems";

        [MenuItem("Gem TD/Phase 2 PR5 Wire Gems + Hydra Seed")]
        public static void Wire()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(GemsFolder))
                AssetDatabase.CreateFolder("Assets/Data", "Gems");

            var lmp = EnsureGem("Gem_LMP.asset", GemId.Lmp, "Multiple Projectiles",
                "−20% damage / +2 projectiles. Spread shotgun.", 2f);
            var chain = EnsureGem("Gem_Chain.asset", GemId.Chain, "Chain",
                "−30% damage / +2 chains. Hop damage ×0.6 after first hit.", 2f);
            var fork = EnsureGem("Gem_Fork.asset", GemId.Fork, "Fork",
                "−15% damage. On hit, split ×2 at ±45°.", 2f);
            var area = EnsureGem("Gem_IncreasedArea.asset", GemId.IncreasedArea, "Increased Area",
                "+35% AoE / −10% fire rate.", 1f);
            var ignite = EnsureGem("Gem_Ignite.asset", GemId.Ignite, "Ignite",
                "Apply Ignite (short fire DoT).", 1f);
            var chill = EnsureGem("Gem_Chill.asset", GemId.Chill, "Chill",
                "Apply Chill (short slow).", 1f);
            var shock = EnsureGem("Gem_Shock.asset", GemId.Shock, "Shock",
                "Apply Shock (target takes extra damage).", 1f);
            var pierce = EnsureGem("Gem_Pierce.asset", GemId.Pierce, "Pierce",
                "−15% damage. Projectile continues through targets.", 1f);
            var prolif = EnsureGem("Gem_ElementalProliferation.asset", GemId.ElementalProliferation,
                "Elemental Proliferation",
                "−25% direct damage. Spread Ignite + Chill + Shock nearby.", 1f);

            var pool = new[] { lmp, chain, fork, area, ignite, chill, shock, pierce, prolif };
            for (var i = 0; i < pool.Length; i++)
            {
                if (pool[i] == null)
                {
                    Debug.LogError($"[PR5 Wire] Missing pool gem at index {i}");
                    return;
                }
            }

            var ballista = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/Data/Towers/Tower_Ballista.asset");
            if (ballista == null)
            {
                Debug.LogError("[PR5 Wire] Missing Tower_Ballista.asset");
                return;
            }

            ballista.SocketCount = 3;
            EditorUtility.SetDirty(ballista);

            var cannon = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/Data/Towers/Tower_Cannon.asset");
            if (cannon != null)
            {
                cannon.SocketCount = 2;
                EditorUtility.SetDirty(cannon);
            }

            var beacon = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/Data/Towers/Tower_Beacon.asset");
            if (beacon != null)
            {
                beacon.SocketCount = 1;
                EditorUtility.SetDirty(beacon);
            }

            var cfg = AssetDatabase.LoadAssetAtPath<RunConfig>("Assets/Data/RunConfig_Default.asset");
            if (cfg == null)
            {
                Debug.LogError("[PR5 Wire] Missing RunConfig_Default.asset");
                return;
            }

            cfg.InventoryCapacity = 10;
            cfg.SeedHydraRecipeGems = true;
            cfg.SeedGems = new[] { lmp, chain, fork };
            EditorUtility.SetDirty(cfg);

            var scene = EditorSceneManager.OpenScene(RunScenePath);
            var root = Object.FindFirstObjectByType<GameCompositionRoot>();
            if (root == null)
            {
                Debug.LogError("[PR5 Wire] GameCompositionRoot not found in Run.unity");
                return;
            }

            var so = new SerializedObject(root);
            var draftProp = so.FindProperty("draftPool");
            if (draftProp == null)
            {
                Debug.LogError("[PR5 Wire] draftPool property missing.");
                return;
            }

            draftProp.arraySize = pool.Length;
            for (var i = 0; i < pool.Length; i++)
                draftProp.GetArrayElementAtIndex(i).objectReferenceValue = pool[i];

            so.FindProperty("runConfig").objectReferenceValue = cfg;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError("[PR5 Wire] SaveScene failed.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[PR5 Wire] draftPool={pool.Length}, Ballista sockets=3, SeedHydraRecipeGems=true.");
        }

        static GemDefinition EnsureGem(string fileName, GemId id, string displayName, string description, float weight)
        {
            var path = $"{GemsFolder}/{fileName}";
            var gem = AssetDatabase.LoadAssetAtPath<GemDefinition>(path);
            if (gem == null)
            {
                gem = ScriptableObject.CreateInstance<GemDefinition>();
                AssetDatabase.CreateAsset(gem, path);
            }

            gem.Id = id;
            gem.DisplayName = displayName;
            gem.Description = description;
            gem.DraftWeight = weight;
            EditorUtility.SetDirty(gem);
            return gem;
        }
    }
}
