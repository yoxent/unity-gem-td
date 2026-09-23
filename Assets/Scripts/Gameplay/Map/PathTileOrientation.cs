namespace GemTD.Gameplay.Map
{
    /// <summary>
    /// Picks a path-tile piece and clockwise yaw from a cell's openings.
    /// Piece openings are the edges open at identity rotation.
    /// </summary>
    public static class PathTileOrientation
    {
        public static EdgeFlags OpeningsAt(ChunkMask mask, int x, int y)
        {
            if (!mask.IsPath(x, y))
                return EdgeFlags.None;

            var edges = EdgeFlags.None;
            if (NeighborOpen(mask, x, y, 0, 1, EdgeFlags.North)) edges |= EdgeFlags.North;
            if (NeighborOpen(mask, x, y, 1, 0, EdgeFlags.East)) edges |= EdgeFlags.East;
            if (NeighborOpen(mask, x, y, 0, -1, EdgeFlags.South)) edges |= EdgeFlags.South;
            if (NeighborOpen(mask, x, y, -1, 0, EdgeFlags.West)) edges |= EdgeFlags.West;
            return edges;
        }

        /// <summary>
        /// First piece whose identity openings match <paramref name="required"/> after a clockwise yaw.
        /// Straight matches yaw 0 before yaw 2. Cross matches yaw 0.
        /// </summary>
        public static bool TryResolve(EdgeFlags required, EdgeFlags[] identityOpenings, out int pieceIndex, out int yaw)
        {
            pieceIndex = 0;
            yaw = 0;
            if (required == EdgeFlags.None || identityOpenings == null)
                return false;

            for (var i = 0; i < identityOpenings.Length; i++)
            {
                var open = identityOpenings[i];
                if (open == EdgeFlags.None)
                    continue;
                for (var turns = 0; turns < 4; turns++)
                {
                    if (open.RotatedCW(turns) != required)
                        continue;
                    pieceIndex = i;
                    yaw = turns;
                    return true;
                }
            }

            return false;
        }

        static bool NeighborOpen(ChunkMask mask, int x, int y, int dx, int dy, EdgeFlags outward)
        {
            var nx = x + dx;
            var ny = y + dy;
            if ((uint)nx < ChunkMask.Size && (uint)ny < ChunkMask.Size)
                return mask.IsPath(nx, ny);

            return x == OpeningX(outward) && y == OpeningY(outward);
        }

        static int OpeningX(EdgeFlags edge)
        {
            if (edge == EdgeFlags.West) return 0;
            if (edge == EdgeFlags.East) return ChunkMask.Size - 1;
            return ChunkMask.Mid;
        }

        static int OpeningY(EdgeFlags edge)
        {
            if (edge == EdgeFlags.South) return 0;
            if (edge == EdgeFlags.North) return ChunkMask.Size - 1;
            return ChunkMask.Mid;
        }
    }
}
