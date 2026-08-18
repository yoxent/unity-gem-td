using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Enemies;

namespace GemTD.Gameplay.Combat
{
    public sealed class TargetSelector
    {
        const float TieEpsilon = 1e-4f;

        public bool TrySelectFirst(
            Vector3 towerPos,
            float range,
            List<EnemyRuntime> candidates,
            out EnemyRuntime target)
        {
            return TrySelect(TargetingRecipe.Default, towerPos, range, candidates, out target);
        }

        public bool TrySelect(
            TargetingRecipe recipe,
            Vector3 towerPos,
            float range,
            List<EnemyRuntime> candidates,
            out EnemyRuntime target)
        {
            target = null;
            if (candidates == null || candidates.Count == 0 || range <= 0f)
                return false;

            var rangeSq = range * range;
            for (var i = 0; i < candidates.Count; i++)
            {
                var enemy = candidates[i];
                if (enemy == null || !enemy.IsAlive)
                    continue;

                var delta = enemy.WorldPosition - towerPos;
                if (delta.sqrMagnitude > rangeSq)
                    continue;

                if (target == null || IsBetter(enemy, target, recipe))
                    target = enemy;
            }

            return target != null;
        }

        static bool IsBetter(EnemyRuntime challenger, EnemyRuntime incumbent, TargetingRecipe recipe)
        {
            for (var slot = 0; slot < TargetingRecipe.SlotCount; slot++)
            {
                var cmp = CompareKey(recipe.Get(slot), challenger, incumbent);
                if (cmp > 0) return true;
                if (cmp < 0) return false;
            }

            return CompareKey(TargetingKey.First, challenger, incumbent) > 0;
        }

        static int CompareKey(TargetingKey key, EnemyRuntime a, EnemyRuntime b)
        {
            switch (key)
            {
                case TargetingKey.Last:
                    return CompareFloat(b.Progress, a.Progress);
                case TargetingKey.LeastHpPct:
                    return CompareFloat(HpPct(b), HpPct(a));
                case TargetingKey.MostHpPct:
                    return CompareFloat(HpPct(a), HpPct(b));
                case TargetingKey.MostArmor:
                    return a.Armor.CompareTo(b.Armor);
                case TargetingKey.MostShield:
                    return CompareFloat(a.ShieldHp, b.ShieldHp);
                case TargetingKey.Fastest:
                    return CompareFloat(a.CurrentMoveSpeed, b.CurrentMoveSpeed);
                case TargetingKey.Slowest:
                    return CompareFloat(b.CurrentMoveSpeed, a.CurrentMoveSpeed);
                default:
                    return CompareFloat(a.Progress, b.Progress);
            }
        }

        static float HpPct(EnemyRuntime e)
        {
            var max = e.MaxHealth;
            if (max <= 0f)
                return 0f;
            return e.Hp / max;
        }

        static int CompareFloat(float left, float right)
        {
            var d = left - right;
            if (d > TieEpsilon) return 1;
            if (d < -TieEpsilon) return -1;
            return 0;
        }
    }
}
