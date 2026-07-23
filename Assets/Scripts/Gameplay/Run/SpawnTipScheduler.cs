using System.Collections.Generic;
using UnityEngine;

namespace GemTD.Gameplay.Run
{
    /// <summary>Round-robin tip picker so wave Count is split across portals, not multiplied.</summary>
    public static class SpawnTipScheduler
    {
        public static Vector2Int Next(IReadOnlyList<Vector2Int> tips, ref int nextIndex)
        {
            var tip = tips[nextIndex % tips.Count];
            nextIndex++;
            return tip;
        }
    }
}
