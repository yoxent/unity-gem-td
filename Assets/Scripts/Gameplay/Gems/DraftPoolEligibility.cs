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

        static bool IsPausedSpecial(GemStat stat)
        {
            switch (stat)
            {
                case GemStat.SpreadDegrees:
                case GemStat.Proliferate:
                case GemStat.KnockbackDistance:
                    return true;
                default:
                    return false;
            }
        }
    }
}
