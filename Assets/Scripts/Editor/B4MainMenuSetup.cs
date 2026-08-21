using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;
using GemTD.UI;

namespace GemTD.Editor
{
    /// <summary>One-shot setup for PR6 B-4: Settings prefab, Run wiring, Main Menu scene, build order.</summary>
    public static class B4MainMenuSetup
    {
        const string RunScenePath = "Assets/Scenes/Run.unity";
        const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        const string SettingsPrefabPath = "Assets/Prefabs/UI/Draft/SettingsPanel.prefab";

        static readonly Color BgPanel = new Color(0.102f, 0.122f, 0.149f, 1f);
        static readonly Color BgPanel2 = new Color(0.141f, 0.169f, 0.2f, 1f);
        static readonly Color TextPrimary = new Color(0.91f, 0.933f, 0.957f, 1f);
        static readonly Color Dimmer = new Color(0f, 0f, 0f, 0.55f);

        [MenuItem("Gem TD/Phase 2 PR6 B-4 Setup Main Menu + Settings")]
        public static void Setup()
        {
            var settingsPrefab = EnsureSettingsPrefab();
            if (settingsPrefab == null) return;

            if (!WireRunScene(settingsPrefab)) return;
            if (!EnsureMainMenuScene(settingsPrefab)) return;
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[B-4 Setup] Main Menu + Settings ready. Build order: MainMenu → Run.");
        }

        static GameObject EnsureSettingsPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPrefabPath);
            if (existing != null)
            {
                Debug.Log("[B-4 Setup] Reusing existing SettingsPanel.prefab.");
                return existing;
            }

            var root = new GameObject("SettingsPanel", typeof(RectTransform), typeof(SettingsController));
            var rootRt = root.GetComponent<RectTransform>();
            StretchFull(rootRt);

            var dimmer = CreateUiObject("Root", root.transform);
            StretchFull(dimmer.GetComponent<RectTransform>());
            var dimmerImage = dimmer.AddComponent<Image>();
            dimmerImage.color = Dimmer;
            dimmerImage.raycastTarget = true;

