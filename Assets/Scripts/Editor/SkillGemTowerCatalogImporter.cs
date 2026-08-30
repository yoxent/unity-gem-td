using System;
using System.Collections.Generic;
using System.IO;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
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

        [MenuItem("Gem TD/Import Fireball Proof")]
        public static void ImportFireballProof()
        {
            ImportProofs(
                categoryFile: "poe_skill_gems_spell.json",
                folder: "Spell",
                dialogTitle: "Fireball Import",
                "Fireball");
        }

        [MenuItem("Gem TD/Import Molten Strike Proof")]
        public static void ImportMoltenStrikeProof()
        {
            ImportProofs(
                categoryFile: "poe_skill_gems_attack.json",
                folder: "Attack",
                dialogTitle: "Molten Strike Import",
                "Molten_Strike");
        }

        [MenuItem("Gem TD/Import Simplest Attack Five")]
        public static void ImportSimplestAttackFive()
        {
            ImportProofs(
                categoryFile: "poe_skill_gems_attack.json",
                folder: "Attack",
                dialogTitle: "Simplest Attack Five",
                "Molten_Strike",
                "Earthquake",
                "Lightning_Arrow",
                "Burning_Arrow",
                "Heavy_Strike");
        }

        [MenuItem("Gem TD/Import Attack Proof Set Two")]
        public static void ImportAttackProofSetTwo()
        {
            ImportProofs(
                categoryFile: "poe_skill_gems_attack.json",
                folder: "Attack",
                dialogTitle: "Attack Proof Set Two",
                "Double_Strike",
                "Dual_Strike",
                "Holy_Hammers",
                "Ice_Crash",
                "Kinetic_Blast");
        }

        [MenuItem("Gem TD/Import Spell Proof Set One")]
        public static void ImportSpellProofSetOne()
        {
            ImportProofs(
                categoryFile: "poe_skill_gems_spell.json",
                folder: "Spell",
                dialogTitle: "Spell Proof Set One",
                "Frostbolt",
                "Firestorm",
                "Ice_Nova",
                "Arc",
                "Fireball");
        }

        [MenuItem("Gem TD/Import Curse Proof Set")]
        public static void ImportCurseProofSet()
        {
            ImportProofs(
                categoryFile: "poe_skill_gems_curse.json",
                folder: "Curse",
                dialogTitle: "Curse Proof Set",
                "Temporal_Chains",
                "Vulnerability",
                "Flammability",
                "Frostbite",
                "Conductivity",
                "Despair");
        }

        [MenuItem("Gem TD/Import Remaining Curse Catalog")]
        public static void ImportRemainingCurseCatalog()
        {
            ImportProofs(
                categoryFile: "poe_skill_gems_curse.json",
                folder: "Curse",
                dialogTitle: "Remaining Curse Catalog",
                "Elemental_Weakness",
                "Enfeeble",
                "Punishment",
                "Warlords_Mark",
                "Snipers_Mark",
                "Poachers_Mark",
                "Assassins_Mark",
                "Alchemists_Mark",
                "Bane",
                "Hexblast");
        }

        [MenuItem("Gem TD/Import Aura Catalog")]
        public static void ImportAuraCatalog()
        {
            ImportProofs(
                categoryFile: "poe_skill_gems_aura.json",
                folder: "Aura",
                dialogTitle: "Aura Catalog",
                "Vitality",
                "Flesh_and_Stone",
                "Anger",
                "Determination",
                "Pride",
                "Purity_of_Fire",
                "Precision",
                "Grace",
                "Haste",
                "Hatred",
                "Purity_of_Ice",
                "Clarity",
                "Discipline",
                "Malevolence",
                "Purity_of_Elements",
                "Purity_of_Lightning",
                "Wrath",
                "Zealotry");
        }

        static void ImportProofs(
            string categoryFile,
            string folder,
            string dialogTitle,
            params string[] slugs)
        {
            var jsonDir = EditorPrefs.GetString(PrefsJsonDir, DefaultJsonDir);
            var path = Path.Combine(jsonDir, categoryFile);
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog(
                    dialogTitle,
                    "Missing file:\n" + path,
                    "OK");
                return;
            }

            var results = SkillGemTowerMap.FromCatalogJson(File.ReadAllText(path));
            EnsureFolder(TowerRoot);
            EnsureFolder(RoleRoot);
            EnsureFolder(TowerRoot + "/" + folder);
            EnsureFolder(RoleRoot + "/" + folder);

            var imported = new List<string>(slugs.Length);
            for (var s = 0; s < slugs.Length; s++)
            {
                var slug = slugs[s];
                SkillGemTowerMap.Result match = null;
                for (var i = 0; i < results.Length; i++)
                {
                    if (string.Equals(results[i].Slug, slug, StringComparison.OrdinalIgnoreCase))
                    {
                        match = results[i];
                        break;
                    }
                }

                if (match == null)
                {
                    EditorUtility.DisplayDialog(
                        dialogTitle,
                        slug.Replace('_', ' ') + " was not found in:\n" + path,
                        "OK");
                    return;
                }

                var roles = WriteRoles(match);
                WriteTower(match, roles);
                imported.Add(match.DisplayName);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Gem TD] " + string.Join(", ", imported) + " imported from " + path);
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
                roles[i] = WriteRole(path, kind, result, result.GetRolePayload(kind));
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

        static TowerRoleDefinition WriteRole(
            string path,
            SkillGemTowerMap.RoleKind kind,
            SkillGemTowerMap.Result result,
            SkillGemTowerMap.RolePayload payload)
        {
            TowerRoleDefinition role;
            switch (kind)
            {
                case SkillGemTowerMap.RoleKind.Attack:
                    role = LoadOrCreate<AttackRoleDefinition>(path);
                    break;
                case SkillGemTowerMap.RoleKind.Spell:
                    role = LoadOrCreate<SpellRoleDefinition>(path);
                    break;
                case SkillGemTowerMap.RoleKind.Curse:
                    role = LoadOrCreate<CurseRoleDefinition>(path);
                    break;
                case SkillGemTowerMap.RoleKind.Aura:
                    role = LoadOrCreate<AuraRoleDefinition>(path);
                    break;
                case SkillGemTowerMap.RoleKind.Trap:
                    role = LoadOrCreate<TrapRoleDefinition>(path);
                    break;
                default:
                    role = LoadOrCreate<MineRoleDefinition>(path);
                    break;
            }

            ClearRoleBehaviorDefaults(role, result.Tags, result.Slug, kind);
            role.Modifiers = payload != null
                ? CopyModifiers(payload.Modifiers)
                : Array.Empty<RoleStatModifier>();
            role.Effects = payload != null
                ? CopyEffects(payload.Effects)
                : Array.Empty<RoleEffectModifier>();
            role.Levels = payload != null
                ? CreateLevels(payload.Levels)
                : Array.Empty<RoleLevelDefinition>();
            role.EffectPayloads = payload != null
                ? CopyEffectPayloads(payload.EffectPayloads)
                : Array.Empty<EffectPayloadDefinition>();
            EditorUtility.SetDirty(role);
            return role;
        }

        static void ClearRoleBehaviorDefaults(
            TowerRoleDefinition role,
            GemTag towerTags,
            string slug,
            SkillGemTowerMap.RoleKind kind)
        {
            var damage = role as DamageRoleDefinition;
            if (damage != null)
            {
                damage.PierceBehavior = PierceMode.Finite;
                SkillGemTowerMap.ResolveFireBehavior(towerTags, slug, kind, out var aim, out var delivery);
                damage.AimMode = aim;
                damage.DeliveryPattern = delivery;
                damage.Mix = SkillGemTowerMap.ResolveProofMix(slug);
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

        static RoleLevelDefinition[] CreateLevels(RoleLevelDefinition[] sourceDefinitions)
        {
            if (sourceDefinitions != null && sourceDefinitions.Length > 0)
            {
                var mapped = new RoleLevelDefinition[sourceDefinitions.Length];
                for (var i = 0; i < sourceDefinitions.Length; i++)
                {
                    var source = sourceDefinitions[i];
                    mapped[i] = new RoleLevelDefinition
                    {
                        SourceLevel = source.SourceLevel,
                        Modifiers = CopyModifiers(source.Modifiers),
                        Effects = CopyEffects(source.Effects)
                    };
                }

                return mapped;
            }

            return Array.Empty<RoleLevelDefinition>();
        }

        static RoleStatModifier[] CopyModifiers(RoleStatModifier[] modifiers)
        {
            if (modifiers == null || modifiers.Length == 0)
                return Array.Empty<RoleStatModifier>();

            var copy = new RoleStatModifier[modifiers.Length];
            Array.Copy(modifiers, copy, modifiers.Length);
            return copy;
        }

        static RoleEffectModifier[] CopyEffects(RoleEffectModifier[] effects)
        {
            if (effects == null || effects.Length == 0)
                return Array.Empty<RoleEffectModifier>();

            var copy = new RoleEffectModifier[effects.Length];
            Array.Copy(effects, copy, effects.Length);
            return copy;
        }

        static EffectPayloadDefinition[] CopyEffectPayloads(EffectPayloadDefinition[] payloads)
        {
            if (payloads == null || payloads.Length == 0)
                return Array.Empty<EffectPayloadDefinition>();

            var copy = new EffectPayloadDefinition[payloads.Length];
            for (var i = 0; i < payloads.Length; i++)
            {
                var source = payloads[i];
                copy[i] = source != null
                    ? new EffectPayloadDefinition
                    {
                        Trigger = source.Trigger,
                        Anchor = source.Anchor,
                        TravelPattern = source.TravelPattern,
                        ScatterPattern = source.ScatterPattern,
                        HitPolicy = source.HitPolicy,
                        Tags = source.Tags,
                        Count = source.Count,
                        DamageMultiplier = source.DamageMultiplier,
                        AoeRadius = source.AoeRadius,
                        MinDistance = source.MinDistance,
                        MaxDistance = source.MaxDistance,
                        ArcHeight = source.ArcHeight,
                        DelaySeconds = source.DelaySeconds,
                        IntervalSeconds = source.IntervalSeconds,
                        RepeatCount = source.RepeatCount
                    }
                    : null;
            }

            return copy;
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
