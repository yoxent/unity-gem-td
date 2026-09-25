using System;
using System.Collections.Generic;
using UnityEngine;

namespace GemTD.Grass
{
    public static class GrassLayoutBuilder
    {
        const uint JitterXSalt = 0x243f6a88u;
        const uint JitterZSalt = 0x85a308d3u;
        const uint YawSalt = 0x13198a2eu;
        const uint ScaleSalt = 0x03707344u;
        const uint ThinSalt = 0xa4093822u;
        const uint VariantSalt = 0x299f31d0u;
        const uint CountSalt = 0x082efa98u;
        const uint CellOffsetSalt = 0xec4e6c89u;

        public static void Build(
            IReadOnlyList<GrassSurfacePatch> patches,
            IReadOnlyList<GrassExclusion> exclusions,
            in GrassLayoutSettings settings,
            int seed,
            List<GrassInstance> output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            output.Clear();

            if (patches == null || patches.Count == 0)
                return;

            var sanitized = settings.Sanitized();

            for (var patchIndex = 0; patchIndex < patches.Count; patchIndex++)
            {
                var patch = patches[patchIndex];
                BuildPatch(patch, exclusions, sanitized, seed, output);
            }
        }

        static void BuildPatch(
            in GrassSurfacePatch patch,
            IReadOnlyList<GrassExclusion> exclusions,
            in GrassLayoutSettings settings,
            int seed,
            List<GrassInstance> output)
        {
            var halfExtents = patch.HalfExtents;
            if (halfExtents.x <= 0f || halfExtents.y <= 0f)
                return;

            var count = GetPatchCount(patch, settings, seed);
            if (count <= 0)
                return;

            var spanX = halfExtents.x * 2f;
            var spanZ = halfExtents.y * 2f;
            var minX = patch.LocalCenter.x - halfExtents.x;
            var maxX = patch.LocalCenter.x + halfExtents.x;
            var minZ = patch.LocalCenter.z - halfExtents.y;
            var maxZ = patch.LocalCenter.z + halfExtents.y;
            var gridSize = Mathf.CeilToInt(Mathf.Sqrt(count));
            var cellCount = gridSize * gridSize;
            var cellSizeX = spanX / gridSize;
            var cellSizeZ = spanZ / gridSize;
            var cellOffset = (int)(Hash(seed, patch.StableKey, -2, CellOffsetSalt) % (uint)cellCount);
            var cellStride = gridSize + 1;

            for (var candidateIndex = 0; candidateIndex < count; candidateIndex++)
            {
                var cellIndex = (cellOffset + (candidateIndex * cellStride)) % cellCount;
                var row = cellIndex / gridSize;
                var column = cellIndex % gridSize;
                var x = minX + (column + UnitFloat(Hash(seed, patch.StableKey, candidateIndex, JitterXSalt))) * cellSizeX;
                var z = minZ + (row + UnitFloat(Hash(seed, patch.StableKey, candidateIndex, JitterZSalt))) * cellSizeZ;

                if (IsRejectedByInsetOrThin(patch, settings, minX, maxX, minZ, maxZ, x, z, seed, candidateIndex))
                    continue;

                if (IsRejectedByExclusion(exclusions, x, z))
                    continue;

                var yaw = 360f * UnitFloat(Hash(seed, patch.StableKey, candidateIndex, YawSalt));
                var scaleT = UnitFloat(Hash(seed, patch.StableKey, candidateIndex, ScaleSalt));
                var scale = Mathf.Lerp(settings.MinScale, settings.MaxScale, scaleT);
                var variant = SelectVariant(settings.VariantWeights, seed, patch.StableKey, candidateIndex);

                output.Add(new GrassInstance(new Vector3(x, patch.LocalCenter.y, z), yaw, scale, variant));
            }
        }

