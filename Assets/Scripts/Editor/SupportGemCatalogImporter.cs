using System;
using System.Collections.Generic;
using System.IO;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;
using UnityEditor;
using UnityEngine;

namespace GemTD.Editor
{
    public static class SupportGemCatalogImporter
    {
        const string PrefsJsonDir = "GemTD.SkillGemJsonDir";
        const string DefaultJsonDir = @"E:\Projects\Docs\project-docs\Unity\unity-gem-td\sources";
        const string SupportJsonFile = "poe1_inspired_tower_support_gems.json";
        const string GemRoot = "Assets/Data/Gems";
        const string GemPoolPath = "Assets/Data/Gems/DraftPoolCatalog.asset";
        const string RarityTablePath = "Assets/Data/Gems/GemRarityTable.asset";
        const string CampaignCatalogPath = "Assets/Data/Draft/DraftCatalog_Campaign.asset";

        sealed class DefinitionIndex
        {
            public readonly Dictionary<string, List<GemDefinition>> ByFileKey =
                new Dictionary<string, List<GemDefinition>>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, List<GemDefinition>> ByDisplaySlug =
                new Dictionary<string, List<GemDefinition>>(StringComparer.OrdinalIgnoreCase);
        }

        [MenuItem("Gem TD/Import Support Gem Catalog")]
        public static void Import()
        {
            var jsonDir = EditorPrefs.GetString(PrefsJsonDir, DefaultJsonDir);
            var jsonPath = Path.Combine(jsonDir, SupportJsonFile);
            if (!File.Exists(jsonPath))
            {
                Abort("Missing support-gem JSON:\n" + jsonPath);
                return;
            }

            SupportGemMap.Result[] results;
            try
            {
                results = SupportGemMap.FromCatalogJson(File.ReadAllText(jsonPath));
            }
            catch (Exception exception)
            {
                Abort("Support-gem JSON could not be parsed:\n" + exception.Message);
                return;
            }

            if (!ValidateSourceResults(results, out var sourceError))
            {
                Abort(sourceError);
                return;
            }

            if (!TryBuildDefinitionIndex(out var index, out var indexError))
            {
                Abort(indexError);
                return;
            }

            var matched = new GemDefinition[results.Length];
            var matchedIds = new HashSet<GemId>();
            for (var i = 0; i < results.Length; i++)
            {
                if (!TryResolveDefinition(index, results[i], out var gem, out var matchError))
                {
                    Abort(matchError);
                    return;
                }

                if (gem == null || gem.Id == GemId.None)
                {
                    Abort("Source slug '" + results[i].Slug
                        + "' resolved to a GemDefinition with GemId.None. No assets changed.");
                    return;
                }

                if (!matchedIds.Add(gem.Id))
                {
                    Abort("Duplicate family ID " + gem.Id
                        + " was resolved from source slug '" + results[i].Slug
                        + ". No assets changed.");
                    return;
                }

                matched[i] = gem;
            }

            var pool = AssetDatabase.LoadAssetAtPath<DraftPoolCatalog>(GemPoolPath);
            if (pool == null)
            {
                Abort("Missing existing DraftPoolCatalog asset:\n" + GemPoolPath);
                return;
            }

            var campaign = AssetDatabase.LoadAssetAtPath<DraftCatalog>(CampaignCatalogPath);
            if (campaign == null)
            {
                Abort("Missing existing campaign DraftCatalog asset:\n" + CampaignCatalogPath);
                return;
            }

            var rarityTable = AssetDatabase.LoadAssetAtPath<GemRarityTable>(RarityTablePath);
            if (rarityTable == null && AssetDatabase.LoadMainAssetAtPath(RarityTablePath) != null)
            {
                Abort("Existing asset at the rarity-table path is not a GemRarityTable:\n"
                    + RarityTablePath);
                return;
            }

            if (rarityTable == null)
            {
                rarityTable = ScriptableObject.CreateInstance<GemRarityTable>();
                AssetDatabase.CreateAsset(rarityTable, RarityTablePath);
            }

            for (var i = 0; i < results.Length; i++)
            {
                var result = results[i];
                var gem = matched[i];
                gem.DisplayName = result.DisplayName;
                gem.Description = result.Description;
                gem.Tags = result.Tags;
                gem.Modifiers = result.CanIngest && result.Modifiers != null
                    ? result.Modifiers
                    : Array.Empty<GemStatModifier>();
                EditorUtility.SetDirty(gem);
            }

            var ordered = new GemDefinition[matched.Length];
            Array.Copy(matched, ordered, matched.Length);
            Array.Sort(ordered, CompareGemIds);

            pool.Gems = ordered;
            campaign.RarityTable = rarityTable;
            EditorUtility.SetDirty(pool);
            EditorUtility.SetDirty(campaign);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var skipped = 0;
            for (var i = 0; i < results.Length; i++)
            {
                if (!results[i].CanIngest)
                    skipped++;
            }

            Debug.Log(
                "[Gem TD] Support-gem catalog imported. source=" + results.Length
                + " matched=" + matched.Length
                + " skipped=" + skipped
                + " rarityTable=" + RarityTablePath);
        }

