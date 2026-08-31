using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GemTD.Gameplay.Towers;
using UnityEditor;
using UnityEngine;

namespace GemTD.Editor
{
    public static class WikiTowerCatalogExporter
    {
        const string PrefsDocsRoot = "GemTD.WikiDocsRoot";
        const string DefaultDocsRoot = @"E:\Projects\Docs\project-docs\Unity\unity-gem-td";
        const string TowerAssetRoot = "Assets/Data/Towers/Catalog";

        [MenuItem("Gem TD/Export Wiki Tower Catalog (Completed)")]
        public static void ExportCompleted()
        {
            var docsRoot = EditorPrefs.GetString(PrefsDocsRoot, DefaultDocsRoot);
            if (!Directory.Exists(docsRoot))
            {
                EditorUtility.DisplayDialog(
                    "Wiki Export",
                    "Docs root not found:\n" + docsRoot + "\n\nSet EditorPrefs '" + PrefsDocsRoot + "'.",
                    "OK");
                return;
            }

            var pages = new List<WikiTowerPage>(WikiTowerCatalogSets.Completed.Length);
            var missing = new List<string>();
            for (var i = 0; i < WikiTowerCatalogSets.Completed.Length; i++)
            {
                var entry = WikiTowerCatalogSets.Completed[i];
                var assetPath = TowerAssetRoot + "/" + entry.CategoryName + "/Tower_" + entry.Slug + ".asset";
                var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>(assetPath);
                if (tower == null)
                {
                    missing.Add(entry.Slug + " (" + assetPath + ")");
                    continue;
                }

                pages.Add(WikiTowerPageBuilder.From(tower, entry));
            }

            if (missing.Count > 0)
            {
                Debug.LogError(
                    "[Gem TD] Wiki export skipped missing towers: " + string.Join("; ", missing));
            }

            var towersRoot = Path.Combine(docsRoot, "wiki", "catalog", "towers");
            Directory.CreateDirectory(towersRoot);

            var attack = Collect(pages, "attack");
            var spell = Collect(pages, "spell");
            var curse = Collect(pages, "curse");
            var aura = Collect(pages, "aura");

            WriteCategory(towersRoot, "Attack", attack);
            WriteCategory(towersRoot, "Spell", spell);
            WriteCategory(towersRoot, "Curse", curse);
            WriteCategory(towersRoot, "Aura", aura);
            PruneStalePages(Path.Combine(towersRoot, "attack"), attack);
            PruneStalePages(Path.Combine(towersRoot, "spell"), spell);
            PruneStalePages(Path.Combine(towersRoot, "curse"), curse);
            PruneStalePages(Path.Combine(towersRoot, "aura"), aura);
            File.WriteAllText(
                Path.Combine(towersRoot, "README.md"),
                WikiTowerMarkdown.TowersRootIndex(attack.Count, spell.Count, curse.Count, aura.Count),
                new UTF8Encoding(false));

            Debug.Log(
                "[Gem TD] Wiki tower catalog exported. pages=" + pages.Count +
                " missing=" + missing.Count +
                " root=" + towersRoot);
        }

        static List<WikiTowerPage> Collect(List<WikiTowerPage> pages, string folder)
        {
            var selected = new List<WikiTowerPage>();
            for (var i = 0; i < pages.Count; i++)
            {
                if (pages[i].CategoryFolder == folder)
                    selected.Add(pages[i]);
            }

            selected.Sort(CompareSlug);
            return selected;
        }

        static int CompareSlug(WikiTowerPage a, WikiTowerPage b)
        {
            return string.CompareOrdinal(a.Slug, b.Slug);
        }

        static void WriteCategory(string towersRoot, string categoryName, List<WikiTowerPage> pages)
        {
            var folder = Path.Combine(towersRoot, categoryName.ToLowerInvariant());
            Directory.CreateDirectory(folder);

            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                var path = Path.Combine(folder, WikiTowerMarkdown.FileNameFromSlug(page.Slug));
                var generated = WikiTowerMarkdown.TowerPage(page);
                var existing = File.Exists(path) ? File.ReadAllText(path) : null;
                File.WriteAllText(path, WikiTowerMarkdown.MergeNotes(generated, existing), new UTF8Encoding(false));
            }

            File.WriteAllText(
                Path.Combine(folder, "README.md"),
                WikiTowerMarkdown.CategoryIndex(categoryName, pages.ToArray()),
                new UTF8Encoding(false));
        }

        static void PruneStalePages(string folder, List<WikiTowerPage> pages)
        {
            if (!Directory.Exists(folder))
                return;

            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "README.md" };
            for (var i = 0; i < pages.Count; i++)
                keep.Add(WikiTowerMarkdown.FileNameFromSlug(pages[i].Slug));

            var files = Directory.GetFiles(folder, "*.md");
            for (var i = 0; i < files.Length; i++)
            {
                var name = Path.GetFileName(files[i]);
                if (!keep.Contains(name))
                    File.Delete(files[i]);
            }
        }
    }
}
