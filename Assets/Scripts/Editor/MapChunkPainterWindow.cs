using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GemTD.Gameplay.Map;

namespace GemTD.Editor
{
    public sealed class MapChunkPainterWindow : EditorWindow
    {
        const int Size = ChunkMask.Size;
        const string ChunksFolder = "Assets/Prefabs/Map/Chunks";
        static readonly string[] ToolLabels = { "Path", "Homebase", "Height lock" };

        enum PaintTool
        {
            Path = 0,
            Home = 1,
            ElevationLock = 2
        }

        const string DefaultCatalogPath = "Assets/Data/Map/ChunkCatalog.asset";
        static readonly ChunkType[] GeneratableTypes =
        {
            ChunkType.DeadEnd,
            ChunkType.Straight,
            ChunkType.Corner,
            ChunkType.TJunction,
            ChunkType.Cross,
            ChunkType.Homebase
        };
        static readonly string[] GeneratableLabels =
        {
            "DeadEnd", "Straight", "Corner", "TJunction", "Cross", "Homebase"
        };

        bool[] _cells = new bool[ChunkMask.CellCount];
        bool[] _elevationLocked = new bool[ChunkMask.CellCount];
        int _homeIndex = -1;
        [SerializeField] PaintTool _tool = PaintTool.Path;
        Vector2 _scroll;
        string _chunkName = "chunk_new";
        Material _pathMat;
        Material _towerMat;
        Material _homeMat;
        Material _lockMat;
        float _cellSize = 1f;
        MapChunkStamp _loaded;
        int _genSeed;
        int _lastGenSeed;
        [SerializeField] ChunkCatalog _chunkIndex;
        [SerializeField] ChunkTypeCatalog _compareCatalog;
        [SerializeField] ChunkType _generateType = ChunkType.Corner;
        ChunkType _lastGridType = ChunkType.Land;
        readonly Dictionary<string, string> _savedCanonical = new Dictionary<string, string>();
        bool _indexDirty = true;

        [MenuItem("Gem TD/Map Chunk Painter")]
        public static void Open()
        {
            var w = GetWindow<MapChunkPainterWindow>("Map Chunk Painter");
            w.minSize = new Vector2(360f, 520f);
        }

        void OnGUI()
        {
            EnsureFolder();
            EnsureMaterials();
            EnsureCellBuffers();
            EnsureDefaultIndex();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            GUILayout.Label($"Paint a {Size}x{Size} chunk.", EditorStyles.boldLabel);
            GUILayout.Space(6);
            GUILayout.Label("Paint mask", EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            var next = (PaintTool)GUILayout.Toolbar((int)_tool, ToolLabels, GUILayout.Height(32));
            if (EditorGUI.EndChangeCheck())
                _tool = next;
            EditorGUILayout.HelpBox(ActiveToolStatus(), MessageType.Info);

            DrawGrid();
            SyncGenerateTypeFromGrid();

            GUILayout.Space(8);
            _chunkName = EditorGUILayout.TextField("Name", _chunkName);
            _pathMat  = (Material)EditorGUILayout.ObjectField("Path Material", _pathMat, typeof(Material), false);
            _towerMat = (Material)EditorGUILayout.ObjectField("Tower Material", _towerMat, typeof(Material), false);
            _homeMat  = (Material)EditorGUILayout.ObjectField("Home Material", _homeMat, typeof(Material), false);
            _lockMat  = (Material)EditorGUILayout.ObjectField("Elevation Lock Material", _lockMat, typeof(Material), false);
            _cellSize = EditorGUILayout.Slider("Cell Size", _cellSize, 0.5f, 2f);

            DrawDerivedInfo();

            GUILayout.Space(8);
            GUILayout.Label("Generate", EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            _chunkIndex = (ChunkCatalog)EditorGUILayout.ObjectField(
                "Chunk Catalog Index", _chunkIndex, typeof(ChunkCatalog), false);
            if (EditorGUI.EndChangeCheck())
            {
                BindCompareFromIndex(_generateType);
                _indexDirty = true;
            }
            DrawGenerateTypePopup();
            EditorGUI.BeginChangeCheck();
            _compareCatalog = (ChunkTypeCatalog)EditorGUILayout.ObjectField(
                "Compare catalog", _compareCatalog, typeof(ChunkTypeCatalog), false);
            if (EditorGUI.EndChangeCheck())
                _indexDirty = true;
            _genSeed = EditorGUILayout.IntField("Seed (0 = random)", _genSeed);
            if (_lastGenSeed != 0)
                EditorGUILayout.LabelField("Last seed", _lastGenSeed.ToString());
            if (GUILayout.Button("Generate", GUILayout.Height(28)))
                Generate();
            DrawSavedLayoutStatus();
            EditorGUILayout.HelpBox(
                GenerateStatusHelp(),
                MessageType.None);

            GUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Prefab", GUILayout.Height(28))) SavePrefab();
                if (GUILayout.Button("Load Prefab", GUILayout.Height(28))) LoadPrefab();
                if (GUILayout.Button("Clear", GUILayout.Height(28))) Clear();
            }

            _loaded = (MapChunkStamp)EditorGUILayout.ObjectField(
                "Loaded Stamp", _loaded, typeof(MapChunkStamp), false);

            EditorGUILayout.EndScrollView();
        }

        void DrawSavedLayoutStatus()
        {
            EnsureSavedIndex();
            if (_compareCatalog == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a compare catalog (or a chunk catalog index) to check uniqueness.",
                    MessageType.Warning);
                return;
            }
            var mask = new ChunkMask(_cells, _homeIndex, _elevationLocked);
            var id = ChunkMaskId.Canonical(mask);
            if (_savedCanonical.TryGetValue(id, out var name))
                EditorGUILayout.HelpBox(
                    "Already in " + _compareCatalog.name + " as " + name + " (same layout, any yaw).",
                    MessageType.Warning);
            else
                EditorGUILayout.HelpBox(
                    "New layout — not in " + _compareCatalog.name + ".",
                    MessageType.Info);
        }

