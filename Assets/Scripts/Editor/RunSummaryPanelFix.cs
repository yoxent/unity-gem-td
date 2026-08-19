#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using GemTD.UI;

namespace GemTD.Editor
{
    /// <summary>Cleans baked tower sections and wires pooled section parent on RunSummaryPanel.</summary>
    public static class RunSummaryPanelFix
    {
        const string PrefabPath = "Assets/Prefabs/UI/Draft/RunSummaryPanel.prefab";
        const string SectionPrefabPath = "Assets/Prefabs/UI/Draft/RunSummarySection.prefab";

        [MenuItem("Gem TD/Fix Run Summary Panel Wiring")]
        public static void Fix()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError("RunSummaryPanelFix: missing prefab.");
                return;
            }

            var panel = root.transform.Find("Panel");
            if (panel == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                Debug.LogError("RunSummaryPanelFix: missing Panel.");
                return;
            }

            var controller = root.GetComponent<RunSummaryController>();
            if (controller == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                Debug.LogError("RunSummaryPanelFix: missing RunSummaryController.");
                return;
            }

            var sectionPrefab = AssetDatabase.LoadAssetAtPath<RunSummarySection>(SectionPrefabPath);
            if (sectionPrefab == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                Debug.LogError("RunSummaryPanelFix: missing RunSummarySection prefab.");
                return;
            }

            DestroyBakedSections(panel);

            var sectionsParent = EnsureSectionsParent(panel);
            ClearChildren(sectionsParent);

            var so = new SerializedObject(controller);
            so.FindProperty("towerSectionsParent").objectReferenceValue = sectionsParent;
            so.FindProperty("towerSummarySectionPrefab").objectReferenceValue = sectionPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            Debug.Log("RunSummaryPanelFix: cleaned and wired " + PrefabPath);
        }

        static Transform EnsureSectionsParent(Transform panel)
        {
            Transform chosen = null;
            for (var i = 0; i < panel.childCount; i++)
            {
                var child = panel.GetChild(i);
                if (child.name != "TowerSections")
                    continue;

                if (chosen == null)
                    chosen = child;
                else
                    Object.DestroyImmediate(child.gameObject);
            }

            if (chosen != null)
            {
                StripScrollComponents(chosen.gameObject);
                return chosen;
            }

            var go = new GameObject("TowerSections", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(panel, false);
            go.transform.SetSiblingIndex(panel.childCount - 2);
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            go.GetComponent<LayoutElement>().preferredHeight = 380f;
            return go.transform;
        }

        static void StripScrollComponents(GameObject go)
        {
            var scroll = go.GetComponent<ScrollRect>();
            if (scroll != null)
                Object.DestroyImmediate(scroll);
            var mask = go.GetComponent<RectMask2D>();
            if (mask != null)
                Object.DestroyImmediate(mask);
            if (go.GetComponent<VerticalLayoutGroup>() == null)
                go.AddComponent<VerticalLayoutGroup>();
        }

        static void DestroyBakedSections(Transform panel)
        {
            for (var i = panel.childCount - 1; i >= 0; i--)
            {
                var child = panel.GetChild(i);
                if (child.name.StartsWith("TowerSection"))
                    Object.DestroyImmediate(child.gameObject);
            }

            for (var i = 0; i < panel.childCount; i++)
            {
                var sections = panel.GetChild(i);
                if (sections.name != "TowerSections")
                    continue;
                ClearChildren(sections);
            }
        }

        static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }
}
#endif
