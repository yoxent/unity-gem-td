using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using GemTD.Gameplay;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Grid;
using GemTD.Gameplay.Map;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;
using GemTD.UI;

namespace GemTD.Editor
{
    /// <summary>
    /// Creates Phase 2 PR1 ScriptableObjects, greybox prefabs, HUD, and wires Run.unity.
    /// Menu: Gem TD / Phase 2 PR1 Bootstrap Assets + Scene
    /// </summary>
    public static class Phase2Pr1Bootstrap
    {
        const string RunScenePath = "Assets/Scenes/Run.unity";
        const string PrefabFolder = "Assets/Prefabs/Phase2";
        const string MatFolder = "Assets/Art/Placeholders";

        [MenuItem("Gem TD/Phase 2 PR1 Bootstrap Assets + Scene")]
        public static void Bootstrap()
        {
            GemTdEditorMenus.EnsureDataFolders();
            GemTdEditorMenus.EnsurePlaceholderMaterials();
            EnsureFolder(PrefabFolder);

            var gemLmp = CreateOrLoadGem("Assets/Data/Gems/Gem_LMP.asset", GemId.Lmp, "Lesser Multiple Projectiles");
            var gemChain = CreateOrLoadGem("Assets/Data/Gems/Gem_Chain.asset", GemId.Chain, "Chain");
            var ballista = CreateOrLoadTower("Assets/Data/Towers/Tower_Ballista.asset");
            var runner = CreateOrLoadEnemy("Assets/Data/Enemies/Enemy_Runner.asset");
            var wave1 = CreateOrLoadWave("Assets/Data/Waves/Wave_01.asset", 1, runner, 4, 0.6f);
            var wave2 = CreateOrLoadWave("Assets/Data/Waves/Wave_02.asset", 2, runner, 8, 0.45f);
            var wave3 = CreateOrLoadWave("Assets/Data/Waves/Wave_03.asset", 3, runner, 12, 0.35f);
            var runConfig = CreateOrLoadRunConfig("Assets/Data/RunConfig_Default.asset", gemLmp, gemChain);

            var enemyPrefab = CreateEnemyPrefab();
            var projectilePrefab = CreateProjectilePrefab();
            var towerPrefab = CreateTowerPrefab();
            var expandPrefab = CreateExpandMarkerPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Reload after import so scene refs serialize with guids.
            runConfig = AssetDatabase.LoadAssetAtPath<RunConfig>("Assets/Data/RunConfig_Default.asset");
            ballista = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/Data/Towers/Tower_Ballista.asset");
            wave1 = AssetDatabase.LoadAssetAtPath<WaveDefinition>("Assets/Data/Waves/Wave_01.asset");
            wave2 = AssetDatabase.LoadAssetAtPath<WaveDefinition>("Assets/Data/Waves/Wave_02.asset");
            wave3 = AssetDatabase.LoadAssetAtPath<WaveDefinition>("Assets/Data/Waves/Wave_03.asset");

            var scene = EditorSceneManager.OpenScene(RunScenePath, OpenSceneMode.Single);
            var rootGo = GameObject.Find("GameCompositionRoot");
            if (rootGo == null)
            {
                Debug.LogError("[Gem TD] GameCompositionRoot missing in Run.unity — run Gem TD/Bootstrap Run Scene first.");
                return;
            }

            var root = rootGo.GetComponent<GameCompositionRoot>();
            if (root == null)
                root = rootGo.AddComponent<GameCompositionRoot>();

            var gridView = Object.FindFirstObjectByType<GridBoardView>();
            if (gridView == null)
            {
                Debug.LogError("[Gem TD] GridBoardView missing in Run.unity.");
                return;
            }

            var pathMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatFolder}/Enemy_Sphere_Yellow.mat");
            var soGrid = new SerializedObject(gridView);
            var pathMatProp = soGrid.FindProperty("pathMaterial");
            if (pathMatProp != null && pathMat != null)
                pathMatProp.objectReferenceValue = pathMat;
            soGrid.ApplyModifiedPropertiesWithoutUndo();

            var input = rootGo.GetComponent<RunInputController>();
            if (input == null)
                input = rootGo.AddComponent<RunInputController>();

            var poolRoot = rootGo.transform.Find("PoolRoot");
            if (poolRoot == null)
            {
                var poolGo = new GameObject("PoolRoot");
                poolGo.transform.SetParent(rootGo.transform, false);
                poolRoot = poolGo.transform;
            }

