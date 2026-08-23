using System;
using System.Collections.Generic;
using System.IO;
using GemTD.Gameplay.Towers;
using UnityEditor;
using UnityEngine;

namespace GemTD.Editor
{
    public static class SkillGemTowerCatalogImporter
    {
        const string PrefsJsonDir = "GemTD.SkillGemJsonDir";
        const string DefaultJsonDir = @"E:\Projects\Docs\project-docs\Unity\unity-gem-td\sources";
        const string TowerCatalogPath = "Assets/Data/Towers/TowerCatalog.asset";
        const string TowerRoot = "Assets/Data/Towers/Catalog";
        const string RoleRoot = "Assets/Data/Towers/Roles/Catalog";

        static readonly string[] Categories = { "attack", "spell", "curse", "aura", "trap", "mine" };

        [MenuItem("Gem TD/Import Skill Gem Tower Catalog")]
        public static void Import()
        {
            var jsonDir = EditorPrefs.GetString(PrefsJsonDir, DefaultJsonDir);
            if (!Directory.Exists(jsonDir))
            {
                EditorUtility.DisplayDialog(
                    "Skill Gem Import",
                    "JSON folder not found:\n" + jsonDir + "\n\nSet EditorPrefs '" + PrefsJsonDir + "' to your docs sources folder.",
                    "OK");
                return;
            }

            var mapped = new List<SkillGemTowerMap.Result>(SkillGemTowerMap.ExpectedGemCount);
            var perFile = new List<string>(Categories.Length);
            for (var i = 0; i < Categories.Length; i++)
            {
                var category = Categories[i];
                var path = Path.Combine(jsonDir, "poe_skill_gems_" + category + ".json");
                if (!File.Exists(path))
                {
                    EditorUtility.DisplayDialog("Skill Gem Import", "Missing file:\n" + path, "OK");
                    return;
                }

                var results = SkillGemTowerMap.FromCatalogJson(File.ReadAllText(path));
                perFile.Add(category + "=" + results.Length);
                mapped.AddRange(results);
            }

            if (mapped.Count != SkillGemTowerMap.ExpectedGemCount)
            {
                EditorUtility.DisplayDialog(
                    "Skill Gem Import",
                    "Expected " + SkillGemTowerMap.ExpectedGemCount + " gems, found " + mapped.Count +
                    " (" + string.Join(", ", perFile) + "). Import aborted. Not inventing gems.",
                    "OK");
                return;
            }

            mapped.Sort(CompareCatalogOrder);

            EnsureFolder(TowerRoot);
            EnsureFolder(RoleRoot);
            for (var i = 0; i < Categories.Length; i++)
            {
                var folder = ToFolderName(Categories[i]);
                EnsureFolder(TowerRoot + "/" + folder);
                EnsureFolder(RoleRoot + "/" + folder);
            }

            var towers = new List<TowerDefinition>(mapped.Count);
            var skippedIncompatible = new List<string>();
            for (var i = 0; i < mapped.Count; i++)
            {
                var result = mapped[i];
                if (!result.IsActiveCatalogCompatible)
                {
                    WriteRoles(result);
                    skippedIncompatible.Add(FormatSkippedEntry(result));
                    continue;
                }

                var roles = WriteRoles(result);
                towers.Add(WriteTower(result, roles));
            }

            var catalog = LoadOrCreate<TowerCatalog>(TowerCatalogPath);
            catalog.Towers = towers.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Gem TD] Skill-gem tower catalog imported. Towers=" + towers.Count +
                " skippedIncompatible=" + skippedIncompatible.Count +
                " (" + string.Join(", ", perFile) + ").");
            if (skippedIncompatible.Count > 0)
            {
                Debug.LogWarning(
                    "[Gem TD] Omitted incompatible skill-gem towers: " +
                    string.Join("; ", skippedIncompatible));
            }
        }

        static int CompareCatalogOrder(SkillGemTowerMap.Result a, SkillGemTowerMap.Result b)
        {
            var ca = CategoryIndex(a.Category);
            var cb = CategoryIndex(b.Category);
            if (ca != cb)
                return ca.CompareTo(cb);
            return string.Compare(a.Slug, b.Slug, StringComparison.OrdinalIgnoreCase);
        }

        static int CategoryIndex(string category)
        {
            for (var i = 0; i < Categories.Length; i++)
            {
                if (Categories[i] == category)
                    return i;
            }

            return Categories.Length;
        }

        static string ToFolderName(string category)
        {
            if (string.IsNullOrEmpty(category))
                return "Attack";
            return char.ToUpperInvariant(category[0]) + category.Substring(1);
        }

        static TowerRoleDefinition[] WriteRoles(SkillGemTowerMap.Result result)
        {
            var roles = new TowerRoleDefinition[result.RoleKinds.Length];
            for (var i = 0; i < result.RoleKinds.Length; i++)
            {
                var kind = result.RoleKinds[i];
                var folder = ToFolderName(RoleFolder(kind));
                var path = RoleRoot + "/" + folder + "/Role_" + folder + "_" + result.Slug + ".asset";
                roles[i] = WriteRole(path, kind, result);
            }

            return roles;
        }

        static string RoleFolder(SkillGemTowerMap.RoleKind kind)
        {
            switch (kind)
            {
                case SkillGemTowerMap.RoleKind.Attack: return "attack";
                case SkillGemTowerMap.RoleKind.Spell: return "spell";
                case SkillGemTowerMap.RoleKind.Curse: return "curse";
                case SkillGemTowerMap.RoleKind.Aura: return "aura";
                case SkillGemTowerMap.RoleKind.Trap: return "trap";
                case SkillGemTowerMap.RoleKind.Mine: return "mine";
                default: return "attack";
            }
        }

        static TowerRoleDefinition WriteRole(string path, SkillGemTowerMap.RoleKind kind, SkillGemTowerMap.Result result)
        {
            switch (kind)
            {
                case SkillGemTowerMap.RoleKind.Attack:
                {
                    var role = LoadOrCreate<AttackRoleDefinition>(path);
                    role.AttackTime = result.AttackTime;
                    role.AttackSpeed = result.AttackSpeed;
                    role.TowerRadius = result.TowerRadius;
                    role.Levels = CreateLevels(result);
                    EditorUtility.SetDirty(role);
                    return role;
                }
                case SkillGemTowerMap.RoleKind.Spell:
                {
                    var role = LoadOrCreate<SpellRoleDefinition>(path);
                    role.CastTime = result.CastTime;
                    role.CastSpeed = result.CastSpeed;
                    role.TowerRadius = result.TowerRadius;
                    role.Levels = CreateLevels(result);
                    EditorUtility.SetDirty(role);
                    return role;
                }
                case SkillGemTowerMap.RoleKind.Curse:
                {
                    var role = LoadOrCreate<CurseRoleDefinition>(path);
                    role.CastTime = result.CastTime;
                    role.CastSpeed = result.CastSpeed;
                    role.TowerRadius = result.TowerRadius;
                    role.Levels = CreateLevels(result);
                    EditorUtility.SetDirty(role);
                    return role;
                }
                case SkillGemTowerMap.RoleKind.Aura:
                {
                    var role = LoadOrCreate<AuraRoleDefinition>(path);
                    role.TowerRadius = result.AuraTowerRadius > 0f
                        ? result.AuraTowerRadius
                        : result.TowerRadius;
                    role.ReservationPercent = result.ReservationPercent;
                    role.Levels = CreateLevels(result);
                    EditorUtility.SetDirty(role);
                    return role;
                }
                case SkillGemTowerMap.RoleKind.Trap:
                {
                    var role = LoadOrCreate<TrapRoleDefinition>(path);
                    role.CastTime = result.CastTime;
                    role.CastSpeed = result.CastSpeed;
                    role.TowerRadius = result.TowerRadius;
                    role.Levels = CreateLevels(result);
                    EditorUtility.SetDirty(role);
                    return role;
                }
                default:
                {
                    var role = LoadOrCreate<MineRoleDefinition>(path);
                    role.CastTime = result.CastTime;
                    role.CastSpeed = result.CastSpeed;
                    role.TowerRadius = result.TowerRadius;
                    role.Levels = CreateLevels(result);
                    EditorUtility.SetDirty(role);
                    return role;
                }
            }
        }

        static TowerDefinition WriteTower(SkillGemTowerMap.Result result, TowerRoleDefinition[] roles)
        {
            var folder = ToFolderName(result.Category);
            var path = TowerRoot + "/" + folder + "/Tower_" + result.Slug + ".asset";
            var tower = LoadOrCreate<TowerDefinition>(path);
            tower.DisplayName = result.DisplayName;
            tower.Cost = result.Cost;
            tower.BuildIncrement = result.BuildIncrement;
            tower.Damage = result.Damage;
            tower.Roles = roles;
            tower.SocketCount = result.SocketCount;
            tower.AllowsHydraEvolution = false;
            tower.Tags = result.Tags;
            EditorUtility.SetDirty(tower);
            return tower;
        }

        static RoleLevelDefinition[] CreateLevels(SkillGemTowerMap.Result result)
        {
            var sourceLevels = result.SourceLevels;
            if (sourceLevels == null || sourceLevels.Length == 0)
                return null;

            var levels = new RoleLevelDefinition[sourceLevels.Length];
            for (var i = 0; i < sourceLevels.Length; i++)
            {
                levels[i] = new RoleLevelDefinition
                {
                    SourceLevel = sourceLevels[i],
                    Modifiers = Array.Empty<RoleStatModifier>()
                };
            }

            return levels;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;
            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        static string FormatSkippedEntry(SkillGemTowerMap.Result result)
        {
            var keys = result.UnsupportedEffectKeys;
            var effects = keys == null || keys.Length == 0
                ? "(no non-base level effect keys)"
                : string.Join(" | ", keys);
            return result.DisplayName + " [" + result.Slug + "] effects=" + effects;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            var parent = Path.GetDirectoryName(folder);
            if (parent != null)
            {
                parent = parent.Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(parent))
                    EnsureFolder(parent);
            }

            var name = Path.GetFileName(folder);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
