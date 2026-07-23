using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Enemies;

namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// Domain projectile: seeks target, applies damage on hit, optional Chain bounce.
    /// </summary>
    public sealed class ProjectileRuntime
    {
        const float HitRadius = 0.15f;

        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }
        public EnemyRuntime Target { get; private set; }
        public float Damage { get; private set; }
        public int ChainRemaining { get; private set; }
        public float Speed { get; private set; }
        public float ChainRange { get; private set; }
        public float AoeRadius { get; private set; }
        public bool IsActive { get; private set; }

        public void Init(
            Vector3 origin,
            Vector3 direction,
            EnemyRuntime target,
            float damage,
            int chainCount,
            float speed,
            float chainRange,
            float aoeRadius = 0f)
        {
            Position = origin;
            Direction = direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector3.forward;
            Target = target;
            Damage = damage;
            ChainRemaining = chainCount;
            Speed = speed;
            ChainRange = chainRange;
            AoeRadius = aoeRadius > 0f ? aoeRadius : 0f;
            IsActive = true;
        }

        public void Deactivate() => IsActive = false;

        /// <summary>
        /// Advances flight. Returns false when the projectile should be removed.
        /// </summary>
        public bool Tick(float dt, List<EnemyRuntime> livingCandidates)
        {
            if (!IsActive)
                return false;

            if (Target == null || !Target.IsAlive)
            {
                IsActive = false;
                return false;
            }

            if (dt <= 0f)
                return true;

            var toTarget = Target.WorldPosition - Position;
            var dist = toTarget.magnitude;
            var step = Speed * dt;

            if (dist <= HitRadius || step >= dist)
            {
                Position = Target.WorldPosition;
                OnHit(livingCandidates);
                return IsActive;
            }

            Direction = toTarget / dist;
            Position += Direction * step;
            return true;
        }

        void OnHit(List<EnemyRuntime> livingCandidates)
        {
            var hit = Target;
            hit.ApplyDamage(Damage);

            if (AoeRadius > 0f && livingCandidates != null)
            {
                var radiusSq = AoeRadius * AoeRadius;
                var hitPos = hit.WorldPosition;
                for (var i = 0; i < livingCandidates.Count; i++)
                {
                    var enemy = livingCandidates[i];
                    if (enemy == null || !enemy.IsAlive || ReferenceEquals(enemy, hit))
                        continue;

                    if ((enemy.WorldPosition - hitPos).sqrMagnitude <= radiusSq)
                        enemy.ApplyDamage(Damage);
                }
            }

            if (ChainRemaining <= 0)
            {
                IsActive = false;
                return;
            }

            var next = FindNearestOther(Position, hit, ChainRange, livingCandidates);
            if (next == null)
            {
                IsActive = false;
                return;
            }

            ChainRemaining--;
            Target = next;
            var aim = next.WorldPosition - Position;
            if (aim.sqrMagnitude > 1e-8f)
                Direction = aim.normalized;
        }

        static EnemyRuntime FindNearestOther(
            Vector3 from,
            EnemyRuntime current,
            float range,
            List<EnemyRuntime> candidates)
        {
            if (candidates == null || range <= 0f)
                return null;

            var rangeSq = range * range;
            EnemyRuntime best = null;
            var bestDistSq = float.PositiveInfinity;

            for (var i = 0; i < candidates.Count; i++)
            {
                var enemy = candidates[i];
                if (enemy == null || !enemy.IsAlive || ReferenceEquals(enemy, current))
                    continue;

                var distSq = (enemy.WorldPosition - from).sqrMagnitude;
                if (distSq > rangeSq || distSq >= bestDistSq)
                    continue;

                bestDistSq = distSq;
                best = enemy;
            }

            return best;
        }
    }
}
