using System;

namespace GemTD.Gameplay.Enemies
{
    public static class EnemySplit
    {
        public static EnemyAffix[] BuildChildAffixes(
            EnemyAffix[] parent,
            EnemyRank childRank,
            Func<int, int> pickDropIndex)
        {
            var scratch = new EnemyAffix[parent == null ? 0 : parent.Length];
            var n = 0;
            if (parent != null)
            {
                for (var i = 0; i < parent.Length; i++)
                {
                    if (parent[i] == EnemyAffix.Splitting)
                        continue;
                    scratch[n++] = parent[i];
                }
            }

            var max = EnemyRankRules.MaxAffixes(childRank);
            while (n > max)
            {
                var drop = pickDropIndex != null ? pickDropIndex(n) : 0;
                if (drop < 0)
                    drop = 0;
                if (drop >= n)
                    drop = n - 1;
                for (var i = drop; i < n - 1; i++)
                    scratch[i] = scratch[i + 1];
                n--;
            }

            var result = new EnemyAffix[n];
            for (var i = 0; i < n; i++)
                result[i] = scratch[i];
            return result;
        }
    }
}
