namespace GemTD.Gameplay.Map
{
    /// <summary>
    /// Machine key for stamp identity. Not shown to players.
    /// Encode is one pose. Canonical is the lexicographically smallest of the four
    /// yaws so uniqueness can ignore rotation while generate still uses a locked pose.
    /// </summary>
    public static class ChunkMaskId
    {
        public const char Path = 'o';
        public const char Tower = 'x';
        public const char LockedTower = 'k';

        public static string Encode(ChunkMask mask)
        {
            var chars = new char[ChunkMask.CellCount];
            for (var y = 0; y < ChunkMask.Size; y++)
            {
                for (var x = 0; x < ChunkMask.Size; x++)
                {
                    var i = y * ChunkMask.Size + x;
                    if (mask.IsPath(x, y))
                        chars[i] = Path;
                    else if (mask.IsElevationLocked(x, y))
                        chars[i] = LockedTower;
                    else
                        chars[i] = Tower;
                }
            }
            return new string(chars);
        }

        public static string Canonical(ChunkMask mask)
        {
            var best = Encode(mask);
            for (var yaw = 1; yaw < 4; yaw++)
            {
                var id = Encode(mask.Rotated(yaw));
                if (string.CompareOrdinal(id, best) < 0)
                    best = id;
            }
            return best;
        }
    }
}
