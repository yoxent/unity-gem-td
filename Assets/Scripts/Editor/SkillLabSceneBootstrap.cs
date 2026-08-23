using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using GemTD.Gameplay.CameraControl;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.SkillLab;
using GemTD.Gameplay.Towers;
using GemTD.UI;

namespace GemTD.Editor
{
    public static class SkillLabSceneBootstrap
    {
        const string ScenePath = "Assets/Scenes/SkillLab.unity";
        const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

        [MenuItem("Gem TD/Bootstrap Skill Lab Scene")]
        public static void Bootstrap()
        {
            var fireball = Load<TowerDefinition>("Assets/Data/Towers/Catalog/Spell/Tower_Fireball.asset");
            var dummyDef = Load<EnemyDefinition>("Assets/Data/Enemies/Enemy_Arrow.asset");
            var gems = new[]
            {
                Load<GemDefinition>("Assets/Data/Gems/Gem_LMP.asset"),
                Load<GemDefinition>("Assets/Data/Gems/Gem_Chain.asset"),
                Load<GemDefinition>("Assets/Data/Gems/Gem_Fork.asset"),
                Load<GemDefinition>("Assets/Data/Gems/Gem_IncreasedArea.asset"),
                Load<GemDefinition>("Assets/Data/Gems/Gem_Pierce.asset"),
                Load<GemDefinition>("Assets/Data/Gems/Gem_ElementalProliferation.asset"),
                Load<GemDefinition>("Assets/Data/Gems/Gem_Combustion.asset"),
                Load<GemDefinition>("Assets/Data/Gems/Gem_AddedFireDamage.asset"),
                Load<GemDefinition>("Assets/Data/Gems/Gem_AddedColdDamage.asset"),
                Load<GemDefinition>("Assets/Data/Gems/Gem_AddedLightningDamage.asset"),
                Load<GemDefinition>("Assets/Data/Gems/Gem_Knockback.asset")
            };

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            camGo.name = "Main Camera";
            var cam = camGo.GetComponent<Camera>();
            if (cam == null)
                cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 10f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
            if (camGo.GetComponent<AudioListener>() == null)
                camGo.AddComponent<AudioListener>();
            var camCtrl = camGo.GetComponent<RunCameraController>();
            if (camCtrl == null)
                camCtrl = camGo.AddComponent<RunCameraController>();
            var camSo = new SerializedObject(camCtrl);
            camSo.FindProperty("focus").vector3Value = Vector3.zero;
            camSo.FindProperty("distance").floatValue = 18f;
            camSo.ApplyModifiedPropertiesWithoutUndo();

            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            EnsureEventSystem();

            var towerGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            towerGo.name = "LabTower";
            towerGo.transform.position = DummyField.DefaultTowerPosition;
            towerGo.transform.localScale = new Vector3(0.8f, 1.6f, 0.8f);
            var towerRend = towerGo.GetComponent<Renderer>();
            Tint(towerRend, new Color(0.45f, 0.5f, 0.7f));

            var dummyViews = new SkillLabDummyView[DummyField.PinCount];
            var homes = new Vector3[DummyField.PinCount];
            DummyField.WriteHomes(homes);
            for (var i = 0; i < DummyField.PinCount; i++)
            {
                var dummyGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                dummyGo.name = "Dummy_" + i;
                dummyGo.transform.position = homes[i];
                dummyGo.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
                var dummyRend = dummyGo.GetComponent<Renderer>();
                Tint(dummyRend, new Color(0.85f, 0.75f, 0.35f));
                var view = dummyGo.AddComponent<SkillLabDummyView>();
                view.SetIndex(i);
                dummyViews[i] = view;
            }

            var root = new GameObject("SkillLab");
            var overlay = root.AddComponent<AttackOverlayView>();
            var controller = root.AddComponent<SkillLabController>();
            var ctrlSo = new SerializedObject(controller);
            ctrlSo.FindProperty("fireball").objectReferenceValue = fireball;
            ctrlSo.FindProperty("dummyDefinition").objectReferenceValue = dummyDef;
            ctrlSo.FindProperty("overlay").objectReferenceValue = overlay;
            ctrlSo.FindProperty("worldCamera").objectReferenceValue = cam;
            ctrlSo.FindProperty("towerView").objectReferenceValue = towerGo.transform;
            var gemsProp = ctrlSo.FindProperty("draftGems");
            gemsProp.arraySize = gems.Length;
            for (var i = 0; i < gems.Length; i++)
                gemsProp.GetArrayElementAtIndex(i).objectReferenceValue = gems[i];
            var viewsProp = ctrlSo.FindProperty("dummyViews");
            viewsProp.arraySize = dummyViews.Length;
            for (var i = 0; i < dummyViews.Length; i++)
                viewsProp.GetArrayElementAtIndex(i).objectReferenceValue = dummyViews[i];
            ctrlSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            if (fireball == null || dummyDef == null || gems[0] == null)
                Debug.LogError("[Gem TD] Skill Lab bootstrap: one or more data assets failed to load — Fire/gems will not work.");

            BuildHud(controller);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
            WireMainMenuSkillLabButton();
            AssetDatabase.Refresh();
            Debug.Log("[Gem TD] Bootstrapped Skill Lab at " + ScenePath + ". Main Menu Skill Lab button wired.");
        }