        static int GetPatchCount(in GrassSurfacePatch patch, in GrassLayoutSettings settings, int seed)
        {
            var area = patch.HalfExtents.x * patch.HalfExtents.y * 4f;
            var multiplier = patch.CliffEdges != GrassPatchEdges.None ? settings.CliffDensityMultiplier : 1f;
            var exact = area * settings.ClumpsPerSquareUnit * multiplier;
            if (exact <= 0f)
                return 0;

            var whole = Mathf.FloorToInt(exact);
            var fraction = exact - whole;
            if (fraction <= 0f)
                return whole;

            var roll = UnitFloat(Hash(seed, patch.StableKey, -1, CountSalt));
            return roll < fraction ? whole + 1 : whole;
        }

        static bool IsRejectedByInsetOrThin(
            in GrassSurfacePatch patch,
            in GrassLayoutSettings settings,
            float minX,
            float maxX,
            float minZ,
            float maxZ,
            float x,
            float z,
            int seed,
            int candidateIndex)
        {
            var inset = settings.EdgeInset;
            if (inset <= 0f)
                return false;

            var insetEdges = patch.InsetEdges;
            var inThinBand = false;

            if ((insetEdges & GrassPatchEdges.West) != 0)
            {
                if (x < minX + inset)
                    return true;
                if (x < minX + inset * 2f)
                    inThinBand = true;
            }

            if ((insetEdges & GrassPatchEdges.East) != 0)
            {
                if (x > maxX - inset)
                    return true;
                if (x > maxX - inset * 2f)
                    inThinBand = true;
            }

            if ((insetEdges & GrassPatchEdges.South) != 0)
            {
                if (z < minZ + inset)
                    return true;
                if (z < minZ + inset * 2f)
                    inThinBand = true;
            }

            if ((insetEdges & GrassPatchEdges.North) != 0)
            {
                if (z > maxZ - inset)
                    return true;
                if (z > maxZ - inset * 2f)
                    inThinBand = true;
            }

            if (!inThinBand || settings.EdgeThinChance <= 0f)
                return false;

            return UnitFloat(Hash(seed, patch.StableKey, candidateIndex, ThinSalt)) < settings.EdgeThinChance;
        }

        static bool IsRejectedByExclusion(IReadOnlyList<GrassExclusion> exclusions, float x, float z)
        {
            if (exclusions == null)
                return false;

            for (var i = 0; i < exclusions.Count; i++)
            {
                var exclusion = exclusions[i];
                var dx = x - exclusion.LocalCenter.x;
                var dz = z - exclusion.LocalCenter.y;

                if (exclusion.Shape == GrassExclusionShape.Circle)
                {
                    var radius = exclusion.Size.x;
                    if ((dx * dx) + (dz * dz) <= radius * radius)
                        return true;

                    continue;
                }

                if (Mathf.Abs(dx) <= exclusion.Size.x && Mathf.Abs(dz) <= exclusion.Size.y)
                    return true;
            }

            return false;
        }

        static int SelectVariant(float[] weights, int seed, int stableKey, int candidateIndex)
        {
            var positiveTotal = 0f;
            for (var i = 0; i < weights.Length; i++)
            {
                if (weights[i] > 0f)
                    positiveTotal += weights[i];
            }

            if (positiveTotal <= 0f)
                return 0;

            var pick = UnitFloat(Hash(seed, stableKey, candidateIndex, VariantSalt)) * positiveTotal;
            for (var i = 0; i < weights.Length; i++)
            {
                var weight = weights[i];
                if (weight <= 0f)
                    continue;

                if (pick < weight)
                    return i;

                pick -= weight;
            }

            for (var i = weights.Length - 1; i >= 0; i--)
            {
                if (weights[i] > 0f)
                    return i;
            }

            return 0;
        }

        static uint Hash(int seed, int stableKey, int candidateIndex, uint salt)
        {
            var value = (uint)seed;
            value = Mix(value ^ 0x9e3779b9u);
            value = Mix(value + (uint)stableKey);
            value = Mix(value + (uint)candidateIndex);
            return Mix(value + salt);
        }

        static uint Mix(uint value)
        {
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            return value ^ (value >> 16);
        }

        static float UnitFloat(uint value)
        {
            return (Mix(value) & 0x00ffffffu) / 16777216f;
        }
    }
}
