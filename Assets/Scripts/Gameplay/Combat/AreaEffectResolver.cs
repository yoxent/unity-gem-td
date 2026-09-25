using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// Shared circle AoE target collection for projectile hits and effect payloads.
    /// </summary>
    public static class AreaEffectResolver
    {
        public static void CollectCircle(
            Vector3 center,
            float radius,
            IReadOnlyList<EnemyRuntime> living,
            List<EnemyRuntime> into,
            EffectPayloadHitPolicy hitPolicy,
            HashSet<EnemyRuntime> oncePerPayloadSeen = null)
        {
            if (into == null || radius <= 0f || living == null)
                return;

            var radiusSq = radius * radius;
            for (var i = 0; i < living.Count; i++)
            {
                var enemy = living[i];
                if (enemy == null || !enemy.IsAlive)
                    continue;

                if ((enemy.WorldPosition - center).sqrMagnitude > radiusSq)
                    continue;

                if (hitPolicy == EffectPayloadHitPolicy.OncePerPayload && oncePerPayloadSeen != null)
                {
                    if (!oncePerPayloadSeen.Add(enemy))
                        continue;
                }

                into.Add(enemy);
            }
        }

        public static void CollectCircleExcludingPrimary(
            Vector3 center,
            float radius,
            EnemyRuntime primary,
            IReadOnlyList<EnemyRuntime> living,
            List<EnemyRuntime> into)
        {
            if (into == null || radius <= 0f || living == null)
                return;

            var radiusSq = radius * radius;
            for (var i = 0; i < living.Count; i++)
            {
                var enemy = living[i];
                if (enemy == null || !enemy.IsAlive || ReferenceEquals(enemy, primary))
                    continue;

                if ((enemy.WorldPosition - center).sqrMagnitude <= radiusSq)
                    into.Add(enemy);
            }
        }
    }
}
