using UnityEditor;
using UnityEngine;
using GemTD.Gameplay.Map;

namespace GemTD.Editor
{
    /// <summary>Writes a <see cref="PathTileSet"/> into chunk prefab path cells. Editor only.</summary>
    public static class PathTileBaker
    {
        public const string ChildName = "PathTile";

        static readonly ChunkType[] CatalogTypes =
        {
            ChunkType.DeadEnd,
            ChunkType.Straight,
            ChunkType.Corner,
            ChunkType.TJunction,
            ChunkType.Cross,
            ChunkType.Homebase
        };

        public static int Apply(PathTileSet set, MapChunkStamp stamp)
        {
            if (set == null || stamp == null)
                return 0;

            var mask = stamp.GetMask();
            var scale = set.UniformScale;
            var dressed = 0;
            var root = stamp.transform;
            for (var i = 0; i < root.childCount; i++)
            {
                var tile = root.GetChild(i);
                if (!TileHeightVisual.TryParseTileName(tile.name, out var x, out var y))
                    continue;

                ClearPieces(tile);
                var renderer = tile.GetComponent<MeshRenderer>();
                if (!mask.IsPath(x, y))
                {
                    if (DressPad(set, tile, renderer, mask.IsElevationLocked(x, y)))
                        dressed++;
                    continue;
                }

                var openings = PathTileOrientation.OpeningsAt(mask, x, y);
                if (!set.TryResolve(openings, out var prefab, out var yaw))
                {
                    if (renderer != null)
                        renderer.enabled = true;
                    Debug.LogWarning(
                        "[PathTileSet] No piece for " + stamp.name + " " + tile.name + " openings " + openings + ".");
                    continue;
                }

                if (renderer != null)
                    renderer.enabled = false;
                tile.localScale = new Vector3(scale, scale, scale);

                var piece = PrefabUtility.InstantiatePrefab(prefab, tile) as GameObject;
                if (piece == null)
                    piece = Object.Instantiate(prefab, tile);
                piece.name = ChildName;
                var pieceTransform = piece.transform;
                pieceTransform.localPosition = Vector3.zero;
                pieceTransform.localRotation = Quaternion.Euler(0f, yaw * 90f, 0f);
                pieceTransform.localScale = Vector3.one;
                StripColliders(piece);
                dressed++;
            }

            return dressed;
        }

        public static void ApplyToCatalog(PathTileSet set)
        {
            if (set == null)
                return;
            var catalog = set.Catalog;
            if (catalog == null)
            {
                Debug.LogError("[PathTileSet] Assign a Chunk Catalog before applying.");
                return;
            }

            var chunks = 0;
            var dressed = 0;
            for (var t = 0; t < CatalogTypes.Length; t++)
            {
                var typeCatalog = catalog.CatalogFor(CatalogTypes[t]);
                if (typeCatalog == null)
                    continue;
                var stamps = typeCatalog.Stamps;
                for (var i = 0; i < stamps.Count; i++)
                {
                    var stamp = stamps[i];
                    if (stamp == null)
                        continue;
                    var path = AssetDatabase.GetAssetPath(stamp.gameObject);
                    if (string.IsNullOrEmpty(path))
                        continue;

                    var root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        var loaded = root.GetComponent<MapChunkStamp>();
                        dressed += Apply(set, loaded);
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        chunks++;
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[PathTileSet] Dressed " + dressed + " tiles across " + chunks + " chunks.");
        }

        static bool DressPad(PathTileSet set, Transform tile, MeshRenderer renderer, bool elevationLocked)
        {
            var tierCount = elevationLocked ? 1 : 3;
            var placed = 0;
            for (var height = 1; height <= tierCount; height++)
            {
                var prefab = set.PadPrefab(height);
                if (prefab == null)
                {
                    Debug.LogWarning("[PathTileSet] Missing cliff prefab for height " + height + ".");
                    continue;
                }

                var piece = PrefabUtility.InstantiatePrefab(prefab, tile) as GameObject;
                if (piece == null)
                    piece = Object.Instantiate(prefab, tile);
                piece.name = TileHeightVisual.PadPrefix + height;
                var pieceTransform = piece.transform;
                pieceTransform.localPosition = Vector3.zero;
                pieceTransform.localRotation = Quaternion.identity;
                pieceTransform.localScale = Vector3.one;
                StripColliders(piece);
                piece.SetActive(height == 1);
                placed++;
            }

            if (placed == 0)
            {
                if (renderer != null)
                    renderer.enabled = true;
                return false;
            }

            if (renderer != null)
                renderer.enabled = false;
            tile.localScale = Vector3.one;
            var pos = tile.localPosition;
            pos.y = 0f;
            tile.localPosition = pos;
            return true;
        }

        static void ClearPieces(Transform tile)
        {
            for (var i = tile.childCount - 1; i >= 0; i--)
            {
                var child = tile.GetChild(i);
                if (child.name == ChildName || child.name.StartsWith(TileHeightVisual.PadPrefix))
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        static void StripColliders(GameObject piece)
        {
            var colliders = piece.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
                Object.DestroyImmediate(colliders[i]);
        }
    }
}
