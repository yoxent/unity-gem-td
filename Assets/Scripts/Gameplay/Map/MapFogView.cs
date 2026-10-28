using UnityEngine;
using GemTD.Core;

namespace GemTD.Gameplay.Map
{
    /// <summary>
    /// Black sheet over the playfield. A placed chunk clears its square and
    /// a disc a few cells past that square. The sheet matches the mask, which
    /// already includes that peek.
    /// </summary>
    public sealed class MapFogView : MonoBehaviour
    {
        [SerializeField] Material fogMaterial;
        [SerializeField] Color fogColor = Color.black;
        [SerializeField, Min(0f)] float edgePeekCells = MapFogMask.DefaultEdgePeekCells;

        ChunkGrid _grid;
        MapFogLayout _layout;
        float _revealRadius;
        GameObject _plane;
        Mesh _mesh;
        Material _runtimeMaterial;
        Texture2D _texture;
        Color32[] _pixels;
        bool _subscribed;

        public void Bind(ChunkBoardView board, ChunkGrid grid)
        {
            if (fogMaterial == null)
            {
                Debug.LogError("[GemTD] Assign the MapFog material on MapFogView.");
                return;
            }

            if (board == null || grid == null)
            {
                Debug.LogError("[GemTD] MapFogView.Bind requires a board and a chunk grid.");
                return;
            }

            Release();
            _grid = grid;
            var peek = edgePeekCells < 0f ? 0f : edgePeekCells;
            _revealRadius = MapFogMask.RevealRadius(ChunkMask.Size, peek);
            _layout = new MapFogLayout(
                grid.ChunksW,
                grid.ChunksH,
                ChunkMask.Size,
                MapFogMask.MarginFor(peek),
                board.CellSize);

            _texture = new Texture2D(_layout.Width, _layout.Height, TextureFormat.RGBA32, false)
            {
                name = "MapFogMask",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            _pixels = new Color32[_layout.Width * _layout.Height];
            _mesh = CreateQuad();
            _plane = new GameObject("MapFog");
            _plane.transform.SetParent(board.transform, false);

            _plane.transform.localPosition = new Vector3(_layout.BoardCenterX, 0f, _layout.BoardCenterZ);
            _plane.transform.localRotation = Quaternion.identity;
            _plane.transform.localScale = new Vector3(_layout.WorldWidth, 1f, _layout.WorldDepth);

            var filter = _plane.AddComponent<MeshFilter>();
            var renderer = _plane.AddComponent<MeshRenderer>();
            filter.sharedMesh = _mesh;
            _runtimeMaterial = new Material(fogMaterial);
            _runtimeMaterial.SetTexture("_MainTex", _texture);
            _runtimeMaterial.SetTextureScale("_MainTex", Vector2.one);
            _runtimeMaterial.SetTextureOffset("_MainTex", Vector2.zero);
            _runtimeMaterial.SetColor("_Color", fogColor);
            renderer.sharedMaterial = _runtimeMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            RepaintOccupied();
            GameEvents.ChunkPlaced += OnChunkPlaced;
            _subscribed = true;
        }

        void OnDestroy()
        {
            if (_subscribed)
                GameEvents.ChunkPlaced -= OnChunkPlaced;
            _subscribed = false;
            Release();
        }

        void OnChunkPlaced(Vector2Int coord)
        {
            if (_pixels == null || _texture == null)
                return;

            Stamp(coord.x, coord.y);
            ApplyTexture();
        }

        void RepaintOccupied()
        {
            MapFogMask.FillFog(_pixels);
            for (var cy = 0; cy < _grid.ChunksH; cy++)
            {
                for (var cx = 0; cx < _grid.ChunksW; cx++)
                {
                    if (!_grid.IsOccupied(cx, cy))
                        continue;
                    Stamp(cx, cy);
                }
            }

            ApplyTexture();
        }

        void Stamp(int cx, int cy)
        {
            if (_grid == null || !_grid.IsOccupied(cx, cy))
                return;

            MapFogMask.RevealChunk(
                _pixels,
                _layout.Width,
                _layout.Height,
                cx,
                cy,
                ChunkMask.Size,
                _layout.Margin,
                _revealRadius);
        }

        void ApplyTexture()
        {
            _texture.SetPixels32(_pixels);
            _texture.Apply(false, false);
        }

        void Release()
        {
            if (_subscribed)
                GameEvents.ChunkPlaced -= OnChunkPlaced;
            _subscribed = false;
            _grid = null;
            _pixels = null;

            if (_runtimeMaterial != null)
                Destroy(_runtimeMaterial);
            if (_texture != null)
                Destroy(_texture);
            if (_mesh != null)
                Destroy(_mesh);
            if (_plane != null)
                Destroy(_plane);

            _runtimeMaterial = null;
            _texture = null;
            _mesh = null;
            _plane = null;
        }

        static Mesh CreateQuad()
        {
            var mesh = new Mesh { name = "MapFogQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, -0.5f),
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f),
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            return mesh;
        }
    }
}
