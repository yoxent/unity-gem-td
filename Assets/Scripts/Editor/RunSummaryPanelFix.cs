#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

            var totalGoldText = EnsureTotalGoldText(panel);

            var so = new SerializedObject(controller);
            so.FindProperty("towerSectionsParent").objectReferenceValue = sectionsParent;
            so.FindProperty("towerSummarySectionPrefab").objectReferenceValue = sectionPrefab;
            so.FindProperty("totalGoldText").objectReferenceValue = totalGoldText;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            Debug.Log("RunSummaryPanelFix: cleaned and wired " + PrefabPath);
        }

        static TMP_Text EnsureTotalGoldText(Transform panel)
        {
            var existing = panel.Find("TotalGoldText");
            if (existing != null)
            {
                var tmp = existing.GetComponent<TMP_Text>();
                if (tmp != null)
                    return tmp;
            }

            var kills = panel.Find("TotalKillsText");
            var go = new GameObject("TotalGoldText", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(panel, false);
            if (kills != null)
                go.transform.SetSiblingIndex(kills.GetSiblingIndex() + 1);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = "Gold earned: 0";
            text.fontSize = 22;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 28f;

            var panelRect = panel as RectTransform;
            if (panelRect != null && panelRect.sizeDelta.y < 760f)
                panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, 760f);

            return text;
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
