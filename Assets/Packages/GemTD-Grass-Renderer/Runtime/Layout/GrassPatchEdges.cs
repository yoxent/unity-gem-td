using System;

namespace GemTD.Grass
{
    [Flags]
    public enum GrassPatchEdges : byte
    {
        None = 0,
        West = 1,
        East = 2,
        South = 4,
        North = 8
    }
}