        void DrawGenerateTypePopup()
        {
            var idx = 0;
            for (var i = 0; i < GeneratableTypes.Length; i++)
            {
                if (GeneratableTypes[i] == _generateType)
                {
                    idx = i;
                    break;
                }
            }
            EditorGUI.BeginChangeCheck();
            var next = EditorGUILayout.Popup("Chunk type", idx, GeneratableLabels);
            if (EditorGUI.EndChangeCheck())
            {
                _generateType = GeneratableTypes[next];
                BindCompareFromIndex(_generateType);
                _indexDirty = true;
            }
        }

        void SyncGenerateTypeFromGrid()
        {
            var type = new ChunkMask(_cells, _homeIndex, _elevationLocked).Type;
            if (type == _lastGridType) return;
            _lastGridType = type;
            if (type == ChunkType.Land) return;
            _generateType = type;
            BindCompareFromIndex(type);
            _indexDirty = true;
        }

        void BindCompareFromIndex(ChunkType type)
        {
            if (_chunkIndex == null) return;
            var cat = _chunkIndex.CatalogFor(type);
            if (cat == null) return;
            _compareCatalog = cat;
        }

        void EnsureDefaultIndex()
        {
            if (_chunkIndex != null) return;
            _chunkIndex = AssetDatabase.LoadAssetAtPath<ChunkCatalog>(DefaultCatalogPath);
            if (_chunkIndex != null && _compareCatalog == null)
                BindCompareFromIndex(_generateType);
        }

        void EnsureSavedIndex()
        {
            if (!_indexDirty) return;
            ChunkStampIdIndex.Load(_compareCatalog, _savedCanonical);
            _indexDirty = false;
        }

        string GenerateStatusHelp()
        {
            if (_generateType == ChunkType.Homebase)
                return "Homebase: paint home (center recommended) + 1–4 mid-edge openings for difficulty. No edge lock. Compare against Homebase catalog. Generate does not create keeps — paint and Save Prefab.";
            if (_generateType == ChunkType.DeadEnd)
                return "DeadEnd generate is not implemented yet. Painter lock: South only. Runtime still yaws.";
            if (_generateType == ChunkType.Straight)
                return "Straight locked to North+South when generating. Uniqueness still matches any yaw of the same layout.";
            if (_generateType == ChunkType.TJunction)
                return "T-junction locked to South+East+West when generating. Uniqueness still matches any yaw of the same layout.";
            if (_generateType == ChunkType.Cross)
                return "Cross locked to all four mid-edges when generating. Uniqueness still matches any yaw of the same layout.";
            return "Corner locked to South+East when generating (runtime still yaws). Uniqueness still matches any yaw of the same layout. 1-wide path, islands 1–3. Inspect, then Save Prefab.";
        }

