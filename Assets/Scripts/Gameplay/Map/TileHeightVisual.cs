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

        public static float TopY(byte layer) => ScaleY(layer) * 0.5f + LocalPosY(layer);

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
