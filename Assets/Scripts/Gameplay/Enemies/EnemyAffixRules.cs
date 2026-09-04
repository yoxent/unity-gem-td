using GemTD.Gameplay.Combat;

namespace GemTD.Gameplay.Enemies
{
    public static class EnemyAffixRules
    {
        public static bool TryValidate(EnemyRank rank, EnemyAffix[] affixes, out string reason)
        {
            reason = null;
            var count = affixes == null ? 0 : affixes.Length;
            var max = EnemyRankRules.MaxAffixes(rank);
            if (count > max)
            {
                reason = "Too many affixes for rank.";
                return false;
            }

            if (affixes == null)
                return true;

            var seen = 0L;
            for (var i = 0; i < affixes.Length; i++)
            {
                var bit = 1L << (int)affixes[i];
                if ((seen & bit) != 0)
                {
                    reason = "Duplicate affix.";
                    return false;
                }

                seen |= bit;
            }

            return true;
        }

        public static float CurseEffectiveness(EnemyAffix[] affixes)
        {
            if (affixes == null)
                return 1f;

            var unhallowed = false;
            for (var i = 0; i < affixes.Length; i++)
            {
                if (affixes[i] == EnemyAffix.Hexproof)
                    return 0f;
                if (affixes[i] == EnemyAffix.Unhallowed)
                    unhallowed = true;
            }

            return unhallowed ? DamageTypeCombat.UnhallowedCurseEffectiveness : 1f;
        }

        public static bool Contains(EnemyAffix[] affixes, EnemyAffix affix)
        {
            if (affixes == null)
                return false;
            for (var i = 0; i < affixes.Length; i++)
            {
                if (affixes[i] == affix)
                    return true;
            }

            return false;
        }
    }
}