        void OnFocus() => _indexDirty = true;

        string ActiveToolStatus()
        {
            switch (_tool)
            {
                case PaintTool.Home:
                    return "Active painter: Homebase. LMB places leak home (always default height). RMB clears the tile.";
                case PaintTool.ElevationLock:
                    return "Active painter: Height lock. LMB locks/unlocks a tower cell at default height. Path and home stay locked. RMB clears the tile.";
                default:
                    return "Active painter: Path. LMB toggles path (blue) / tower (grey). Path is always default height. RMB clears the tile.";
            }
        }

        void DrawGrid()
        {
            var sw = EditorGUIUtility.currentViewWidth - 36f;
            var cs = Mathf.Max(18f, sw / Size);
            var rect = GUILayoutUtility.GetRect(sw, cs * Size, GUILayout.ExpandWidth(false));
            for (var y = Size - 1; y >= 0; y--)
            {
                for (var x = 0; x < Size; x++)
                {
                    var r = new Rect(rect.x + x * cs, rect.y + (Size - 1 - y) * cs, cs - 1f, cs - 1f);
                    var i = y * Size + x;
                    var prev = GUI.color;
                    GUI.color = CellColor(i);
                    GUI.DrawTexture(r, EditorGUIUtility.whiteTexture);
                    GUI.color = prev;
                    if (Event.current.type == EventType.MouseDown
                        && r.Contains(Event.current.mousePosition))
                    {
                        if (Event.current.button == 0)
                            ApplyPaint(i);
                        else if (Event.current.button == 1)
                            ClearTile(i);
                        Event.current.Use();
                        GUI.changed = true;
                        Repaint();
                    }
                    else if (Event.current.type == EventType.ContextClick
                             && r.Contains(Event.current.mousePosition))
                    {
                        Event.current.Use();
                    }
                    DrawEdgeMarker(r, x, y);
                }
            }
        }

        Color CellColor(int i)
        {
            if (_tool == PaintTool.ElevationLock)
                return _elevationLocked[i]
                    ? new Color(0.55f, 0.35f, 0.75f)
                    : new Color(0.55f, 0.6f, 0.55f);

            if (i == _homeIndex) return new Color(0.85f, 0.35f, 0.2f);
            if (_cells[i]) return new Color(0.25f, 0.55f, 0.95f);
            if (_elevationLocked[i]) return new Color(0.55f, 0.35f, 0.75f);
            return new Color(0.55f, 0.6f, 0.55f);
        }

        void ApplyPaint(int i)
        {
            switch (_tool)
            {
                case PaintTool.Home:
                    if (_homeIndex == i) _homeIndex = -1;
                    else
                    {
                        _homeIndex = i;
                        _cells[i] = true;
                        _elevationLocked[i] = true;
                    }
                    break;
                case PaintTool.ElevationLock:
                    if (_cells[i] || i == _homeIndex)
                    {
                        _elevationLocked[i] = true;
                        break;
                    }
                    _elevationLocked[i] = !_elevationLocked[i];
                    break;
                default:
                    _cells[i] = !_cells[i];
                    if (_cells[i])
                        _elevationLocked[i] = true;
                    if (!_cells[i] && _homeIndex == i)
                        _homeIndex = -1;
                    break;
            }
        }

        void ClearTile(int i)
        {
            _cells[i] = false;
            _elevationLocked[i] = false;
            if (_homeIndex == i)
                _homeIndex = -1;
        }

