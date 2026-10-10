using UnityEngine;

namespace GemTD.Grass
{
    public enum GrassExclusionShape : byte
    {
        Circle = 0,
        Box = 1
    }

    public readonly struct GrassExclusion
    {
        public readonly GrassExclusionShape Shape;
        public readonly Vector2 LocalCenter;
        public readonly Vector2 Size;

        GrassExclusion(GrassExclusionShape shape, Vector2 localCenter, Vector2 size)
        {
            Shape = shape;
            LocalCenter = localCenter;
            Size = size;
        }

        public static GrassExclusion Circle(Vector2 center, float radius)
        {
            var safeRadius = Mathf.Max(0f, radius);
            return new GrassExclusion(GrassExclusionShape.Circle, center, new Vector2(safeRadius, safeRadius));
        }

        public static GrassExclusion Box(Vector2 center, Vector2 halfExtents)
        {
            return new GrassExclusion(
                GrassExclusionShape.Box,
                center,
                new Vector2(Mathf.Abs(halfExtents.x), Mathf.Abs(halfExtents.y)));
        }
    }
}