        static bool ValidateSourceResults(
            SupportGemMap.Result[] results,
            out string error)
        {
            if (results == null || results.Length != SupportGemMap.ExpectedGemCount)
            {
                error = "Expected " + SupportGemMap.ExpectedGemCount
                    + " support gems, found " + (results == null ? 0 : results.Length)
                    + ". No assets changed.";
                return false;
            }

            var slugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < results.Length; i++)
            {
                var result = results[i];
                if (result == null || string.IsNullOrEmpty(result.Slug))
                {
                    error = "Source entry " + i + " has no usable slug. No assets changed.";
                    return false;
                }

                if (!slugs.Add(result.Slug))
                {
                    error = "Duplicate source slug '" + result.Slug
                        + "'. No assets changed.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        static bool TryBuildDefinitionIndex(
            out DefinitionIndex index,
            out string error)
        {
            index = new DefinitionIndex();
            var guids = AssetDatabase.FindAssets("t:GemDefinition", new[] { GemRoot });
            var ids = new HashSet<GemId>();
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var gem = AssetDatabase.LoadAssetAtPath<GemDefinition>(path);
                if (gem == null)
                {
                    error = "Could not load GemDefinition at " + path + ". No assets changed.";
                    return false;
                }

                if (gem.Id == GemId.None)
                {
                    error = "GemDefinition at " + path
                        + " has GemId.None. No assets changed.";
                    return false;
                }

                if (!ids.Add(gem.Id))
                {
                    error = "Duplicate GemId " + gem.Id
                        + " exists in the discovered GemDefinition assets. No assets changed.";
                    return false;
                }

                var fileName = Path.GetFileNameWithoutExtension(path);
                var fileKey = fileName.StartsWith("Gem_", StringComparison.OrdinalIgnoreCase)
                    ? fileName.Substring("Gem_".Length)
                    : fileName;
                Add(index.ByFileKey, fileKey, gem);
                Add(index.ByDisplaySlug, SupportGemMap.SlugFromName(gem.DisplayName), gem);
            }

            if (guids.Length != SupportGemMap.ExpectedGemCount)
            {
                error = "Expected " + SupportGemMap.ExpectedGemCount
                    + " GemDefinition assets, found " + guids.Length
                    + ". No assets changed.";
                return false;
            }

            error = null;
            return true;
        }

        static bool TryResolveDefinition(
            DefinitionIndex index,
            SupportGemMap.Result result,
            out GemDefinition gem,
            out string error)
        {
            var fileKey = AssetKeyForSourceSlug(result.Slug);
            var fileMatches = GetMatches(index.ByFileKey, fileKey);
            var displayMatches = GetMatches(index.ByDisplaySlug, result.Slug);

            if (fileMatches.Count > 1)
            {
                gem = null;
                error = "Source slug '" + result.Slug
                    + "' maps to duplicate filename key '" + fileKey
                    + "'. No assets changed.";
                return false;
            }

            if (fileMatches.Count == 1)
            {
                gem = fileMatches[0];
                for (var i = 0; i < displayMatches.Count; i++)
                {
                    if (displayMatches[i] != gem)
                    {
                        gem = null;
                        error = "Source slug '" + result.Slug
                            + "' has ambiguous filename/display-name asset matches. No assets changed.";
                        return false;
                    }
                }

                error = null;
                return true;
            }

            if (displayMatches.Count == 1)
            {
                gem = displayMatches[0];
                error = null;
                return true;
            }

            gem = null;
            error = displayMatches.Count == 0
                ? "No GemDefinition asset matches source slug '" + result.Slug
                    + "' (filename key '" + fileKey + "'). No assets changed."
                : "Source slug '" + result.Slug
                    + "' maps to duplicate display-name assets. No assets changed.";
            return false;
        }

        static List<GemDefinition> GetMatches(
            Dictionary<string, List<GemDefinition>> index,
            string key)
        {
            if (key != null && index.TryGetValue(key, out var matches))
                return matches;
            return EmptyMatches;
        }

        static readonly List<GemDefinition> EmptyMatches = new List<GemDefinition>(0);

        static void Add(
            Dictionary<string, List<GemDefinition>> index,
            string key,
            GemDefinition gem)
        {
            if (string.IsNullOrEmpty(key))
                return;
            if (!index.TryGetValue(key, out var matches))
            {
                matches = new List<GemDefinition>(1);
                index.Add(key, matches);
            }

            matches.Add(gem);
        }

        static int CompareGemIds(GemDefinition a, GemDefinition b)
        {
            var idCompare = ((int)a.Id).CompareTo((int)b.Id);
            if (idCompare != 0)
                return idCompare;
            return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
        }

        static string AssetKeyForSourceSlug(string slug)
        {
            switch (slug)
            {
                case "chance_to_bleed":
                    return "Chance_to_Bleed";
                case "increased_area_of_effect":
                    return "IncreasedArea";
                default:
                    return ToPascal(slug);
            }
        }

        static string ToPascal(string slug)
        {
            if (string.IsNullOrEmpty(slug))
                return "";

            var chars = new char[slug.Length];
            var written = 0;
            var capitalize = true;
            for (var i = 0; i < slug.Length; i++)
            {
                var c = slug[i];
                if (!char.IsLetterOrDigit(c))
                {
                    capitalize = true;
                    continue;
                }

                chars[written++] = capitalize
                    ? char.ToUpperInvariant(c)
                    : c;
                capitalize = false;
            }

            return new string(chars, 0, written);
        }

        static void Abort(string message)
        {
            var fullMessage = "[Gem TD] Support-gem import aborted: " + message;
            Debug.LogError(fullMessage);
            EditorUtility.DisplayDialog(
                "Support Gem Import",
                message + "\n\nNo assets changed.",
                "OK");
        }
    }
}