            var panel = CreateUiObject("Panel", dimmer.transform);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(480f, 360f);
            panelRt.anchoredPosition = Vector2.zero;
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = BgPanel;
            panelImage.raycastTarget = true;
            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 24, 24);
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            panel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var title = CreateTmp("Title", panel.transform, 28, FontStyles.Bold, "Settings");
            var titleLe = title.gameObject.AddComponent<LayoutElement>();
            titleLe.preferredHeight = 40f;

            var volumeRow = CreateUiObject("VolumeRow", panel.transform);
            volumeRow.AddComponent<HorizontalLayoutGroup>().spacing = 12f;
            var rowLe = volumeRow.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 36f;
            var rowHlg = volumeRow.GetComponent<HorizontalLayoutGroup>();
            rowHlg.childAlignment = TextAnchor.MiddleCenter;
            rowHlg.childControlWidth = true;
            rowHlg.childControlHeight = true;
            rowHlg.childForceExpandWidth = false;
            rowHlg.childForceExpandHeight = false;

            var volumeLabel = CreateTmp("VolumeLabel", volumeRow.transform, 18, FontStyles.Normal, "Master volume");
            volumeLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 160f;

            var sliderGo = CreateUiObject("MasterSlider", volumeRow.transform);
            sliderGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var sliderBg = sliderGo.AddComponent<Image>();
            sliderBg.color = BgPanel2;
            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.wholeNumbers = false;

            var fillArea = CreateUiObject("Fill Area", sliderGo.transform);
            StretchFull(fillArea.GetComponent<RectTransform>());
            var fill = CreateUiObject("Fill", fillArea.transform);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(5f, 5f);
            fillRt.offsetMax = new Vector2(-5f, -5f);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.878f, 0.706f, 0.294f, 1f);
            slider.fillRect = fillRt;

            var handleArea = CreateUiObject("Handle Slide Area", sliderGo.transform);
            StretchFull(handleArea.GetComponent<RectTransform>());
            var handle = CreateUiObject("Handle", handleArea.transform);
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(20f, 20f);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = TextPrimary;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImage;

            var valueLabel = CreateTmp("MasterValueLabel", volumeRow.transform, 18, FontStyles.Normal, "100%");
            valueLabel.alignment = TextAlignmentOptions.MidlineRight;
            valueLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 56f;

            var closeButton = CreateButton("CloseButton", panel.transform, "Close");
            var quitButton = CreateButton("QuitToMenuButton", panel.transform, "Quit to Main Menu");

            var controller = root.GetComponent<SettingsController>();
            var so = new SerializedObject(controller);
            so.FindProperty("rootPanel").objectReferenceValue = dimmer;
            so.FindProperty("masterSlider").objectReferenceValue = slider;
            so.FindProperty("masterValueLabel").objectReferenceValue = valueLabel;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("quitToMenuButton").objectReferenceValue = quitButton;
            so.FindProperty("showQuitToMenu").boolValue = false;
            so.FindProperty("popup").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();

            dimmer.SetActive(false);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, SettingsPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static bool WireRunScene(GameObject settingsPrefab)
        {
            var scene = EditorSceneManager.OpenScene(RunScenePath);
            var binder = Object.FindFirstObjectByType<RunHudBinder>();
            if (binder == null)
            {
                Debug.LogError("[B-4 Setup] RunHudBinder not found in Run.unity.");
                return false;
            }

            var canvas = binder.GetComponent<RectTransform>();
            if (canvas == null)
            {
                Debug.LogError("[B-4 Setup] RunHudBinder has no RectTransform.");
                return false;
            }

            var margin = canvas.Find("Margin");
            var parent = margin != null ? margin : canvas.transform;

            SettingsController settings = null;
            foreach (Transform child in parent)
            {
                settings = child.GetComponent<SettingsController>();
                if (settings != null) break;
            }

            if (settings == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab, parent);
                instance.name = "SettingsPanel";
                StretchFull(instance.GetComponent<RectTransform>());
                settings = instance.GetComponent<SettingsController>();
            }

            var popup = binder.GetComponentInChildren<PopupManager>(true);
            if (popup == null)
            {
                Debug.LogError("[B-4 Setup] PopupManager not found under RunHudCanvas.");
                return false;
            }

            var settingsSo = new SerializedObject(settings);
            settingsSo.FindProperty("showQuitToMenu").boolValue = true;
            settingsSo.FindProperty("popup").objectReferenceValue = popup;
            settingsSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);

            var binderSo = new SerializedObject(binder);
            binderSo.FindProperty("settings").objectReferenceValue = settings;
            binderSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(binder);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError("[B-4 Setup] Failed to save Run.unity.");
                return false;
            }

            Debug.Log("[B-4 Setup] Run.unity wired (SettingsPanel + RunHudBinder.settings).");
            return true;
        }

        static bool EnsureMainMenuScene(GameObject settingsPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>() ?? camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.102f, 0.122f, 0.149f, 1f);
            if (camGo.GetComponent<AudioListener>() == null)
                camGo.AddComponent<AudioListener>();

            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<InputSystemUIInputModule>();
            }

            var canvasGo = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            StretchFull(canvasGo.GetComponent<RectTransform>());

            var menuRoot = CreateUiObject("MenuRoot", canvasGo.transform);
            var menuRt = menuRoot.GetComponent<RectTransform>();
            menuRt.anchorMin = new Vector2(0.5f, 0.5f);
            menuRt.anchorMax = new Vector2(0.5f, 0.5f);
            menuRt.pivot = new Vector2(0.5f, 0.5f);
            menuRt.sizeDelta = new Vector2(320f, 420f);
            menuRt.anchoredPosition = Vector2.zero;
            var menuVlg = menuRoot.AddComponent<VerticalLayoutGroup>();
            menuVlg.spacing = 16f;
            menuVlg.childAlignment = TextAnchor.MiddleCenter;
            menuVlg.childControlWidth = true;
            menuVlg.childControlHeight = true;
            menuVlg.childForceExpandWidth = true;
            menuVlg.childForceExpandHeight = false;

            var title = CreateTmp("Title", menuRoot.transform, 48, FontStyles.Bold, "Gem TD");
            title.color = new Color(0.878f, 0.706f, 0.294f, 1f);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;

            var playButton = CreateButton("PlayButton", menuRoot.transform, "Play");
            var settingsButton = CreateButton("SettingsButton", menuRoot.transform, "Settings");
            var quitButton = CreateButton("QuitButton", menuRoot.transform, "Quit");

            var settingsInstance = (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab, canvasGo.transform);
            settingsInstance.name = "SettingsPanel";
            StretchFull(settingsInstance.GetComponent<RectTransform>());
            var settingsController = settingsInstance.GetComponent<SettingsController>();

            var menuController = menuRoot.AddComponent<MainMenuController>();
            var menuSo = new SerializedObject(menuController);
            menuSo.FindProperty("playButton").objectReferenceValue = playButton;
            menuSo.FindProperty("settingsButton").objectReferenceValue = settingsButton;
            menuSo.FindProperty("quitButton").objectReferenceValue = quitButton;
            menuSo.FindProperty("settings").objectReferenceValue = settingsController;
            menuSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            Debug.Log("[B-4 Setup] MainMenu.unity created.");
            return true;
        }

        static void UpdateBuildSettings()
        {
            var mainMenu = new EditorBuildSettingsScene(MainMenuScenePath, true);
            var run = new EditorBuildSettingsScene(RunScenePath, true);
            var sample = new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", false);

            EditorBuildSettings.scenes = new[] { mainMenu, run, sample };
            Debug.Log("[B-4 Setup] Build settings: MainMenu (0), Run (1), SampleScene disabled.");
        }

        static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        static TMP_Text CreateTmp(string name, Transform parent, float fontSize, FontStyles style, string text)
        {
            var go = CreateUiObject(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        static Button CreateButton(string name, Transform parent, string label)
        {
            var go = CreateUiObject(name, parent);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 44f;
            le.minHeight = 44f;
            var image = go.AddComponent<Image>();
            image.color = BgPanel2;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var labelTmp = CreateTmp("Label", go.transform, 20, FontStyles.Normal, label);
            StretchFull(labelTmp.rectTransform);
            return button;
        }
    }
}