            var so = new SerializedObject(root);
            so.FindProperty("runConfig").objectReferenceValue = runConfig;
            so.FindProperty("ballistaDef").objectReferenceValue = ballista;
            var wavesProp = so.FindProperty("waves");
            wavesProp.arraySize = 3;
            wavesProp.GetArrayElementAtIndex(0).objectReferenceValue = wave1;
            wavesProp.GetArrayElementAtIndex(1).objectReferenceValue = wave2;
            wavesProp.GetArrayElementAtIndex(2).objectReferenceValue = wave3;
            so.FindProperty("gridView").objectReferenceValue = gridView;
            so.FindProperty("inputController").objectReferenceValue = input;
            so.FindProperty("poolRoot").objectReferenceValue = poolRoot;
            so.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab.GetComponent<EnemyView>();
            so.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab.GetComponent<ProjectileView>();
            so.FindProperty("towerPrefab").objectReferenceValue = towerPrefab.GetComponent<TowerView>();
            so.FindProperty("expandMarkerPrefab").objectReferenceValue = expandPrefab.GetComponent<ExpandMarkerView>();
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(root);

            var inputSo = new SerializedObject(input);
            var camProp = inputSo.FindProperty("worldCamera");
            if (camProp != null)
                camProp.objectReferenceValue = Camera.main;
            inputSo.ApplyModifiedPropertiesWithoutUndo();

