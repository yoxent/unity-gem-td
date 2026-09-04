using UnityEngine;
using GemTD.Gameplay.Enemies;

namespace GemTD.Gameplay.Combat
{
    public static class PathIntercept
    {
        const int Iterations = 24;

        public static Vector3 Predict(Vector3 origin, float projectileSpeed, EnemyRuntime enemy)
        {
            if (enemy == null)
                return origin;
            if (projectileSpeed <= 0.01f)
                return enemy.WorldPosition;
            if (!enemy.TryGetPositionAfter(0f, out var atZero))
                return enemy.WorldPosition;

            var distZero = (atZero - origin).magnitude;
            if (distZero <= ProjectileRuntime.HitRadius)
                return atZero;

            var lo = 0f;
            var hi = ProjectileRuntime.MaxLifetimeSeconds;
            for (var i = 0; i < Iterations; i++)
            {
                var mid = (lo + hi) * 0.5f;
                enemy.TryGetPositionAfter(mid, out var sample);
                var f = (sample - origin).magnitude - projectileSpeed * mid;
                if (f > 0f)
                    lo = mid;
                else
                    hi = mid;
            }

            var t = (lo + hi) * 0.5f;
            enemy.TryGetPositionAfter(t, out var predicted);
            return predicted;
        }
    }
}
