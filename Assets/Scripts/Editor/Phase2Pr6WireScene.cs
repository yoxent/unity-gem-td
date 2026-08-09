using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using GemTD.UI;

namespace GemTD.Editor
{
    /// <summary>Builds the Speed panel UI in Run.unity and wires RunHudView serialized refs
    /// at editor time (no runtime instantiation). PR6 sub-PR B-1.</summary>
    public static class Phase2Pr6WireScene
    {
        const string RunScenePath = "Assets/Scenes/Run.unity";

        [MenuItem("Gem TD/Phase 2 PR6 Wire Speed Panel")]
        public static void Wire()
        {
            var scene = EditorSceneManager.OpenScene(RunScenePath);
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[PR6 Wire] No Canvas in Run.unity.");
                return;
            }

            // Build the SpeedPanel tree under the canvas (or reuse if present).
            var panelGo = FindOrCreate(canvas.transform, "SpeedPanel");
            var panelRt = panelGo.GetComponent<RectTransform>();
            var panelImg = panelGo.GetComponent<Image>();
            panelRt.anchorMin = new Vector2(1f, 1f);
            panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.pivot = new Vector2(1f, 1f);
            panelRt.sizeDelta = new Vector2(180f, 44f);
            panelRt.anchoredPosition = new Vector2(-12f, -12f);
            panelImg.color = new Color(0.07f, 0.08f, 0.1f, 0.9f);

            var speed1 = MakeSpeedButton(panelGo.transform, "Speed1", "1x", new Vector2(-66f, 0f));
            var speed2 = MakeSpeedButton(panelGo.transform, "Speed2", "2x", new Vector2(0f, 0f));
            var speed4 = MakeSpeedButton(panelGo.transform, "Speed4", "4x", new Vector2(66f, 0f));

            // Pause chip (disabled by default).
            var chipGo = FindOrCreate(panelGo.transform, "PauseChip");
            var chipRt = chipGo.GetComponent<RectTransform>();
            chipRt.anchorMin = new Vector2(0.5f, 1f);
            chipRt.anchorMax = new Vector2(0.5f, 1f);
            chipRt.pivot = new Vector2(0.5f, 0f);
            chipRt.sizeDelta = new Vector2(120f, 20f);
            chipRt.anchoredPosition = new Vector2(0f, 4f);
            chipGo.GetComponent<Image>().color = new Color(0.45f, 0.14f, 0.14f, 0.9f);
            var chipText = FindOrCreateText(chipGo.transform, "PauseText", "PAUSED", 13);
            chipGo.SetActive(false);

            // Wire RunHudView serialized refs.
            var hud = canvas.GetComponent<RunHudView>();
            if (hud == null)
            {
                Debug.LogError("[PR6 Wire] RunHudView not found on canvas.");
                return;
            }
            var so = new SerializedObject(hud);
            so.FindProperty("speedBtn1").objectReferenceValue = speed1;
            so.FindProperty("speedBtn2").objectReferenceValue = speed2;
            so.FindProperty("speedBtn4").objectReferenceValue = speed4;
            so.FindProperty("pauseChip").objectReferenceValue = chipGo;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PR6 Wire] Speed panel wired and serialized to RunHudView.");
        }

        static GameObject FindOrCreate(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
                return existing.gameObject;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            return go;
        }

        static GameObject FindOrCreateText(Transform parent, string name, string value, int fontSize)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                var t = existing.GetComponent<Text>();
                if (t != null) t.text = value;
                return existing.gameObject;
            }
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            return go;
        }

        static Button MakeSpeedButton(Transform parent, string name, string label, Vector2 anchored)
        {
            var go = FindOrCreate(parent, name);
            // Ensure Button + Image components exist.
            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.color = new Color(0.22f, 0.28f, 0.38f, 1f);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(48f, 28f);
            rt.anchoredPosition = anchored;

            var labelGo = FindOrCreateText(go.transform, "Label", label, 14);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            return btn;
        }
    }
}