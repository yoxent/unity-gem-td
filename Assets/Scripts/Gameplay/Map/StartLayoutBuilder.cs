using System.Collections.Generic;
using GemTD.Gameplay.Grid;
using UnityEngine;

namespace GemTD.Gameplay.Map
{
    public static class StartLayoutBuilder
    {
        // Open-arm order by difficulty: 1=E, 2=E+S, 3=E+S+N, 4=all.
        static readonly EdgeFlags[] ArmOrder =
        {
            EdgeFlags.East,
            EdgeFlags.South,
            EdgeFlags.North,
            EdgeFlags.West
        };

        public static void Build(ChunkGrid grid, ChunkStampService stamp, PathGraph path,
            GridBoard board, IChunkCatalog catalog, System.Random rng,
            int openArmCount = 1)
        {
            var keep = new Vector2Int(grid.ChunksW / 2, grid.ChunksH / 2);

            if (openArmCount < 1) openArmCount = 1;
            else if (openArmCount > 4) openArmCount = 4;

            var keepPrefab = PickKeep(catalog, openArmCount);
            if (keepPrefab == null)
            {
                Debug.LogError(
                    "[GemTD] No Homebase stamp with " + openArmCount +
                    " opening(s). Assign one keep per difficulty to ChunkTypeCatalog_Homebase.");
                return;
            }

            // One keep per difficulty (opening count). Yaw so openings match
            // ArmOrder (E → S → N → W); painted keeps are South-first.
            var keepYaw = PickKeepYaw(keepPrefab, openArmCount);
            var keepRes = stamp.StampTentative(keep, keepPrefab, keepYaw, path, board);
            stamp.Commit(keep, keepPrefab, keepYaw, keepRes.Mask, grid);
            if (keepRes.Mask.HasHome)
            {
                var home = keepRes.Mask.HomeWorldCell(keep);
                path.SetHome(home.x, home.y);
            }
            if ((keepRes.Mask.OpenEdges & ArmOrder[0]) == 0)
                Debug.LogError("[GemTD] Keep has no mid-edge opening toward the start arm. Paint a path to a mid-edge cell.");

            // One Straight per open arm (Home + N Straights), not a long corridor.
            for (var a = 0; a < openArmCount; a++)
            {
                var dir = ArmOrder[a];
                var offset = OffsetFor(dir);
                var requiredEdge = dir.Opposite();
                var coord = new Vector2Int(keep.x + offset.x, keep.y + offset.y);
                if (!grid.InBounds(coord.x, coord.y)) continue;

                var straight = PickStraight(catalog, rng);
                var yaw = PickYawForEdge(straight, requiredEdge, rng);
                var res = stamp.StampTentative(coord, straight, yaw, path, board);
                stamp.Commit(coord, straight, yaw, res.Mask, grid);
            }

            if (!keepRes.Mask.HasHome)
                Debug.LogError("[GemTD] Keep chunk has no painted home. Enemies leak at PathGraph.Home — paint a home cell on the keep prefab.");
        }

        static Vector2Int OffsetFor(EdgeFlags dir)
        {
            switch (dir)
            {
                case EdgeFlags.North: return new Vector2Int(0, 1);
                case EdgeFlags.South: return new Vector2Int(0, -1);
                case EdgeFlags.East:  return new Vector2Int(1, 0);
                case EdgeFlags.West:  return new Vector2Int(-1, 0);
                default: return Vector2Int.zero;
            }
        }

        // Homebase catalog only — never expand picks. One prefab per opening count.
        static MapChunkStamp PickKeep(IChunkCatalog catalog, int openArmCount)
        {
            var keeps = new List<MapChunkStamp>(4);
            catalog.CopyType(ChunkType.Homebase, keeps);
            for (var i = 0; i < keeps.Count; i++)
            {
                var keep = keeps[i];
                if (keep == null) continue;
                if (keep.GetMask().OpenEdges.Count() == openArmCount)
                    return keep;
            }
            return null;
        }

        static int PickKeepYaw(MapChunkStamp prefab, int openArmCount)
        {
            var required = EdgeFlags.None;
            for (var i = 0; i < openArmCount; i++)
                required |= ArmOrder[i];
            for (var yaw = 0; yaw < 4; yaw++)
                if (prefab.GetRotatedMask(yaw).OpenEdges == required)
                    return yaw;
            for (var yaw = 0; yaw < 4; yaw++)
                if ((prefab.GetRotatedMask(yaw).OpenEdges & ArmOrder[0]) != 0)
                    return yaw;
            return 0;
        }

        // Straight type catalog only so DeadEnd / Corner / T / Cross are never start-arm picks.
        static MapChunkStamp PickStraight(IChunkCatalog catalog, System.Random rng)
        {
            var straights = new List<MapChunkStamp>(16);
            catalog.CopyType(ChunkType.Straight, straights);
            return PickNonNull(straights, rng);
        }

        static MapChunkStamp PickNonNull(List<MapChunkStamp> stamps, System.Random rng)
        {
            var count = 0;
            for (var i = 0; i < stamps.Count; i++)
                if (stamps[i] != null) count++;
            if (count == 0) return null;
            for (;;)
            {
                var pick = stamps[rng.Next(stamps.Count)];
                if (pick != null) return pick;
            }
        }

        static int PickYawForEdge(MapChunkStamp prefab, EdgeFlags requiredEdge, System.Random rng)
        {
            // Collect valid yaws, pick one at random.
            var valid = new int[4];
            var n = 0;
            for (var yaw = 0; yaw < 4; yaw++)
                if ((prefab.GetRotatedMask(yaw).OpenEdges & requiredEdge) != 0)
                    valid[n++] = yaw;
            if (n == 0) return 0; // shouldn't happen for a Straight facing any edge
            return valid[rng.Next(n)];
        }
    }
}
