using UnityEngine;

namespace GemTD.Gameplay.Map
{
    /// <summary>
    /// Cell mask for the map fog sheet. Each occupied chunk clears its square
    /// and a disc centered on that chunk, so the empty ground just outside
    /// the square peeks through. The texture border stays fog so clamping
    /// does not smear a clear edge across the rest of the world.
    /// </summary>
    public static class MapFogMask
    {
        public const float DefaultEdgePeekCells = 3f;

        public static readonly Color32 Fog = new Color32(255, 255, 255, 255);
        public static readonly Color32 Clear = new Color32(255, 255, 255, 0);

        public static float RevealRadius(int chunkSize, float edgePeekCells)
        {
            if (chunkSize < 1)
                chunkSize = 1;
            if (edgePeekCells < 0f)
                edgePeekCells = 0f;
            return chunkSize * 0.5f + edgePeekCells;
        }

        public static int MarginFor(float edgePeekCells)
        {
            if (edgePeekCells < 0f)
                edgePeekCells = 0f;
            return Mathf.CeilToInt(edgePeekCells) + 1;
        }

        public static void FillFog(Color32[] pixels)
        {
            var fog = Fog;
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = fog;
        }

        public static void RevealChunk(
            Color32[] pixels,
            int width,
            int height,
            int chunkX,
            int chunkY,
            int chunkSize,
            int margin,
            float radius)
        {
            if (pixels == null || width <= 0 || height <= 0 || chunkSize <= 0 || margin < 0)
                return;

            var originX = margin + chunkX * chunkSize;
            var originY = margin + chunkY * chunkSize;
            var clear = Clear;
            for (var ly = 0; ly < chunkSize; ly++)
            {
                var y = originY + ly;
                if ((uint)y >= (uint)height)
                    continue;
                var row = y * width;
                for (var lx = 0; lx < chunkSize; lx++)
                {
                    var x = originX + lx;
                    if ((uint)x >= (uint)width || x == 0 || y == 0 || x == width - 1 || y == height - 1)
                        continue;
                    pixels[row + x] = clear;
                }
            }

            if (radius <= 0f || width < 3 || height < 3)
                return;

            var centerX = originX + chunkSize * 0.5f;
            var centerY = originY + chunkSize * 0.5f;
            var radiusSq = radius * radius;
            var minX = Mathf.FloorToInt(centerX - radius);
            var maxX = Mathf.CeilToInt(centerX + radius);
            var minY = Mathf.FloorToInt(centerY - radius);
            var maxY = Mathf.CeilToInt(centerY + radius);
            if (minX < 1) minX = 1;
            if (minY < 1) minY = 1;
            if (maxX > width - 2) maxX = width - 2;
            if (maxY > height - 2) maxY = height - 2;

            for (var y = minY; y <= maxY; y++)
            {
                var row = y * width;
                var dy = y + 0.5f - centerY;
                for (var x = minX; x <= maxX; x++)
                {
                    var dx = x + 0.5f - centerX;
                    if (dx * dx + dy * dy <= radiusSq)
                        pixels[row + x] = clear;
                }
            }
        }
    }

    public readonly struct MapFogLayout
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int Margin;
        public readonly float OriginX;
        public readonly float OriginZ;
        public readonly float WorldWidth;
        public readonly float WorldDepth;
        public readonly float BoardCenterX;
        public readonly float BoardCenterZ;

        public MapFogLayout(int chunksW, int chunksH, int chunkSize, int margin, float cellSize)
        {
            var cellsW = chunksW * chunkSize;
            var cellsH = chunksH * chunkSize;
            Margin = margin;
            Width = cellsW + margin * 2;
            Height = cellsH + margin * 2;
            OriginX = -margin * cellSize;
            OriginZ = -margin * cellSize;
            WorldWidth = Width * cellSize;
            WorldDepth = Height * cellSize;
            BoardCenterX = cellsW * cellSize * 0.5f;
            BoardCenterZ = cellsH * cellSize * 0.5f;
        }

        public Vector2 TextureScale(float coverage)
        {
            return new Vector2(coverage / WorldWidth, coverage / WorldDepth);
        }

        public Vector2 TextureOffset(float coverage)
        {
            var planeMinX = BoardCenterX - coverage * 0.5f;
            var planeMinZ = BoardCenterZ - coverage * 0.5f;
            return new Vector2(
                (planeMinX - OriginX) / WorldWidth,
                (planeMinZ - OriginZ) / WorldDepth);
        }

        public float TextureU(float worldX, float coverage)
        {
            var scale = TextureScale(coverage);
            var offset = TextureOffset(coverage);
            var planeMinX = BoardCenterX - coverage * 0.5f;
            var meshU = (worldX - planeMinX) / coverage;
            return offset.x + meshU * scale.x;
        }

        public float TextureV(float worldZ, float coverage)
        {
            var scale = TextureScale(coverage);
            var offset = TextureOffset(coverage);
            var planeMinZ = BoardCenterZ - coverage * 0.5f;
            var meshV = (worldZ - planeMinZ) / coverage;
            return offset.y + meshV * scale.y;
        }
    }
}
