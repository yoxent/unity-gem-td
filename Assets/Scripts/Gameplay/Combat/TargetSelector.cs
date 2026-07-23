using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Enemies;

namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// Nordhold-style targeting: First, Last, Closest, Strongest among living enemies in range.
    /// </summary>
    public sealed class TargetSelector
    {
        public bool TrySelectFirst(
            Vector3 towerPos,
            float range,
            List<EnemyRuntime> candidates,
            out EnemyRuntime target)
        {
            return TrySelect(TargetingMode.First, towerPos, range, candidates, out target);
        }

        public bool TrySelect(
            TargetingMode mode,
            Vector3 towerPos,
            float range,
            List<EnemyRuntime> candidates,
            out EnemyRuntime target)
        {
            target = null;
            if (candidates == null || candidates.Count == 0 || range <= 0f)
                return false;

            var rangeSq = range * range;
            var bestProgress = mode == TargetingMode.Last
                ? float.PositiveInfinity
                : float.NegativeInfinity;
            var bestDistSq = float.PositiveInfinity;
            var bestHp = float.NegativeInfinity;

            for (var i = 0; i < candidates.Count; i++)
            {
                var enemy = candidates[i];
                if (enemy == null || !enemy.IsAlive)
                    continue;

                var delta = enemy.WorldPosition - towerPos;
                var distSq = delta.sqrMagnitude;
                if (distSq > rangeSq)
                    continue;

                switch (mode)
                {
                    case TargetingMode.First:
                        if (enemy.Progress > bestProgress)
                        {
                            bestProgress = enemy.Progress;
                            target = enemy;
                        }
                        break;

                    case TargetingMode.Last:
                        if (enemy.Progress < bestProgress)
                        {
                            bestProgress = enemy.Progress;
                            target = enemy;
                        }
                        break;

                    case TargetingMode.Closest:
                        if (distSq < bestDistSq)
                        {
                            bestDistSq = distSq;
                            target = enemy;
                        }
                        break;

                    case TargetingMode.Strongest:
                        if (enemy.Hp > bestHp ||
                            (enemy.Hp == bestHp && enemy.Progress > bestProgress))
                        {
                            bestHp = enemy.Hp;
                            bestProgress = enemy.Progress;
                            target = enemy;
                        }
                        break;
                }
            }

            return target != null;
        }
    }
}