        static void BuildHud(SkillLabController lab)
        {
            var canvasGo = new GameObject("SkillLabHud");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panel.transform.SetParent(canvasGo.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 1f);
            panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot = new Vector2(0f, 1f);
            panelRt.anchoredPosition = new Vector2(24f, -24f);
            panelRt.sizeDelta = new Vector2(360f, 0f);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 0.92f);
            var vlg = panel.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var fireball = CreateButton(panel.transform, "FireballButton", "Fireball");
            var slot0 = CreateDropdown(panel.transform, "Dropdown_GemSlot0");
            var slot1 = CreateDropdown(panel.transform, "Dropdown_GemSlot1");
            var slot2 = CreateDropdown(panel.transform, "Dropdown_GemSlot2");
            var fire = CreateButton(panel.transform, "FireButton", "Fire");
            var clear = CreateButton(panel.transform, "ClearButton", "Clear");
            var reset = CreateButton(panel.transform, "ResetPinsButton", "Reset Pins");
            var back = CreateButton(panel.transform, "BackButton", "Back");
            var hydra = CreateLabel(panel.transform, "HydraLabel", "Hydra");
            var status = CreateLabel(panel.transform, "StatusLabel", "");
            var legend = CreateLabel(panel.transform, "LegendLabel", "White primary  Cyan hydra  Yellow pierce  Magenta fork  Orange chain  Red AoE");
            legend.fontSize = 16f;
            hydra.gameObject.SetActive(false);

            var hud = canvasGo.AddComponent<SkillLabHud>();
            var so = new SerializedObject(hud);
            so.FindProperty("lab").objectReferenceValue = lab;
            so.FindProperty("fireballButton").objectReferenceValue = fireball;
            so.FindProperty("fireButton").objectReferenceValue = fire;
            so.FindProperty("clearButton").objectReferenceValue = clear;
            so.FindProperty("resetPinsButton").objectReferenceValue = reset;
            so.FindProperty("backButton").objectReferenceValue = back;
            so.FindProperty("hydraLabel").objectReferenceValue = hydra;
            so.FindProperty("statusLabel").objectReferenceValue = status;
            so.FindProperty("legendLabel").objectReferenceValue = legend;
            var slots = so.FindProperty("gemSlotDropdowns");
            slots.arraySize = 3;
            slots.GetArrayElementAtIndex(0).objectReferenceValue = slot0;
            slots.GetArrayElementAtIndex(1).objectReferenceValue = slot1;
            slots.GetArrayElementAtIndex(2).objectReferenceValue = slot2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static TMP_Dropdown CreateDropdown(Transform parent, string name)
        {
            var go = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 40f;
            le.minHeight = 36f;
            var dropdown = go.GetComponent<TMP_Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(new System.Collections.Generic.List<string> { "Empty" });
            return dropdown;
        }

        static Button CreateButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 40f;
            go.GetComponent<LayoutElement>().minHeight = 36f;
            go.GetComponent<Image>().color = new Color(0.2f, 0.22f, 0.28f, 1f);
            var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var rt = textGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 22f;
            tmp.color = Color.white;
            return go.GetComponent<Button>();
        }

        static TextMeshProUGUI CreateLabel(Transform parent, string name, string text)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 28f;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20f;
            tmp.color = Color.white;
            return tmp;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        static void AddSceneToBuild(string path)
        {
            var scenes = EditorBuildSettings.scenes;
            for (var i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == path)
                    return;
            }

            var next = new EditorBuildSettingsScene[scenes.Length + 1];
            for (var i = 0; i < scenes.Length; i++)
                next[i] = scenes[i];
            next[scenes.Length] = new EditorBuildSettingsScene(path, true);
            EditorBuildSettings.scenes = next;
        }

        static void WireMainMenuSkillLabButton()
        {
            var menu = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Additive);
            MainMenuController controller = null;
            var roots = menu.GetRootGameObjects();
            for (var r = 0; r < roots.Length; r++)
            {
                controller = roots[r].GetComponentInChildren<MainMenuController>(true);
                if (controller != null)
                    break;
            }

            if (controller == null)
            {
                Debug.LogError("[Gem TD] MainMenuController not found; Skill Lab button not wired.");
                EditorSceneManager.CloseScene(menu, true);
                return;
            }

            var so = new SerializedObject(controller);
            var existing = so.FindProperty("skillLabButton").objectReferenceValue as Button;
            if (existing == null)
            {
                var play = so.FindProperty("playButton").objectReferenceValue as Button;
                var settings = so.FindProperty("settingsButton").objectReferenceValue as Button;
                if (play == null)
                {
                    Debug.LogError("[Gem TD] Play button missing; cannot clone Skill Lab button.");
                    EditorSceneManager.CloseScene(menu, true);
                    return;
                }

                var clone = Object.Instantiate(play.gameObject, play.transform.parent);
                clone.name = "SkillLabButton";
                var label = clone.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = "Skill Lab";
                if (settings != null)
                    clone.transform.SetSiblingIndex(settings.transform.GetSiblingIndex() + 1);
                so.FindProperty("skillLabButton").objectReferenceValue = clone.GetComponent<Button>();
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.SaveScene(menu);
            EditorSceneManager.CloseScene(menu, true);
        }

        static void Tint(Renderer rend, Color color)
        {
            if (rend == null || rend.sharedMaterial == null)
                return;
            var mat = new Material(rend.sharedMaterial);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
            rend.sharedMaterial = mat;
        }

        static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogError("[Gem TD] Missing asset: " + path);
            return asset;
        }
    }
}
