using UnityEngine;

namespace GemTD.Gameplay.Map
{
    public static class TileHeightVisual
    {
        public const float PathScaleY = 0.1f;
        public const float PadLift = 0.15f;
        public const float Step = 0.4f;
        public const float MinFootprint = 0.05f;

        public static float TileScaleXz(float cellSize, float spacing)
        {
            var xz = cellSize - spacing;
            return xz < MinFootprint ? MinFootprint : xz;
        }

        public static void ApplyFootprint(Transform tile, float cellSize, float spacing)
        {
            if (tile == null)
                return;

            var xz = TileScaleXz(cellSize, spacing);
            var scale = tile.localScale;
            scale.x = xz;
            scale.z = xz;
            tile.localScale = scale;
        }

        static readonly Color[] LayerAlbedo =
        {
            new Color(0.42f, 0.46f, 0.42f, 1f),
            new Color(0.55f, 0.60f, 0.55f, 1f),
            new Color(0.74f, 0.80f, 0.74f, 1f)
        };

        public static float ScaleY(byte layer) => PathScaleY + PadLift + layer * Step;

        public static float LocalPosY(byte layer) => (ScaleY(layer) - PathScaleY) * 0.5f;

        public const string PadPrefix = "PadHeight";
        public const int PadTopSalt = 0x51A7C3E1;
        public const int PadSideSalt = unchecked((int)0xC3E251A7);

        /// <summary>Layer 0/1/2 are cliff tiers height 1/2/3. Tops match the Kenney blocks.</summary>
        public static int PadHeight(byte layer)
        {
            if (layer >= 2) return 3;
            return layer + 1;
        }

        public static string PadChildName(byte layer) => PadPrefix + PadHeight(layer);

        /// <summary>Stable index in <c>[0, count)</c>. Different salts decorrelate two lists of the same length.</summary>
        public static int RollIndex(int count, int x, int y, int salt)
        {
            if (count <= 0)
                return -1;
            if (count == 1)
                return 0;

            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)salt * 1442695041u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (int)(h % (uint)count);
            }
        }

        public static float TopY(byte layer)
        {
            if (layer >= 2) return 1f;
            if (layer == 1) return 0.5f;
            return 0.25f;
        }

        public static bool HasPad(Transform tile)
        {
            if (tile == null)
                return false;
            for (var i = 0; i < tile.childCount; i++)
            {
                if (tile.GetChild(i).name.StartsWith(PadPrefix))
                    return true;
            }
            return false;
        }

        /// <summary>Shows the baked cliff for this layer. False when the tile has no pad children.</summary>
        public static bool TryActivatePad(Transform tile, byte layer)
        {
            if (!HasPad(tile))
                return false;

            var target = PadChildName(layer);
            for (var i = 0; i < tile.childCount; i++)
            {
                var child = tile.GetChild(i);
                if (!child.name.StartsWith(PadPrefix))
                    continue;
                child.gameObject.SetActive(child.name == target);
            }
            return true;
        }

        /// <summary>Writes top into slot 0 and sides into slot 1 on the active pad. Null leaves that slot.</summary>
        public static void ApplyPadLook(Transform tile, Material top, Material sides)
        {
            if (tile == null || (top == null && sides == null))
                return;

            for (var i = 0; i < tile.childCount; i++)
            {
                var child = tile.GetChild(i);
                if (!child.gameObject.activeSelf || !child.name.StartsWith(PadPrefix))
                    continue;
                ApplyLook(child, top, sides);
            }
        }

        static void ApplyLook(Transform pad, Material top, Material sides)
        {
            var renderers = pad.GetComponentsInChildren<MeshRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                var mats = renderer.sharedMaterials;
                if (mats == null || mats.Length == 0)
                    continue;
                if (top != null)
                    mats[0] = top;
                if (sides != null && mats.Length > 1)
                    mats[1] = sides;
                renderer.sharedMaterials = mats;
            }
        }

        public static bool TryParseTileName(string name, out int x, out int y)
        {
            x = 0;
            y = 0;
            if (name == null || name.Length < 8)
                return false;
            if (!name.StartsWith("Tile_"))
                return false;
            var split = name.IndexOf('_', 5);
            if (split < 0)
                return false;
            return int.TryParse(name.Substring(5, split - 5), out x)
                && int.TryParse(name.Substring(split + 1), out y);
        }

        public static void ApplyPad(Transform tile, byte layer, Material mat)
        {
            if (tile == null)
                return;

            var scale = tile.localScale;
            scale.y = ScaleY(layer);
            tile.localScale = scale;
            var pos = tile.localPosition;
            pos.y = LocalPosY(layer);
            tile.localPosition = pos;

            if (mat == null)
                return;
            var renderer = tile.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = mat;
        }

        public static Material[] CreateLayerMaterials(Material source)
        {
            var mats = new Material[3];
            for (var i = 0; i < 3; i++)
            {
                mats[i] = source != null ? new Material(source) : new Material(Shader.Find("Sprites/Default"));
                mats[i].name = $"Chunk_Tower_H{i}_runtime";
                mats[i].enableInstancing = true;
                WriteAlbedo(mats[i], LayerAlbedo[i]);
            }

            return mats;
        }

        public static Color ReadAlbedo(Material mat)
        {
            if (mat == null)
                return Color.black;
            if (mat.HasProperty("_BaseColor"))
                return mat.GetColor("_BaseColor");
            if (mat.HasProperty("_Color"))
                return mat.GetColor("_Color");
            return mat.color;
        }

        static void WriteAlbedo(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
        }
    }
}
