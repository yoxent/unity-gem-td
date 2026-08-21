using System;

namespace GemTD.Gameplay.Map
{
    [Flags]
    public enum EdgeFlags : byte
    {
        None = 0, North = 1, East = 2, South = 4, West = 8
    }

    public static class EdgeFlagsExtensions
    {
        public static EdgeFlags Opposite(this EdgeFlags e) => e switch
        {
            EdgeFlags.North => EdgeFlags.South,
            EdgeFlags.South => EdgeFlags.North,
            EdgeFlags.East  => EdgeFlags.West,
            EdgeFlags.West  => EdgeFlags.East,
            _ => EdgeFlags.None
        };

        public static int YawTurnsCW(this EdgeFlags e) => e switch
        {
            EdgeFlags.North => 0,
            EdgeFlags.East => 1,
            EdgeFlags.South => 2,
            EdgeFlags.West => 3,
            _ => 0
        };

        public static int Count(this EdgeFlags e)
        {
            var n = 0;
            if ((e & EdgeFlags.North) != 0) n++;
            if ((e & EdgeFlags.East)  != 0) n++;
            if ((e & EdgeFlags.South) != 0) n++;
            if ((e & EdgeFlags.West)  != 0) n++;
            return n;
        }
    }
}