            EnsureHudCanvas();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Gem TD] Phase 2 PR1 bootstrap complete — SOs, prefabs, HUD, and Run.unity wired.");
        }

        static RunConfig CreateOrLoadRunConfig(string path, GemDefinition lmp, GemDefinition chain)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<RunConfig>(path);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<RunConfig>();
                AssetDatabase.CreateAsset(cfg, path);
            }

            cfg.StartingGold = 100;
            cfg.StartingLives = 20;
            cfg.EndWaveGold = 25;
            cfg.SeedGems = new[] { lmp, chain };
            EditorUtility.SetDirty(cfg);
            return cfg;
        }

        static GemDefinition CreateOrLoadGem(string path, GemId id, string display)
        {
            var gem = AssetDatabase.LoadAssetAtPath<GemDefinition>(path);
            if (gem == null)
            {
                gem = ScriptableObject.CreateInstance<GemDefinition>();
                AssetDatabase.CreateAsset(gem, path);
            }

            gem.Id = id;
            gem.DisplayName = display;
            EditorUtility.SetDirty(gem);
            return gem;
        }

        static TowerDefinition CreateOrLoadTower(string path)
        {
            var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>(path);
            if (tower == null)
            {
                tower = ScriptableObject.CreateInstance<TowerDefinition>();
                AssetDatabase.CreateAsset(tower, path);
            }

            tower.DisplayName = "Ballista";
            tower.Cost = 50;
            tower.Range = 5f;
            tower.Damage = 10f;
            tower.AttackInterval = 1f;
            tower.SocketCount = 2;
            EditorUtility.SetDirty(tower);
            return tower;
        }

        static EnemyDefinition CreateOrLoadEnemy(string path)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (enemy == null)
            {
                enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
                AssetDatabase.CreateAsset(enemy, path);
            }

            enemy.DisplayName = "Runner";
            enemy.MaxHealth = 20f;
            enemy.MoveSpeed = 2f;
            enemy.Armor = 0;
            enemy.KillGold = 5;
            enemy.PlaceholderMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MatFolder}/Enemy_Sphere_Yellow.mat");
            EditorUtility.SetDirty(enemy);
            return enemy;
        }

        static WaveDefinition CreateOrLoadWave(string path, int number, EnemyDefinition enemy, int count, float interval)
        {
            var wave = AssetDatabase.LoadAssetAtPath<WaveDefinition>(path);
            if (wave == null)
            {
                wave = ScriptableObject.CreateInstance<WaveDefinition>();
                AssetDatabase.CreateAsset(wave, path);
            }

            wave.WaveNumber = number;
            wave.Enemy = enemy;
            wave.Count = count;
            wave.SpawnInterval = interval;
            EditorUtility.SetDirty(wave);
            return wave;
        }

        static GameObject CreateEnemyPrefab()
        {
            const string path = PrefabFolder + "/Enemy_Runner.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Enemy_Runner";
            go.transform.localScale = Vector3.one * 0.7f;
            var col = go.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
            go.AddComponent<EnemyView>();
            ApplyMat(go, $"{MatFolder}/Enemy_Sphere_Yellow.mat");
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject CreateProjectilePrefab()
        {
            const string path = PrefabFolder + "/Projectile_Bolt.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Projectile_Bolt";
            go.transform.localScale = new Vector3(0.15f, 0.15f, 0.4f);
            var col = go.GetComponent<Collider>();
            if (col != null)
                Object.DestroyImmediate(col);
            go.AddComponent<ProjectileView>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject CreateTowerPrefab()
        {
            const string path = PrefabFolder + "/Tower_Ballista.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Tower_Ballista";
            go.transform.localScale = new Vector3(0.7f, 1.1f, 0.7f);
            go.AddComponent<TowerView>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject CreateExpandMarkerPrefab()
        {
            const string path = PrefabFolder + "/ExpandMarker.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "ExpandMarker";
            go.transform.localScale = new Vector3(0.55f, 0.08f, 0.55f);
            go.AddComponent<ExpandMarkerView>();
            ApplyMat(go, $"{MatFolder}/Enemy_Pyramid_Teal.mat");
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void EnsureHudCanvas()
        {
            var canvasGo = GameObject.Find("RunHudCanvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("RunHudCanvas");
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            var hud = canvasGo.GetComponent<RunHudView>();
            if (hud == null)
                hud = canvasGo.AddComponent<RunHudView>();

            var panel = canvasGo.transform.Find("Panel");
            if (panel == null)
            {
                var panelGo = new GameObject("Panel", typeof(RectTransform));
                panelGo.transform.SetParent(canvasGo.transform, false);
                var rt = panelGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(12f, -12f);
                rt.sizeDelta = new Vector2(360f, 220f);
                panel = panelGo.transform;
            }

            var lives = EnsureText(panel, "LivesText", "Lives: 20", new Vector2(0f, -0f));
            var gold = EnsureText(panel, "GoldText", "Gold: 100", new Vector2(0f, -24f));
            var wave = EnsureText(panel, "WaveText", "Wave: 0", new Vector2(0f, -48f));
            var state = EnsureText(panel, "StateText", "State: Boot", new Vector2(0f, -72f));
            var defeat = EnsureText(panel, "DefeatText", "Defeat — restart Play Mode", new Vector2(0f, -160f));
            defeat.gameObject.SetActive(false);
            defeat.color = new Color(0.88f, 0.35f, 0.35f);

            var startBtn = EnsureButton(panel, "StartWaveButton", "Start Wave", new Vector2(0f, -100f));
            var sellBtn = EnsureButton(panel, "SellButton", "Sell", new Vector2(120f, -100f));
            var lmpBtn = EnsureButton(panel, "SocketLmpButton", "Socket LMP", new Vector2(0f, -132f));
            var chainBtn = EnsureButton(panel, "SocketChainButton", "Socket Chain", new Vector2(120f, -132f));

            var confirm = EnsureSellConfirmPanel(canvasGo.transform);

            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("livesText").objectReferenceValue = lives;
            hudSo.FindProperty("goldText").objectReferenceValue = gold;
            hudSo.FindProperty("waveText").objectReferenceValue = wave;
            hudSo.FindProperty("stateText").objectReferenceValue = state;
            hudSo.FindProperty("defeatText").objectReferenceValue = defeat;
            hudSo.FindProperty("startWaveButton").objectReferenceValue = startBtn;
            hudSo.FindProperty("sellButton").objectReferenceValue = sellBtn;
            hudSo.FindProperty("socketLmpButton").objectReferenceValue = lmpBtn;
            hudSo.FindProperty("socketChainButton").objectReferenceValue = chainBtn;
            hudSo.FindProperty("sellConfirmPanel").objectReferenceValue = confirm.panel;
            hudSo.FindProperty("sellConfirmMessage").objectReferenceValue = confirm.message;
            hudSo.FindProperty("sellConfirmDontShowAgain").objectReferenceValue = confirm.toggle;
            hudSo.FindProperty("sellConfirmYesButton").objectReferenceValue = confirm.yes;
            hudSo.FindProperty("sellConfirmNoButton").objectReferenceValue = confirm.no;
            hudSo.ApplyModifiedPropertiesWithoutUndo();
        }

        struct SellConfirmRefs
        {
            public GameObject panel;
            public Text message;
            public Toggle toggle;
            public Button yes;
            public Button no;
        }

        static SellConfirmRefs EnsureSellConfirmPanel(Transform canvas)
        {
            var existing = canvas.Find("SellConfirmPanel");
            GameObject panelGo;
            if (existing == null)
            {
                panelGo = new GameObject("SellConfirmPanel", typeof(RectTransform), typeof(Image));
                panelGo.transform.SetParent(canvas, false);
            }
            else
            {
                panelGo = existing.gameObject;
            }

            var prt = panelGo.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(420f, 200f);
            var bg = panelGo.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            var message = EnsureCenteredText(
                panelGo.transform,
                "Message",
                "This tower has a socketed gem.\nSelling will unsocket the gem back to your inventory.",
                new Vector2(0f, 40f),
                new Vector2(380f, 70f));

            var toggle = EnsureToggle(
                panelGo.transform,
                "DontShowAgain",
                "Do not show this popup again",
                new Vector2(0f, -20f));

            var yes = EnsureButton(panelGo.transform, "ConfirmSellButton", "Sell anyway", new Vector2(-70f, -70f));
            var no = EnsureButton(panelGo.transform, "CancelSellButton", "Cancel", new Vector2(70f, -70f));

            // Centered anchors for confirm buttons (EnsureButton uses top-left).
            CenterButton(yes.GetComponent<RectTransform>(), new Vector2(-70f, -70f));
            CenterButton(no.GetComponent<RectTransform>(), new Vector2(70f, -70f));

            panelGo.SetActive(false);
            return new SellConfirmRefs
            {
                panel = panelGo,
                message = message,
                toggle = toggle,
                yes = yes,
                no = no
            };
        }

        static void CenterButton(RectTransform rt, Vector2 anchored)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(120f, 28f);
        }

        static Text EnsureCenteredText(Transform parent, string name, string value, Vector2 anchored, Vector2 size)
        {
            var t = parent.Find(name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Text));
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = t.gameObject;
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = size;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 15;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        static Toggle EnsureToggle(Transform parent, string name, string label, Vector2 anchored)
        {
            var t = parent.Find(name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
                go.transform.SetParent(parent, false);

                var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bgGo.transform.SetParent(go.transform, false);
                var bgRt = bgGo.GetComponent<RectTransform>();
                bgRt.anchorMin = new Vector2(0f, 0.5f);
                bgRt.anchorMax = new Vector2(0f, 0.5f);
                bgRt.pivot = new Vector2(0f, 0.5f);
                bgRt.anchoredPosition = Vector2.zero;
                bgRt.sizeDelta = new Vector2(20f, 20f);
                bgGo.GetComponent<Image>().color = Color.white;

                var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
                checkGo.transform.SetParent(bgGo.transform, false);
                var checkRt = checkGo.GetComponent<RectTransform>();
                checkRt.anchorMin = Vector2.zero;
                checkRt.anchorMax = Vector2.one;
                checkRt.offsetMin = new Vector2(3f, 3f);
                checkRt.offsetMax = new Vector2(-3f, -3f);
                checkGo.GetComponent<Image>().color = new Color(0.2f, 0.55f, 0.3f);

                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(go.transform, false);
                var lrt = labelGo.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0f, 0.5f);
                lrt.anchorMax = new Vector2(1f, 0.5f);
                lrt.pivot = new Vector2(0f, 0.5f);
                lrt.anchoredPosition = new Vector2(28f, 0f);
                lrt.sizeDelta = new Vector2(-28f, 22f);
                var lt = labelGo.GetComponent<Text>();
                lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (lt.font == null)
                    lt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                lt.fontSize = 13;
                lt.color = new Color(0.85f, 0.88f, 0.92f);
                lt.alignment = TextAnchor.MiddleLeft;
                lt.text = label;

                var toggleNew = go.GetComponent<Toggle>();
                toggleNew.targetGraphic = bgGo.GetComponent<Image>();
                toggleNew.graphic = checkGo.GetComponent<Image>();
                toggleNew.isOn = false;
            }
            else
            {
                go = t.gameObject;
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(360f, 24f);
            return go.GetComponent<Toggle>();
        }

        static Text EnsureText(Transform parent, string name, string value, Vector2 anchored)
        {
            var t = parent.Find(name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Text));
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = t.gameObject;
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(340f, 22f);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        static Button EnsureButton(Transform parent, string name, string label, Vector2 anchored)
        {
            var t = parent.Find(name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(go.transform, false);
                var lrt = labelGo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
                var lt = labelGo.GetComponent<Text>();
                lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (lt.font == null)
                    lt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                lt.alignment = TextAnchor.MiddleCenter;
                lt.fontSize = 14;
                lt.color = Color.black;
                lt.text = label;
            }
            else
            {
                go = t.gameObject;
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(110f, 28f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.85f, 0.85f, 0.88f);
            return go.GetComponent<Button>();
        }

        static void ApplyMat(GameObject go, string matPath)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            var r = go.GetComponent<MeshRenderer>();
            if (r != null && mat != null)
                r.sharedMaterial = mat;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parts = path.Split('/');
            var cur = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{cur}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