        void DrawEdgeMarker(Rect r, int x, int y)
        {
            if (x != ChunkMask.Mid && y != ChunkMask.Mid) return;
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.4f);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2f), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 2f, r.width, 2f), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, 2f, r.height), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - 2f, r.y, 2f, r.height), EditorGUIUtility.whiteTexture);
            GUI.color = prev;
        }

        void DrawDerivedInfo()
        {
            var mask = new ChunkMask(_cells, _homeIndex, _elevationLocked);
            var edges = mask.OpenEdges;
            var type = mask.Type;
            var lockedCount = 0;
            for (var i = 0; i < _elevationLocked.Length; i++)
                if (_elevationLocked[i]) lockedCount++;
            EditorGUILayout.LabelField("Open Edges", edges.ToString());
            EditorGUILayout.LabelField("Chunk Type", type.ToString());
            EditorGUILayout.LabelField("Editor lock", ChunkPathRules.EditorLockedEdges(type).ToString());
            EditorGUILayout.LabelField("Elevation locked", $"{lockedCount} cells (default height)");
            if (mask.HasHome)
                EditorGUILayout.LabelField("Home", $"{mask.HomeLocal.x}, {mask.HomeLocal.y} (leak cell)");
            else
                EditorGUILayout.LabelField("Home", "none — select Homebase and click a cell");
            EditorGUILayout.LabelField("Colors", "blue=path  grey=tower  terracotta=home  purple=height lock");
            if (type == ChunkType.Homebase)
                EditorGUILayout.HelpBox(
                    "Homebase (keep): paint home at center (or leak cell). Openings 1–4 by difficulty — no single painter edge lock. Runtime yaws an opening onto the East start arm. Not used as an expand pick.",
                    MessageType.Info);
            else if (type == ChunkType.DeadEnd)
                EditorGUILayout.HelpBox(
                    "Dead-end cap: one opening. Paint path to the center — that cell is the spawn portal.",
                    MessageType.Info);
            if (edges != EdgeFlags.None && !mask.AreOpeningsConnected())
                EditorGUILayout.HelpBox("Disconnected openings: path does not connect all open edges.",
                    MessageType.Warning);
            if (type != ChunkType.Land && type != ChunkType.Homebase
                && !ChunkPathRules.HasEditorLockedEdges(mask))
                EditorGUILayout.HelpBox(
                    "Painter lock: " + type + " should open " + ChunkPathRules.EditorLockedEdges(type)
                    + " only. Runtime expand still yaws this prefab.",
                    MessageType.Warning);
            if (HasPathButNoOpenings())
                EditorGUILayout.HelpBox("Interior path with no openings (ends at the wall).",
                    MessageType.Info);
        }

        bool HasPathButNoOpenings()
        {
            var any = false;
            for (var i = 0; i < _cells.Length; i++)
                if (_cells[i]) { any = true; break; }
            return any && new ChunkMask(_cells).OpenEdges == EdgeFlags.None;
        }

        void SavePrefab()
        {
            EnsureCellBuffers();
            if (string.IsNullOrWhiteSpace(_chunkName))
            {
                Debug.LogError("[ChunkPainter] Name required.");
                return;
            }
            EnsureFolder();
            var path = $"{ChunksFolder}/{_chunkName}.prefab";
            var root = new GameObject(_chunkName);
            var stamp = root.AddComponent<MapChunkStamp>();
            stamp.ApplyMask(new ChunkMask(_cells, _homeIndex, _elevationLocked));
            stamp.BuildVisuals(_pathMat, _towerMat, _cellSize, _homeMat, _lockMat);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path);
            AddStampToCurrentCatalog(prefab);
            _indexDirty = true;
            Debug.Log($"[ChunkPainter] Saved {path}");
        }

        void AddStampToCurrentCatalog(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[ChunkPainter] Save succeeded but prefab asset was null; catalog not updated.");
                return;
            }
            var saved = prefab.GetComponent<MapChunkStamp>();
            if (saved == null)
            {
                Debug.LogWarning("[ChunkPainter] Saved prefab has no MapChunkStamp.");
                return;
            }
            var catalog = _compareCatalog;
            if (catalog == null && _chunkIndex != null)
                catalog = _chunkIndex.CatalogFor(saved.GetMask().Type);
            if (catalog == null)
            {
                Debug.LogWarning("[ChunkPainter] Saved prefab but no compare catalog is assigned, so it was not added to a catalog.");
                return;
            }
            if (!catalog.TryAddStamp(saved))
                return;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        void Generate()
        {
            EnsureCellBuffers();
            if (_generateType == ChunkType.Homebase)
            {
                Debug.LogWarning("[ChunkPainter] Homebase is paint-only — place home + openings, then Save Prefab.");
                return;
            }
            if (_generateType == ChunkType.DeadEnd || _generateType == ChunkType.Land)
            {
                Debug.LogWarning("[ChunkPainter] Generate is implemented for Straight, Corner, TJunction, and Cross.");
                return;
            }
            EnsureSavedIndex();
            _lastGenSeed = _genSeed != 0 ? _genSeed : System.Environment.TickCount;
            var rng = new System.Random(_lastGenSeed);
            var exclude = new HashSet<string>(_savedCanonical.Keys);
            var unique = ChunkMaskGenerator.TryGenerate(_generateType, rng, out var mask, exclude);
            if (mask.Type != _generateType)
            {
                Debug.LogWarning("[ChunkPainter] " + _generateType + " generate failed.");
                return;
            }
            ApplyMaskToBuffers(mask);
            _chunkName = "chunk_" + _generateType.ToString().ToLowerInvariant() + "_" + _lastGenSeed;
            _lastGridType = mask.Type;
            BindCompareFromIndex(_generateType);
            _indexDirty = true;
            if (!unique)
                Debug.LogWarning("[ChunkPainter] Every attempt matched the compare catalog. Showing a duplicate.");
            Repaint();
        }

        void ApplyMaskToBuffers(ChunkMask mask)
        {
            for (var i = 0; i < ChunkMask.CellCount; i++)
            {
                _cells[i] = mask.IsPath(i % Size, i / Size);
                _elevationLocked[i] = false;
            }
            mask.CopyElevationLocked(_elevationLocked);
            _homeIndex = mask.HasHome
                ? mask.HomeLocal.y * Size + mask.HomeLocal.x
                : -1;
        }

        void LoadPrefab()
        {
            if (_loaded == null) return;
            EnsureCellBuffers();
            ApplyMaskToBuffers(_loaded.GetMask());
            _chunkName = _loaded.gameObject.name;
            Repaint();
        }

        void Clear()
        {
            EnsureCellBuffers();
            for (var i = 0; i < _cells.Length; i++)
            {
                _cells[i] = false;
                _elevationLocked[i] = false;
            }
            _homeIndex = -1;
            Repaint();
        }

        void EnsureCellBuffers()
        {
            var n = ChunkMask.CellCount;
            _cells = ResizeBoolBuffer(_cells, n);
            _elevationLocked = ResizeBoolBuffer(_elevationLocked, n);
            if (_homeIndex >= n)
                _homeIndex = -1;
        }

        static bool[] ResizeBoolBuffer(bool[] src, int n)
        {
            if (src != null && src.Length == n)
                return src;
            var dst = new bool[n];
            if (src != null)
            {
                var copy = src.Length < n ? src.Length : n;
                for (var i = 0; i < copy; i++)
                    dst[i] = src[i];
            }
            return dst;
        }

        void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(ChunksFolder)) return;
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Map"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Map");
            AssetDatabase.CreateFolder("Assets/Prefabs/Map", "Chunks");
        }

        void EnsureMaterials()
        {
            if (_pathMat != null && _towerMat != null && _homeMat != null && _lockMat != null) return;
            const string folder = "Assets/Art/Placeholders";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
                AssetDatabase.CreateFolder("Assets/Art", "Placeholders");
            }
            _pathMat  = EnsureMat("Chunk_Path",  new Color(0.25f, 0.55f, 0.95f), folder);
            _towerMat = EnsureMat("Chunk_Tower", new Color(0.55f, 0.6f, 0.55f), folder);
            _homeMat  = EnsureMat("Chunk_Home",  new Color(0.85f, 0.35f, 0.2f), folder);
            _lockMat  = EnsureMat("Chunk_ElevationLock", new Color(0.55f, 0.35f, 0.75f), folder);
        }

        static Material EnsureMat(string name, Color color, string folder)
        {
            var path = $"{folder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
