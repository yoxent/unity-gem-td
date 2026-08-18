using UnityEditor;
using UnityEngine;
using GemTD.Gameplay.Map;

namespace GemTD.Editor
{
    public sealed class MapChunkPainterWindow : EditorWindow
    {
        const int Size = ChunkMask.Size;
        const string ChunksFolder = "Assets/Prefabs/Map/Chunks";

        bool[] _cells = new bool[ChunkMask.CellCount];
        string _chunkName = "chunk_new";
        Material _pathMat;
        Material _towerMat;
        float _cellSize = 1f;
        MapChunkStamp _loaded;

        [MenuItem("Gem TD/Map Chunk Painter")]
        public static void Open() => GetWindow<MapChunkPainterWindow>("Map Chunk Painter");

        void OnGUI()
        {
            EnsureFolder();
            EnsureMaterials();

            GUILayout.Label("Paint a 5x5 chunk. Blue = path, grey = tower.", EditorStyles.wordWrappedLabel);

            DrawGrid();

            GUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                _chunkName = EditorGUILayout.TextField("Name", _chunkName);
            }
            _pathMat  = (Material)EditorGUILayout.ObjectField("Path Material", _pathMat, typeof(Material), false);
            _towerMat = (Material)EditorGUILayout.ObjectField("Tower Material", _towerMat, typeof(Material), false);
            _cellSize = EditorGUILayout.Slider("Cell Size", _cellSize, 0.5f, 2f);

            DrawDerivedInfo();

            GUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Prefab", GUILayout.Height(28))) SavePrefab();
                if (GUILayout.Button("Load Prefab", GUILayout.Height(28))) LoadPrefab();
                if (GUILayout.Button("Clear", GUILayout.Height(28))) Clear();
            }

            _loaded = (MapChunkStamp)EditorGUILayout.ObjectField(
                "Loaded Stamp", _loaded, typeof(MapChunkStamp), false);
        }

        void DrawGrid()
        {
            var sw = EditorGUIUtility.currentViewWidth - 30f;
            var cs = sw / Size;
            var rect = GUILayoutUtility.GetRect(sw, cs * Size);
            for (var y = Size - 1; y >= 0; y--)
            {
                for (var x = 0; x < Size; x++)
                {
                    var r = new Rect(rect.x + x * cs, rect.y + (Size - 1 - y) * cs, cs, cs);
                    var i = y * Size + x;
                    var prev = GUI.color;
                    GUI.color = _cells[i] ? new Color(0.25f, 0.55f, 0.95f) : new Color(0.55f, 0.6f, 0.55f);
                    GUI.DrawTexture(r, EditorGUIUtility.whiteTexture);
                    GUI.color = prev;
                    if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                    {
                        _cells[i] = !_cells[i];
                        Event.current.Use();
                        GUI.changed = true;
                        Repaint();
                    }
                    DrawEdgeMarker(r, x, y);
                }
            }
        }

        void DrawEdgeMarker(Rect r, int x, int y)
        {
            // Mark middle-of-edge cells with a thin border so the author sees which cells are openings.
            if (x != 2 && y != 2) return;
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
            var mask = new ChunkMask(_cells);
            var edges = mask.OpenEdges;
            var type = mask.Type;
            EditorGUILayout.LabelField("Open Edges", edges.ToString());
            EditorGUILayout.LabelField("Chunk Type", type.ToString());
            if (edges != EdgeFlags.None && !mask.AreOpeningsConnected())
                EditorGUILayout.HelpBox("Disconnected openings: path does not connect all open edges.",
                    MessageType.Warning);
            if (HasPathButNoOpenings())
                EditorGUILayout.HelpBox("Interior path with no openings (ends at the wall).",
                    MessageType.Info);
        }

        bool HasPathButNoOpenings()
        {
            var any = false;
            for (var i = 0; i < _cells.Length; i++) if (_cells[i]) { any = true; break; }
            return any && new ChunkMask(_cells).OpenEdges == EdgeFlags.None;
        }

        void SavePrefab()
        {
            if (string.IsNullOrWhiteSpace(_chunkName))
            {
                Debug.LogError("[ChunkPainter] Name required.");
                return;
            }
            EnsureFolder();
            var path = $"{ChunksFolder}/{_chunkName}.prefab";
            var root = new GameObject(_chunkName);
            var stamp = root.AddComponent<MapChunkStamp>();
            stamp.ApplyMask(new ChunkMask(_cells));
            stamp.BuildVisuals(_pathMat, _towerMat, _cellSize);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path);
            Debug.Log($"[ChunkPainter] Saved {path}");
        }

        void LoadPrefab()
        {
            if (_loaded == null) return;
            var mask = _loaded.GetMask();
            for (var i = 0; i < ChunkMask.CellCount; i++)
                _cells[i] = mask.IsPath(i % Size, i / Size);
            _chunkName = _loaded.gameObject.name;
            Repaint();
        }

        void Clear()
        {
            for (var i = 0; i < _cells.Length; i++) _cells[i] = false;
            Repaint();
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
            if (_pathMat != null && _towerMat != null) return;
            const string folder = "Assets/Art/Placeholders";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
                AssetDatabase.CreateFolder("Assets/Art", "Placeholders");
            }
            _pathMat  = EnsureMat("Chunk_Path",  new Color(0.25f, 0.55f, 0.95f), folder);
            _towerMat = EnsureMat("Chunk_Tower", new Color(0.55f, 0.6f, 0.55f), folder);
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
