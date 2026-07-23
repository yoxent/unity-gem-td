using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GemTD.Gameplay;
using GemTD.Gameplay.CameraControl;
using GemTD.Gameplay.Grid;
using GemTD.Gameplay.Run;

namespace GemTD.Editor
{
    public static class GemTdEditorMenus
    {
        const string RunScenePath = "Assets/Scenes/Run.unity";
        const string PlaceholderMatFolder = "Assets/Art/Placeholders";
        const string KenneyFolder = "Assets/Art/Kenney";

        [MenuItem("Gem TD/Create Data Folders")]
        public static void EnsureDataFolders()
        {
            Ensure("Assets/Data/Towers");
            Ensure("Assets/Data/Gems");
            Ensure("Assets/Data/Enemies");
            Ensure("Assets/Data/Waves");
            Ensure("Assets/Data/Evolutions");
            AssetDatabase.Refresh();
            Debug.Log("[Gem TD] Data folders ready.");
        }

        [MenuItem("Gem TD/Create Placeholder Enemy Materials")]
        public static void EnsurePlaceholderMaterials()
        {
            Ensure(PlaceholderMatFolder);
            CreateLitMat("Enemy_Sphere_Yellow", new Color(0.95f, 0.85f, 0.2f));
            CreateLitMat("Enemy_Cube_Blue", new Color(0.25f, 0.45f, 0.9f));
            CreateLitMat("Enemy_Pyramid_Teal", new Color(0.2f, 0.75f, 0.7f));
            CreateLitMat("Enemy_Diamond_Purple", new Color(0.55f, 0.3f, 0.85f));
            CreateLitMat("Grid_Tile", new Color(0.22f, 0.26f, 0.32f));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Gem TD] Placeholder materials ready.");
        }

        [MenuItem("Gem TD/Bootstrap Run Scene")]
        public static void BootstrapRunScene()
        {
            EnsureDataFolders();
            EnsurePlaceholderMaterials();
            Ensure(KenneyFolder);
            EnsureKenneyReadme();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            camGo.name = "Main Camera";
            var cam = camGo.GetComponent<Camera>();
            if (cam == null) cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
            // Isometric pose — controller ApplyPose runs on Awake; seed transform for edit-mode preview.
            var pitch = 40f;
            var yaw = 45f;
            var distance = 22f;
            var focus = new Vector3(4f, 0f, 4f);
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            camGo.transform.rotation = rot;
            camGo.transform.position = focus - rot * Vector3.forward * distance;
            if (camGo.GetComponent<AudioListener>() == null)
                camGo.AddComponent<AudioListener>();
            if (camGo.GetComponent<RunCameraController>() == null)
                camGo.AddComponent<RunCameraController>();

            var root = new GameObject("GameCompositionRoot");
            root.AddComponent<GameCompositionRoot>();
            root.AddComponent<RunStateDebugHud>();

            var gridGo = new GameObject("GridRoot");
            var gridView = gridGo.AddComponent<GridBoardView>();
            var tileMat = AssetDatabase.LoadAssetAtPath<Material>($"{PlaceholderMatFolder}/Grid_Tile.mat");
            if (tileMat != null)
            {
                var so = new SerializedObject(gridView);
                so.FindProperty("tileMaterial").objectReferenceValue = tileMat;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            EditorSceneManager.SaveScene(scene, RunScenePath);
            AddRunSceneToBuild();
            AssetDatabase.Refresh();
            Debug.Log($"[Gem TD] Bootstrapped {RunScenePath}. Press Play, then Space/F5 to cycle Expand→Build→Combat.");
        }

        static void AddRunSceneToBuild()
        {
            var scenes = EditorBuildSettings.scenes;
            for (var i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == RunScenePath)
                    return;
            }

            var next = new EditorBuildSettingsScene[scenes.Length + 1];
            for (var i = 0; i < scenes.Length; i++)
                next[i] = scenes[i];
            next[scenes.Length] = new EditorBuildSettingsScene(RunScenePath, true);
            EditorBuildSettings.scenes = next;
        }

        static void CreateLitMat(string name, Color color)
        {
            var path = $"{PlaceholderMatFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                if (existing.HasProperty("_BaseColor"))
                    existing.SetColor("_BaseColor", color);
                else if (existing.HasProperty("_Color"))
                    existing.SetColor("_Color", color);
                EditorUtility.SetDirty(existing);
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("HDRP/Lit")
                         ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = name };
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            AssetDatabase.CreateAsset(mat, path);
        }

        static void EnsureKenneyReadme()
        {
            var path = $"{KenneyFolder}/README.md";
            if (System.IO.File.Exists(path)) return;
            System.IO.File.WriteAllText(path,
                "# Kenney assets\n\n" +
                "Drop Kenney terrain / tower / UI packs here (CC0).\n" +
                "Phase 1 uses Unity primitives + `Assets/Art/Placeholders` until packs are imported.\n");
            AssetDatabase.ImportAsset(path);
        }

        static void Ensure(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            {
                if (!AssetDatabase.IsValidFolder(parent))
                    Ensure(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
