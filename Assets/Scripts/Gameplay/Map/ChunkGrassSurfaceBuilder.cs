using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace GemTD.Gameplay.Map
{
    /// <summary>
    /// Builds the top-only distribution mesh consumed by PointGrassRenderer.
    /// The mesh is local to one instantiated chunk, so the grass renderer can
    /// retain the chunk's normal position, rotation, and culling bounds.
    /// </summary>
    public static class ChunkGrassSurfaceBuilder
    {
        const float CubeTop = 0.5f;

        public static Mesh Build(
            Transform chunkRoot,
            ChunkMask mask,
            Vector2Int chunkCoord,
            int yaw,
            TileHeightMap heights,
            GrassPatchPalette palette,
            float surfaceOffset)
        {
            if (chunkRoot == null)
                return null;

            var vertices = new List<Vector3>(ChunkMask.CellCount * 4);
            var triangles = new List<int>(ChunkMask.CellCount * 6);
            var uvs = new List<Vector2>(ChunkMask.CellCount * 4);
            var colors = new List<Color>(ChunkMask.CellCount * 4);

            for (var i = 0; i < chunkRoot.childCount; i++)
            {
                var tile = chunkRoot.GetChild(i);
                if (!TileHeightVisual.TryParseTileName(tile.name, out var localX, out var localY))
                    continue;
                if (localX < 0 || localX >= ChunkMask.Size || localY < 0 || localY >= ChunkMask.Size)
                    continue;
                var worldLocal = RotateLocalCw(localX, localY, yaw);
                if (mask.IsElevationLocked(worldLocal.x, worldLocal.y))
                    continue;

                var halfX = Mathf.Abs(tile.localScale.x) * CubeTop;
                var halfZ = Mathf.Abs(tile.localScale.z) * CubeTop;
                if (halfX <= 0f || halfZ <= 0f)
                    continue;

                var topY = tile.localPosition.y + tile.localScale.y * CubeTop;
                var worldCell = new Vector2Int(
                    chunkCoord.x * ChunkMask.Size + worldLocal.x,
                    chunkCoord.y * ChunkMask.Size + worldLocal.y);
                if (heights != null)
                {
                    topY = TileHeightVisual.TopY(heights.Get(worldCell.x, worldCell.y));
                }
                var tint = palette != null ? palette.GetTint(worldCell) : Color.white;

                var center = tile.localPosition;
                center.y = topY + surfaceOffset;

                var vertexStart = vertices.Count;
                vertices.Add(new Vector3(center.x - halfX, center.y, center.z - halfZ));
                vertices.Add(new Vector3(center.x - halfX, center.y, center.z + halfZ));
                vertices.Add(new Vector3(center.x + halfX, center.y, center.z + halfZ));
                vertices.Add(new Vector3(center.x + halfX, center.y, center.z - halfZ));

                triangles.Add(vertexStart);
                triangles.Add(vertexStart + 1);
                triangles.Add(vertexStart + 2);
                triangles.Add(vertexStart);
                triangles.Add(vertexStart + 2);
                triangles.Add(vertexStart + 3);

                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(1f, 0f));

                colors.Add(tint);
                colors.Add(tint);
                colors.Add(tint);
                colors.Add(tint);
            }

            if (vertices.Count == 0)
                return null;

            var mesh = new Mesh
            {
                name = $"ChunkGrassSurface_{chunkCoord.x}_{chunkCoord.y}",
                indexFormat = vertices.Count > ushort.MaxValue
                    ? IndexFormat.UInt32
                    : IndexFormat.UInt16
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, true);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Vector2Int RotateLocalCw(int x, int y, int yaw)
        {
            var turns = ((yaw % 4) + 4) % 4;
            for (var i = 0; i < turns; i++)
            {
                var nextX = y;
                var nextY = ChunkMask.Size - 1 - x;
                x = nextX;
                y = nextY;
            }
            return new Vector2Int(x, y);
        }
    }
}
