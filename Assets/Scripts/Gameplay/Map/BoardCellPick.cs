using UnityEngine;

namespace GemTD.Gameplay.Map
{
    /// <summary>
    /// Board cell under a ray, tested against each pad column.
    /// A y=0 plane shifts along a pitched camera, so raised pads pick the wrong cell.
    /// The ray is in board-local space: cell grid on XZ, pad height on Y.
    /// </summary>
    public static class BoardCellPick
    {
        const float Epsilon = 1e-4f;

        public static bool TryPick(Ray localRay, float cellSize, TileHeightMap heights, out Vector2Int cell)
        {
            cell = default;
            if (heights == null || cellSize <= Epsilon)
                return false;

            var origin = localRay.origin;
            var dir = localRay.direction;
            if (dir.sqrMagnitude <= Epsilon * Epsilon)
                return false;
            dir.Normalize();

            var width = heights.Width;
            var depth = heights.Height;
            if (!TryEnterRect(origin, dir, width * cellSize, depth * cellSize, out var tEnter))
                return false;

            var start = origin + dir * (tEnter + Epsilon);
            var ix = Mathf.FloorToInt(start.x / cellSize);
            var iz = Mathf.FloorToInt(start.z / cellSize);
            if (ix < 0 || iz < 0 || ix >= width || iz >= depth)
                return false;

            var stepX = dir.x > Epsilon ? 1 : (dir.x < -Epsilon ? -1 : 0);
            var stepZ = dir.z > Epsilon ? 1 : (dir.z < -Epsilon ? -1 : 0);
            var tDeltaX = stepX == 0 ? float.PositiveInfinity : cellSize / Mathf.Abs(dir.x);
            var tDeltaZ = stepZ == 0 ? float.PositiveInfinity : cellSize / Mathf.Abs(dir.z);
            var tMaxX = BoundaryT(origin.x, dir.x, ix, stepX, cellSize);
            var tMaxZ = BoundaryT(origin.z, dir.z, iz, stepZ, cellSize);
            if (tMaxX <= tEnter)
                tMaxX += tDeltaX;
            if (tMaxZ <= tEnter)
                tMaxZ += tDeltaZ;

            var maxSteps = width + depth + 2;
            for (var n = 0; n < maxSteps; n++)
            {
                if (ix < 0 || iz < 0 || ix >= width || iz >= depth)
                    return false;

                var top = TileHeightVisual.TopY(heights.Get(ix, iz));
                if (HitsColumn(origin, dir, ix, iz, cellSize, top, tEnter))
                {
                    cell = new Vector2Int(ix, iz);
                    return true;
                }

                if (!Advance(ref ix, ref iz, ref tMaxX, ref tMaxZ, stepX, stepZ, tDeltaX, tDeltaZ))
                    return false;
            }

            return false;
        }

        static bool HitsColumn(
            Vector3 origin, Vector3 dir, int ix, int iz, float cellSize, float top, float tEnter)
        {
            var min = new Vector3(ix * cellSize, 0f, iz * cellSize);
            var max = new Vector3((ix + 1) * cellSize, top, (iz + 1) * cellSize);
            if (!RayAabb(origin, dir, min, max, out var tHit, out var tExit))
                return false;
            return tHit >= tEnter - Epsilon && tExit - tHit >= Epsilon;
        }

        static bool Advance(
            ref int ix, ref int iz, ref float tMaxX, ref float tMaxZ,
            int stepX, int stepZ, float tDeltaX, float tDeltaZ)
        {
            if (stepX != 0 && stepZ != 0 && Mathf.Abs(tMaxX - tMaxZ) <= 1e-5f)
            {
                ix += stepX;
                iz += stepZ;
                tMaxX += tDeltaX;
                tMaxZ += tDeltaZ;
                return true;
            }

            if (stepX != 0 && tMaxX <= tMaxZ)
            {
                ix += stepX;
                tMaxX += tDeltaX;
                return true;
            }

            if (stepZ != 0)
            {
                iz += stepZ;
                tMaxZ += tDeltaZ;
                return true;
            }

            return false;
        }

        static float BoundaryT(float origin, float dir, int index, int step, float cellSize)
        {
            if (step == 0)
                return float.PositiveInfinity;
            var boundary = step > 0 ? (index + 1) * cellSize : index * cellSize;
            return (boundary - origin) / dir;
        }

        static bool TryEnterRect(Vector3 origin, Vector3 dir, float maxX, float maxZ, out float tEnter)
        {
            tEnter = 0f;
            var tMin = 0f;
            var tMax = float.PositiveInfinity;
            if (!Slab(origin.x, dir.x, 0f, maxX, ref tMin, ref tMax))
                return false;
            if (!Slab(origin.z, dir.z, 0f, maxZ, ref tMin, ref tMax))
                return false;
            if (tMax < 0f || tMin > tMax)
                return false;
            tEnter = tMin < 0f ? 0f : tMin;
            return true;
        }

        static bool RayAabb(Vector3 origin, Vector3 dir, Vector3 min, Vector3 max, out float tHit, out float tExit)
        {
            tHit = 0f;
            tExit = 0f;
            var tMin = 0f;
            var tMax = float.PositiveInfinity;
            if (!Slab(origin.x, dir.x, min.x, max.x, ref tMin, ref tMax))
                return false;
            if (!Slab(origin.y, dir.y, min.y, max.y, ref tMin, ref tMax))
                return false;
            if (!Slab(origin.z, dir.z, min.z, max.z, ref tMin, ref tMax))
                return false;
            if (tMax < 0f || tMin > tMax)
                return false;
            tHit = tMin;
            tExit = tMax;
            return true;
        }

        static bool Slab(float origin, float dir, float min, float max, ref float tMin, ref float tMax)
        {
            if (Mathf.Abs(dir) < 1e-8f)
                return origin >= min && origin <= max;

            var t1 = (min - origin) / dir;
            var t2 = (max - origin) / dir;
            if (t1 > t2)
            {
                var swap = t1;
                t1 = t2;
                t2 = swap;
            }

            if (t1 > tMin)
                tMin = t1;
            if (t2 < tMax)
                tMax = t2;
            return tMin <= tMax;
        }
    }
}
