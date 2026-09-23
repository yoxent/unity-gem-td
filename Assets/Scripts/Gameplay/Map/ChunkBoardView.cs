using System.Collections.Generic;
using UnityEngine;
using GemTD.Core;
using MicahW.PointGrass;

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
        [Header("Grass")]
        [SerializeField] PointGrassRenderer grassRendererTemplate;
        [SerializeField] GrassPatchPalette grassPatchPalette;
        [Tooltip("Small lift applied to the generated chunk distribution surface.")]
        [SerializeField, Min(0f)] float grassSurfaceOffset = 0.01f;

        public float CellSize => cellSize;

        ChunkGrid _grid;
        TileHeightMap _heights;
        float _tileSpacing = 0.05f;
        Material[] _tintedFallback;
        bool _loggedMissingAuthoredMats;
        bool _loggedMissingGrassTemplate;
        readonly Dictionary<Vector2Int, GameObject> _instances = new Dictionary<Vector2Int, GameObject>(32);
        readonly Dictionary<Vector2Int, PointGrassRenderer> _grassRenderers =
            new Dictionary<Vector2Int, PointGrassRenderer>(32);
        readonly Dictionary<Vector2Int, Mesh> _grassMeshes =
            new Dictionary<Vector2Int, Mesh>(32);

        public void Bind(ChunkGrid grid, TileHeightMap heights = null, float tileSpacing = 0.05f)
        {
            _grid = grid;
            _heights = heights;
            _tileSpacing = tileSpacing < 0f ? 0f : tileSpacing;
            GameEvents.ChunkPlaced += OnChunkPlaced;
        }

        public void SetTileSpacing(float spacing)
        {
            if (spacing < 0f)
                spacing = 0f;
            if (Mathf.Abs(_tileSpacing - spacing) < 1e-5f)
                return;
            _tileSpacing = spacing;
            foreach (var instance in _instances.Values)
            {
                if (instance != null)
                    ApplyFootprints(instance.transform);
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
            ApplyFootprints(instance.transform);
            ApplyTileHeights(instance.transform, coord, slot);
            RebuildGrass(coord, instance.transform, slot);
        }

        void ApplyFootprints(Transform instance)
        {
            for (var i = 0; i < instance.childCount; i++)
            {
                var child = instance.GetChild(i);
                if (!TileHeightVisual.TryParseTileName(child.name, out _, out _))
                    continue;
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
                var renderer = child.GetComponent<MeshRenderer>();
                var mat = ResolveHeightMaterial(layer, renderer != null ? renderer.sharedMaterial : null);
                TileHeightVisual.ApplyPad(child, layer, mat);
            }
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

        void RebuildAllGrass()
        {
            if (_grid == null || grassRendererTemplate == null)
            {
                WarnMissingGrassTemplate();
                return;
            }

            foreach (var pair in _instances)
            {
                if (pair.Value == null || !_grid.TryGet(pair.Key.x, pair.Key.y, out var slot))
                    continue;
                RebuildGrass(pair.Key, pair.Value.transform, slot);
            }
        }

        void RebuildGrass(Vector2Int coord, Transform instance, ChunkSlot slot)
        {
            if (grassRendererTemplate == null || instance == null)
            {
                WarnMissingGrassTemplate();
                return;
            }

            if (!_grassRenderers.TryGetValue(coord, out var renderer) || renderer == null)
            {
                renderer = instance.gameObject.AddComponent<PointGrassRenderer>();
                _grassRenderers[coord] = renderer;
            }

            renderer.enabled = false;
            if (_grassMeshes.TryGetValue(coord, out var previousMesh) && previousMesh != null)
            {
                renderer.baseMesh = null;
                DisposeGrassMesh(previousMesh);
            }

            var mesh = ChunkGrassSurfaceBuilder.Build(
                instance,
                slot.Mask,
                coord,
                slot.Yaw,
                _heights,
                grassPatchPalette,
                grassSurfaceOffset);
            _grassMeshes[coord] = mesh;
            if (mesh == null)
                return;

            CopyTemplateSettings(
                grassRendererTemplate,
                renderer,
                mesh,
                grassPatchPalette != null);
            renderer.enabled = true;
        }

        void WarnMissingGrassTemplate()
        {
            if (_loggedMissingGrassTemplate)
                return;

            _loggedMissingGrassTemplate = true;
            Debug.LogWarning(
                "[GemTD] ChunkBoardView needs a GrassRendererTemplate reference before it can build chunk grass.");
        }

        static void CopyTemplateSettings(
            PointGrassRenderer source,
            PointGrassRenderer target,
            Mesh distributionMesh,
            bool usePaletteTint)
        {
            // The generated top-surface mesh is the only setting that must differ
            // from the authoring template for a runtime chunk.
            target.distSource = PointGrassCommon.DistributionSource.Mesh;
            target.baseMesh = distributionMesh;
            target.terrain = null;
            target.terrainLayers = null;
            target.sceneFilters = null;

            target.bladeType = source.bladeType;
            target.multipleMeshes = source.multipleMeshes;
            target.grassBladeMesh = source.grassBladeMesh;
            target.grassBladeMeshes = CopyArray(source.grassBladeMeshes);
            target.meshDensityValues = CopyArray(source.meshDensityValues);

            target.multipleMaterials = source.multipleMaterials;
            target.material = source.material;
            target.materials = CopyArray(source.materials);

            target.shadowMode = source.shadowMode;
            target.renderLayer = source.renderLayer;

            target.pointCount = source.pointCount;
            target.multiplyByArea = source.multiplyByArea;
            target.pointLODFactor = source.pointLODFactor;
            target.randomiseSeed = source.randomiseSeed;
            target.seed = source.seed;

            target.overwriteNormalDirection = source.overwriteNormalDirection;
            target.forcedNormal = source.forcedNormal;
            target.useDensity = source.useDensity;
            target.densityCutoff = source.densityCutoff;
            target.useLength = source.useLength;
            target.lengthMapping = source.lengthMapping;

            target.projectType = source.projectType;
            target.projectMask = source.projectMask;
            target.boundingBoxOffset = source.boundingBoxOffset;

            // Point Grass reuses source vertex R/G values for density/length.
            // Palette mode reserves those colors for the per-tile tint.
            if (usePaletteTint)
            {
                target.useDensity = false;
                target.useLength = false;
            }
        }

        static T[] CopyArray<T>(T[] source)
        {
            if (source == null)
                return null;

            var copy = new T[source.Length];
            System.Array.Copy(source, copy, source.Length);
            return copy;
        }

        static void DisposeGrassMesh(Mesh mesh)
        {
            if (mesh == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(mesh);
            else
                Object.DestroyImmediate(mesh);
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

            foreach (var renderer in _grassRenderers.Values)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }

            foreach (var mesh in _grassMeshes.Values)
                DisposeGrassMesh(mesh);

            _grassRenderers.Clear();
            _grassMeshes.Clear();
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
