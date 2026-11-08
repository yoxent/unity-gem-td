namespace GemTD.Gameplay.Gems
{
    public static class DraftPoolEligibility
    {
        public static bool HasResolvedCombatModifier(GemStatModifier[] modifiers)
        {
            if (modifiers == null)
                return false;

            for (var i = 0; i < modifiers.Length; i++)
            {
                if (!IsPausedSpecial(modifiers[i].Stat))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Stats that do not by themselves make a gem worth drafting. This is an
        /// eligibility filter, not a "not implemented" list: crit works, but the
        /// two crit-only gems stay out of the campaign pool for this release.
        /// Do not remove these rows believing crit is unfinished.
        /// </summary>
        static bool IsPausedSpecial(GemStat stat)
        {
            switch (stat)
            {
                case GemStat.SpreadDegrees:
                case GemStat.Proliferate:
                case GemStat.KnockbackDistance:
                case GemStat.CritChance:
                case GemStat.CritMultiplier:
                    return true;
                default:
                    return false;
            }
        }
    }
}
