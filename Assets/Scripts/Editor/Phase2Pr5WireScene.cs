using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GemTD.Gameplay;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Meta;

namespace GemTD.Editor
{
    /// <summary>Creates the MVP gem SOs, Hydra Codex entry, and 11-gem draft pool.</summary>
    public static class Phase2Pr5WireScene
    {
        const string RunScenePath = "Assets/Scenes/Run.unity";
        const string GemsFolder = "Assets/Data/Gems";
        const string CodexFolder = "Assets/Data/Codex";

        [MenuItem("Gem TD/Phase 2 PR5 Wire Gems + Hydra Seed")]
        public static void Wire()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(GemsFolder))
                AssetDatabase.CreateFolder("Assets/Data", "Gems");

            var multipleProjectiles = EnsureGem("Gem_MultipleProjectiles.asset", GemId.MultipleProjectiles, "Multiple Projectiles",
                "−20% damage / +2 projectiles. Spread shotgun.", 2f);
            var chain = EnsureGem("Gem_Chain.asset", GemId.Chain, "Chain",
                "−30% damage / +1 chain (3 units). Hop damage ×0.6 after first hit.", 2f);
            var fork = EnsureGem("Gem_Fork.asset", GemId.Fork, "Fork",
                "−15% damage. On hit, split ×2 at ±45°.", 2f);
            var area = EnsureGem("Gem_IncreasedArea.asset", GemId.IncreasedArea, "Increased Area",
                "+35% AoE / −10% fire rate.", 1f);
            var pierce = EnsureGem("Gem_Pierce.asset", GemId.Pierce, "Pierce",
                "−15% damage. Continues through one extra target.", 1f);
            var prolif = EnsureGem("Gem_ElementalProliferation.asset", GemId.ElementalProliferation,
                "Elemental Proliferation",
                "−25% direct damage. Spread Ignite + Chill + Shock nearby.", 1f);
            var combustion = EnsureGem("Gem_Combustion.asset", GemId.Combustion, "Combustion",
                "+14% more damage. Apply Ignite.", 1f);
            var addedFire = EnsureGem("Gem_AddedFireDamage.asset", GemId.AddedFireDamage, "Added Fire Damage",
                "+31% of hit as extra fire damage.", 1f);
            var addedCold = EnsureGem("Gem_AddedColdDamage.asset", GemId.AddedColdDamage, "Added Cold Damage",
                "+4 added damage. Apply Chill.", 1f);
            var addedLightning = EnsureGem("Gem_AddedLightningDamage.asset", GemId.AddedLightningDamage,
                "Added Lightning Damage", "+4 added damage. Apply Shock.", 1f);
            var knockback = EnsureGem("Gem_Knockback.asset", GemId.Knockback, "Knockback",
                "34% chance to knock back 1 unit along the path.", 1f);

            var pool = new[]
            {
                multipleProjectiles, chain, fork, area, pierce, prolif,
                combustion, addedFire, addedCold, addedLightning, knockback
            };
            for (var i = 0; i < pool.Length; i++)
            {
                if (pool[i] == null)
                {
                    Debug.LogError($"[PR5 Wire] Missing pool gem at index {i}");
                    return;
                }
            }

            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(CodexFolder))
                AssetDatabase.CreateFolder("Assets/Data", "Codex");

            var hydra = AssetDatabase.LoadAssetAtPath<CodexEntry>($"{CodexFolder}/Codex_Hydra.asset");
            if (hydra == null)
            {
                hydra = ScriptableObject.CreateInstance<CodexEntry>();
                AssetDatabase.CreateAsset(hydra, $"{CodexFolder}/Codex_Hydra.asset");
            }
            hydra.Id = "hydra-ballista";
            hydra.DisplayName = "Hydra";
            hydra.LockedHint = "Three jaws share one quarrelsome appetite.";
            hydra.UnlockedText = "Hydra — Chain + Fork + Multiple Projectiles";
            hydra.Recipe = new[] { chain, fork, multipleProjectiles };

            var snakeIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Temp/Icons/snake.png");
            hydra.Icon = snakeIcon;
            if (snakeIcon == null)
                Debug.LogWarning("[PR5 Wire] snake.png Sprite not found — Codex Hydra icon will be null (??? fallback).");
            EditorUtility.SetDirty(hydra);

            var catalog = AssetDatabase.LoadAssetAtPath<CodexCatalog>($"{CodexFolder}/CodexCatalog.asset");
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CodexCatalog>();
                AssetDatabase.CreateAsset(catalog, $"{CodexFolder}/CodexCatalog.asset");
            }
            catalog.Entries = new[] { hydra };
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            var cfg = AssetDatabase.LoadAssetAtPath<RunConfig>("Assets/Data/RunConfig_Default.asset");
            if (cfg == null)
            {
                Debug.LogError("[PR5 Wire] Missing RunConfig_Default.asset");
                return;
            }

            cfg.InventoryCapacity = 10;
            cfg.SeedGems = new[] { multipleProjectiles, chain, fork };
            EditorUtility.SetDirty(cfg);

            var scene = EditorSceneManager.OpenScene(RunScenePath);
            var root = Object.FindFirstObjectByType<GameCompositionRoot>();
            if (root == null)
            {
                Debug.LogError("[PR5 Wire] GameCompositionRoot not found in Run.unity");
                return;
            }

            var draftCatalogPath = $"{GemsFolder}/DraftPoolCatalog.asset";
            var draftCatalog = AssetDatabase.LoadAssetAtPath<DraftPoolCatalog>(draftCatalogPath);
            if (draftCatalog == null)
            {
                draftCatalog = ScriptableObject.CreateInstance<DraftPoolCatalog>();
                AssetDatabase.CreateAsset(draftCatalog, draftCatalogPath);
            }

            draftCatalog.Gems = pool;
            EditorUtility.SetDirty(draftCatalog);

            var so = new SerializedObject(root);
            var draftProp = so.FindProperty("draftPoolCatalog");
            if (draftProp == null)
            {
                Debug.LogError("[PR5 Wire] draftPoolCatalog property missing.");
                return;
            }

            draftProp.objectReferenceValue = draftCatalog;
            so.FindProperty("runConfig").objectReferenceValue = cfg;
            so.FindProperty("codexCatalog").objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError("[PR5 Wire] SaveScene failed.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[PR5 Wire] draftPoolCatalog={pool.Length}, Hydra off.");
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
