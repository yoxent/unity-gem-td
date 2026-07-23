using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Enemies;

namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// First targeting: highest path Progress among living enemies in range.
    /// </summary>
    public sealed class TargetSelector
    {
        public bool TrySelectFirst(
            Vector3 towerPos,
            float range,
            List<EnemyRuntime> candidates,
            out EnemyRuntime target)
        {
            target = null;
            if (candidates == null || candidates.Count == 0 || range <= 0f)
                return false;

            var rangeSq = range * range;
            var bestProgress = float.NegativeInfinity;

            for (var i = 0; i < candidates.Count; i++)
            {
                var enemy = candidates[i];
                if (enemy == null || !enemy.IsAlive)
                    continue;

                var delta = enemy.WorldPosition - towerPos;
                if (delta.sqrMagnitude > rangeSq)
                    continue;

                if (enemy.Progress > bestProgress)
                {
                    bestProgress = enemy.Progress;
                    target = enemy;
                }
            }

            return target != null;
        }
    }
}
