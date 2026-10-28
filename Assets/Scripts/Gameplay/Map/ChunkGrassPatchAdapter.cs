using System;
using System.Collections.Generic;
using UnityEngine;
using GemTD.Grass;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Map
{
    public static class ChunkGrassPatchAdapter
    {
        static readonly Vector2Int[] LocalCardinalDeltas =
        {
            new Vector2Int(0, -1),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(-1, 0)
        };

        static readonly GrassPatchEdges[] LocalEdges =
        {
            GrassPatchEdges.South,
            GrassPatchEdges.East,
            GrassPatchEdges.North,
            GrassPatchEdges.West
        };

        public static void BuildPatches(
            Transform chunkRoot,
            Vector2Int chunkCoord,
            in ChunkSlot slot,
            ChunkGrid grid,
            TileHeightMap heights,
            float surfaceOffset,
            List<GrassSurfacePatch> output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            output.Clear();
            if (chunkRoot == null)
                return;

            var safeSurfaceOffset = SanitizeNonNegative(surfaceOffset);
            for (var i = 0; i < chunkRoot.childCount; i++)
            {
                var tile = chunkRoot.GetChild(i);
                if (!TryGetEligibleTile(
                        tile,
                        chunkCoord,
                        in slot,
                        out var worldLocal,
                        out var worldCell))
                {
                    continue;
                }

                var halfX = Mathf.Abs(tile.localScale.x) * 0.5f;
                var halfZ = Mathf.Abs(tile.localScale.z) * 0.5f;
                if (halfX <= 0f || halfZ <= 0f)
                    continue;

                var topY = heights != null
                    ? TileHeightVisual.TopY(heights.Get(worldCell.x, worldCell.y))
                    : tile.localPosition.y + Mathf.Abs(tile.localScale.y) * 0.5f;
                var insetEdges = GrassPatchEdges.None;
                var cliffEdges = GrassPatchEdges.None;
                ClassifyEdges(
                    chunkCoord,
                    worldCell,
                    in slot,
                    grid,
                    ref insetEdges,
                    ref cliffEdges);

                output.Add(new GrassSurfacePatch(
                    new Vector3(
                        tile.localPosition.x,
                        topY + safeSurfaceOffset,
                        tile.localPosition.z),
                    new Vector2(halfX, halfZ),
                    insetEdges,
                    cliffEdges,
                    StableKey(worldCell)));
            }
        }

        public static void BuildExclusions(
            Transform chunkRoot,
            Vector2Int chunkCoord,
            in ChunkSlot slot,
            TowerPlacementService placement,
            float radius,
            List<GrassExclusion> output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            output.Clear();
            if (chunkRoot == null || placement == null)
                return;

            var safeRadius = SanitizeNonNegative(radius);
            for (var i = 0; i < chunkRoot.childCount; i++)
            {
                var tile = chunkRoot.GetChild(i);
                if (!TryGetEligibleTile(
                        tile,
                        chunkCoord,
                        in slot,
                        out _,
                        out var worldCell))
                {
                    continue;
                }

                if (!placement.IsOccupied(worldCell))
                    continue;

                output.Add(GrassExclusion.Circle(
                    new Vector2(tile.localPosition.x, tile.localPosition.z),
                    safeRadius));
            }
        }

        public static Bounds CalculateLocalBounds(IReadOnlyList<GrassSurfacePatch> patches)
        {
            if (patches == null || patches.Count == 0)
                return new Bounds(Vector3.zero, Vector3.zero);

            var first = patches[0];
            var bounds = new Bounds(first.LocalCenter, Vector3.zero);
            for (var i = 0; i < patches.Count; i++)
            {
                var patch = patches[i];
                var halfX = Mathf.Abs(patch.HalfExtents.x);
                var halfZ = Mathf.Abs(patch.HalfExtents.y);
                var min = new Vector3(
                    patch.LocalCenter.x - halfX,
                    patch.LocalCenter.y,
                    patch.LocalCenter.z - halfZ);
                var max = new Vector3(
                    patch.LocalCenter.x + halfX,
                    patch.LocalCenter.y,
                    patch.LocalCenter.z + halfZ);
                bounds.Encapsulate(min);
                bounds.Encapsulate(max);
            }

            return bounds;
        }

        static bool TryGetEligibleTile(
            Transform tile,
            Vector2Int chunkCoord,
            in ChunkSlot slot,
            out Vector2Int worldLocal,
            out Vector2Int worldCell)
        {
            worldLocal = default;
            worldCell = default;
            if (tile == null || !TileHeightVisual.TryParseTileName(tile.name, out var localX, out var localY))
                return false;
            if (localX < 0 || localX >= ChunkMask.Size || localY < 0 || localY >= ChunkMask.Size)
                return false;

            worldLocal = RotateLocalCw(localX, localY, slot.Yaw);
            if (slot.Mask.IsElevationLocked(worldLocal.x, worldLocal.y))
                return false;

            worldCell = new Vector2Int(
                chunkCoord.x * ChunkMask.Size + worldLocal.x,
                chunkCoord.y * ChunkMask.Size + worldLocal.y);
            return true;
        }

        static void ClassifyEdges(
            Vector2Int chunkCoord,
            Vector2Int worldCell,
            in ChunkSlot slot,
            ChunkGrid grid,
            ref GrassPatchEdges insetEdges,
            ref GrassPatchEdges cliffEdges)
        {
            var turns = NormalizeTurns(slot.Yaw);
            for (var i = 0; i < LocalCardinalDeltas.Length; i++)
            {
                var worldDelta = RotateDeltaCw(LocalCardinalDeltas[i], turns);
                var neighborCell = worldCell + worldDelta;
                if (!TryGetPlacedCell(
                        chunkCoord,
                        neighborCell,
                        in slot,
                        grid,
                        out var neighborSlot,
                        out var neighborLocal))
                {
                    cliffEdges |= LocalEdges[i];
                    continue;
                }

                if (neighborSlot.Mask.IsElevationLocked(neighborLocal.x, neighborLocal.y))
                    insetEdges |= LocalEdges[i];
            }
        }

        static bool TryGetPlacedCell(
            Vector2Int currentChunk,
            Vector2Int worldCell,
            in ChunkSlot currentSlot,
            ChunkGrid grid,
            out ChunkSlot neighborSlot,
            out Vector2Int neighborLocal)
        {
            var chunkX = FloorDiv(worldCell.x, ChunkMask.Size);
            var chunkY = FloorDiv(worldCell.y, ChunkMask.Size);
            neighborLocal = new Vector2Int(
                worldCell.x - chunkX * ChunkMask.Size,
                worldCell.y - chunkY * ChunkMask.Size);

            if (chunkX == currentChunk.x && chunkY == currentChunk.y)
            {
                neighborSlot = currentSlot;
                return true;
            }

            if (grid != null && grid.TryGet(chunkX, chunkY, out neighborSlot))
                return true;

            neighborSlot = default;
            return false;
        }

        static Vector2Int RotateLocalCw(int x, int y, int yaw)
        {
            var turns = NormalizeTurns(yaw);
            for (var i = 0; i < turns; i++)
            {
                var nextX = y;
                var nextY = ChunkMask.Size - 1 - x;
                x = nextX;
                y = nextY;
            }

            return new Vector2Int(x, y);
        }

        static Vector2Int RotateDeltaCw(Vector2Int delta, int turns)
        {
            for (var i = 0; i < turns; i++)
                delta = new Vector2Int(delta.y, -delta.x);
            return delta;
        }

        static int NormalizeTurns(int yaw) => ((yaw % 4) + 4) % 4;

        static int FloorDiv(int value, int divisor)
        {
            var quotient = value / divisor;
            var remainder = value % divisor;
            if (remainder < 0)
                quotient--;
            return quotient;
        }

        static float SanitizeNonNegative(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                return 0f;
            return value;
        }

        static int StableKey(Vector2Int worldCell)
        {
            unchecked
            {
                uint value = (uint)worldCell.x * 0x9e3779b1u;
                value ^= (uint)worldCell.y + 0x85ebca6bu;
                value ^= value >> 16;
                value *= 0x7feb352du;
                value ^= value >> 15;
                value *= 0x846ca68bu;
                return (int)(value ^ (value >> 16));
            }
        }
    }
}
