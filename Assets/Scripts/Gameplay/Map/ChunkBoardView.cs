using System.Collections.Generic;
using UnityEngine;
using GemTD.Core;
using GemTD.Grass;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Map
{
    /// <summary>Replaces GridBoardView. Listens to ChunkPlaced and instantiates chunk prefabs.</summary>
    public sealed class ChunkBoardView : MonoBehaviour
    {
        [SerializeField] float cellSize = 1f;
        [SerializeField] Transform chunkParent;
        [SerializeField] Material towerHeight0;
        [SerializeField] Material towerHeight1;
        [SerializeField] Material towerHeight2;
        [SerializeField] PathTileSet pathTiles;
        [Header("Grass")]
        [SerializeField] bool useGrassRenderer = true;
        [SerializeField] GrassChunkRenderer grassRendererPrefab;
        [SerializeField] GrassStyleDefinition grassStyle;
        [Tooltip("Small lift applied to the generated chunk distribution surface.")]
        [SerializeField, Min(0f)] float grassSurfaceOffset = 0.01f;

        public float CellSize => cellSize;

        ChunkGrid _grid;
        TileHeightMap _heights;
        float _tileSpacing = 0.05f;
        Material[] _tintedFallback;
        bool _loggedMissingAuthoredMats;
        bool _loggedMissingPathTiles;
        bool _loggedMissingGrassReferences;
        bool _hasAppliedGrassUse;
        bool _appliedUseGrassRenderer;
        readonly Dictionary<Vector2Int, GameObject> _instances = new Dictionary<Vector2Int, GameObject>(32);
        const int GrassLayoutSeed = unchecked((int)0x5e7a91c3);
        readonly Dictionary<Vector2Int, GrassChunkRenderer> _grassRenderers =
            new Dictionary<Vector2Int, GrassChunkRenderer>(32);
        readonly List<GrassSurfacePatch> _grassPatchScratch = new List<GrassSurfacePatch>(49);
        readonly List<GrassExclusion> _grassExclusionScratch = new List<GrassExclusion>(8);
        readonly List<GrassInstance> _grassInstanceScratch = new List<GrassInstance>(512);
        TowerPlacementService _grassPlacement;

        public void Bind(ChunkGrid grid, TileHeightMap heights = null, float tileSpacing = 0.05f)
        {
            _grid = grid;
            _heights = heights;
            _tileSpacing = tileSpacing < 0f ? 0f : tileSpacing;
            GameEvents.ChunkPlaced += OnChunkPlaced;
        }

        public void BindGrassOccupancy(TowerPlacementService placement)
        {
            if (_grassPlacement == placement)
                return;

            if (_grassPlacement != null)
                _grassPlacement.OccupancyChanged -= OnGrassOccupancyChanged;

            _grassPlacement = placement;
            if (_grassPlacement != null)
                _grassPlacement.OccupancyChanged += OnGrassOccupancyChanged;

            if (useGrassRenderer)
                RebuildAllGrass();
        }

        public void SetTileSpacing(float spacing)
        {
            if (spacing < 0f)
                spacing = 0f;
            if (Mathf.Abs(_tileSpacing - spacing) < 1e-5f)
                return;
            _tileSpacing = spacing;
            foreach (var pair in _instances)
            {
                if (pair.Value == null)
                    continue;
                if (_grid != null && _grid.TryGet(pair.Key.x, pair.Key.y, out var slot))
                    ApplyFootprints(pair.Value.transform, slot);
                else
                    ApplyFootprints(pair.Value.transform, default);
            }

            RebuildAllGrass();
        }

        public void OnChunkPlaced(Vector2Int coord)
        {
            if (_grid == null || !_grid.TryGet(coord.x, coord.y, out var slot)) return;
            if (slot.Prefab == null) return;

            var parent = chunkParent != null ? chunkParent : transform;
            var instance = Instantiate(slot.Prefab, parent);
            instance.transform.localRotation = Quaternion.Euler(0f, slot.Yaw * 90f, 0f);
            instance.transform.localPosition = ChunkInstanceLocalPosition(coord, slot.Yaw, cellSize);
            _instances[coord] = instance.gameObject;
            ApplyFootprints(instance.transform, slot);
            ApplyTileHeights(instance.transform, coord, slot);
            RebuildGrass(coord, instance.transform, slot);
        }

        void ApplyFootprints(Transform instance, ChunkSlot slot)
        {
            // Baked path meshes stay at the set's scale so corridors meet across cells.
            var skipPath = slot.Prefab != null;
            for (var i = 0; i < instance.childCount; i++)
            {
                var child = instance.GetChild(i);
                if (!TileHeightVisual.TryParseTileName(child.name, out var px, out var py))
                    continue;
                if (TileHeightVisual.HasPad(child))
                    continue;
                if (skipPath)
                {
                    var worldLocal = RotateLocalCw(px, py, slot.Yaw);
                    if (slot.Mask.IsPath(worldLocal.x, worldLocal.y))
                        continue;
                }
                TileHeightVisual.ApplyFootprint(child, cellSize, _tileSpacing);
            }
        }

        void ApplyTileHeights(Transform instance, Vector2Int coord, ChunkSlot slot)
        {
            if (_heights == null)
                return;

            for (var i = 0; i < instance.childCount; i++)
            {
                var child = instance.GetChild(i);
                if (!TileHeightVisual.TryParseTileName(child.name, out var px, out var py))
                    continue;

                var worldLocal = RotateLocalCw(px, py, slot.Yaw);
                if (slot.Mask.IsElevationLocked(worldLocal.x, worldLocal.y))
                    continue;

                var wx = coord.x * ChunkMask.Size + worldLocal.x;
                var wy = coord.y * ChunkMask.Size + worldLocal.y;
                var layer = _heights.Get(wx, wy);
                if (TileHeightVisual.TryActivatePad(child, layer))
                {
                    ApplyRolledPadMaterials(child, layer, wx, wy);
                    continue;
                }

                var renderer = child.GetComponent<MeshRenderer>();
                var mat = ResolveHeightMaterial(layer, renderer != null ? renderer.sharedMaterial : null);
                TileHeightVisual.ApplyPad(child, layer, mat);
            }
        }

        void ApplyRolledPadMaterials(Transform tile, byte layer, int wx, int wy)
        {
            if (pathTiles == null)
            {
                if (_loggedMissingPathTiles)
                    return;
                _loggedMissingPathTiles = true;
                Debug.LogWarning(
                    "[GemTD] ChunkBoardView has no Path Tile Set — pad materials stay on the cliff prefab.");
                return;
            }

            var height = TileHeightVisual.PadHeight(layer);
            if (!pathTiles.TryRollPadMaterials(height, wx, wy, out var top, out var sides))
                return;
            TileHeightVisual.ApplyPadLook(tile, top, sides);
        }

        Material ResolveHeightMaterial(byte layer, Material source)
        {
            var authored = HeightMaterial(layer);
            if (authored != null)
                return authored;

            if (_tintedFallback == null && source != null)
            {
                _tintedFallback = TileHeightVisual.CreateLayerMaterials(source);
                if (!_loggedMissingAuthoredMats)
                {
                    _loggedMissingAuthoredMats = true;
                    Debug.LogWarning(
                        "[GemTD] ChunkBoardView towerHeight0/1/2 are unassigned — tinting from the pad's prefab material.");
                }
            }

            if (_tintedFallback == null)
                return null;
            var i = layer >= 2 ? 2 : (int)layer;
            return _tintedFallback[i];
        }

        Material HeightMaterial(byte layer)
        {
            if (layer >= 2) return towerHeight2;
            if (layer == 1) return towerHeight1;
            return towerHeight0;
        }

        void LateUpdate()
        {
            if (!_hasAppliedGrassUse)
            {
                _hasAppliedGrassUse = true;
                _appliedUseGrassRenderer = useGrassRenderer;
                if (!useGrassRenderer)
                    ClearAllGrass();
                return;
            }

            if (_appliedUseGrassRenderer == useGrassRenderer)
                return;

            _appliedUseGrassRenderer = useGrassRenderer;
            if (useGrassRenderer)
                RebuildAllGrass();
            else
                ClearAllGrass();
        }

        void RebuildAllGrass()
        {
            if (!useGrassRenderer)
            {
                ClearAllGrass();
                return;
            }

            if (_grid == null)
                return;

            foreach (var pair in _instances)
            {
                if (pair.Value == null || !_grid.TryGet(pair.Key.x, pair.Key.y, out var slot))
                    continue;
                RebuildGrass(pair.Key, pair.Value.transform, slot);
            }
        }

        void RebuildGrass(Vector2Int coord, Transform instance, ChunkSlot slot)
        {
            if (!useGrassRenderer)
            {
                ClearGrass(coord);
                return;
            }

            if (grassRendererPrefab == null || grassStyle == null || instance == null)
            {
                WarnMissingGrassReferences();
                return;
            }

            if (!_grassRenderers.TryGetValue(coord, out var renderer) || renderer == null)
            {
                renderer = Instantiate(grassRendererPrefab, instance);
                renderer.transform.localPosition = Vector3.zero;
                renderer.transform.localRotation = Quaternion.identity;
                renderer.transform.localScale = Vector3.one;
                _grassRenderers[coord] = renderer;
            }

            ChunkGrassPatchAdapter.BuildPatches(
                instance,
                coord,
                in slot,
                _grid,
                _heights,
                grassSurfaceOffset,
                _grassPatchScratch);
            ChunkGrassPatchAdapter.BuildExclusions(
                instance,
                coord,
                in slot,
                _grassPlacement,
                grassStyle.TowerClearanceRadius,
                _grassExclusionScratch);
            var settings = grassStyle.CreateLayoutSettings();
            GrassLayoutBuilder.Build(
                _grassPatchScratch,
                _grassExclusionScratch,
                in settings,
                GrassLayoutSeed,
                _grassInstanceScratch);
            var bounds = ChunkGrassPatchAdapter.CalculateLocalBounds(_grassPatchScratch);
            renderer.Bind(grassStyle, _grassInstanceScratch, bounds);
            renderer.enabled = true;
        }

        void ClearAllGrass()
        {
            foreach (var renderer in _grassRenderers.Values)
                ReleaseRenderer(renderer);

            _grassRenderers.Clear();
        }

        void ClearGrass(Vector2Int coord)
        {
            if (_grassRenderers.TryGetValue(coord, out var renderer))
            {
                ReleaseRenderer(renderer);
                _grassRenderers.Remove(coord);
            }
        }

        static void ReleaseRenderer(GrassChunkRenderer renderer)
        {
            if (renderer == null)
                return;

            var gameObject = renderer.gameObject;
            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
        }

        void WarnMissingGrassReferences()
        {
            if (_loggedMissingGrassReferences)
                return;

            _loggedMissingGrassReferences = true;
            Debug.LogWarning(
                "[GemTD] ChunkBoardView needs GrassRendererPrefab and GrassStyle references before it can build chunk grass.");
        }

        void OnGrassOccupancyChanged(Vector2Int cell)
        {
            if (!useGrassRenderer || _grid == null)
                return;

            var coord = new Vector2Int(
                FloorDiv(cell.x, ChunkMask.Size),
                FloorDiv(cell.y, ChunkMask.Size));
            if (!_instances.TryGetValue(coord, out var instance) || instance == null)
                return;
            if (!_grid.TryGet(coord.x, coord.y, out var slot))
                return;

            RebuildGrass(coord, instance.transform, slot);
        }

        static int FloorDiv(int value, int divisor)
        {
            var quotient = value / divisor;
            if (value % divisor < 0)
                quotient--;
            return quotient;
        }

        static Vector2Int RotateLocalCw(int x, int y, int yaw)
        {
            var n = ChunkMask.Size;
            var turns = ((yaw % 4) + 4) % 4;
            for (var t = 0; t < turns; t++)
            {
                var nx = y;
                var ny = n - 1 - x;
                x = nx;
                y = ny;
            }
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Prefab pivot is the SW corner (painter tiles at x+0.5, z+0.5). Mask rotation is
        /// in-place around the chunk center, so the instance must orbit that same center.
        /// </summary>
        public static Vector3 ChunkInstanceLocalPosition(Vector2Int coord, int yaw, float cellSize)
        {
            var size = ChunkMask.Size * cellSize;
            var origin = new Vector3(coord.x * size, 0f, coord.y * size);
            var half = new Vector3(size * 0.5f, 0f, size * 0.5f);
            var rot = Quaternion.Euler(0f, yaw * 90f, 0f);
            return origin + half - rot * half;
        }

        /// <summary>World position of a prefab-local tile center after yaw around the chunk center.</summary>
        public static Vector3 TileWorldAfterYaw(Vector2Int coord, int yaw, int lx, int ly, float cellSize)
        {
            var rot = Quaternion.Euler(0f, yaw * 90f, 0f);
            var local = new Vector3(lx * cellSize + cellSize * 0.5f, 0f, ly * cellSize + cellSize * 0.5f);
            return ChunkInstanceLocalPosition(coord, yaw, cellSize) + rot * local;
        }

        void OnDestroy()
        {
            GameEvents.ChunkPlaced -= OnChunkPlaced;
            if (_grassPlacement != null)
                _grassPlacement.OccupancyChanged -= OnGrassOccupancyChanged;
            _grassPlacement = null;
            ClearAllGrass();
        }

        public Vector3 CellToWorld(int x, int y) => CellCenterWorld(x, y);
        public Vector3 CellToWorld(Vector2Int cell) => CellCenterWorld(cell.x, cell.y);

        public Vector3 TowerCellWorld(Vector2Int cell) => TowerCellWorld(cell.x, cell.y);

        public Vector3 TowerCellWorld(int x, int y)
        {
            var p = CellCenterWorld(x, y);
            var layer = _heights != null ? _heights.Get(x, y) : (byte)0;
            p.y += TileHeightVisual.TopY(layer);
            return p;
        }

        public Vector3 CellCenterWorld(int x, int y)
        {
            var half = cellSize * 0.5f;
            return transform.TransformPoint(new Vector3(x * cellSize + half, 0f, y * cellSize + half));
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            var local = transform.InverseTransformPoint(world);
            var x = Mathf.FloorToInt(local.x / cellSize);
            var y = Mathf.FloorToInt(local.z / cellSize);
            return new Vector2Int(x, y);
        }

        /// <summary>Cell whose pad the ray hits. Falls back to the y=0 plane before heights are bound.</summary>
        public bool TryPickCell(Ray worldRay, out Vector2Int cell)
        {
            if (_heights == null)
                return TryPickFlatPlane(worldRay, out cell);

            var origin = transform.InverseTransformPoint(worldRay.origin);
            var direction = transform.InverseTransformVector(worldRay.direction);
            return BoardCellPick.TryPick(new Ray(origin, direction), cellSize, _heights, out cell);
        }

        bool TryPickFlatPlane(Ray worldRay, out Vector2Int cell)
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(worldRay, out var enter))
            {
                cell = default;
                return false;
            }

            cell = WorldToCell(worldRay.GetPoint(enter));
            return true;
        }

        public Vector3 ChunkCenterWorld(Vector2Int coord)
        {
            var halfChunk = ChunkMask.Size * cellSize * 0.5f;
            return transform.TransformPoint(new Vector3(
                coord.x * ChunkMask.Size * cellSize + halfChunk, 0f,
                coord.y * ChunkMask.Size * cellSize + halfChunk));
        }

        public Vector3 ChunkCellWorld(Vector2Int coord, int lx, int ly)
        {
            var wx = coord.x * ChunkMask.Size + lx;
            var wy = coord.y * ChunkMask.Size + ly;
            return CellCenterWorld(wx, wy);
        }

#if UNITY_EDITOR
        void OnValidate() => cellSize = Mathf.Max(0.1f, cellSize);
#endif
    }
}
