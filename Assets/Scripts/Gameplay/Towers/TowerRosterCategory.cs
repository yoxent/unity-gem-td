namespace GemTD.Gameplay.Towers
{
    public enum TowerRosterCategory
    {
        Damaging = 0,
        Curse = 1,
        Aura = 2
    }

    public static class TowerRosterCategoryRules
    {
        public static TowerRosterCategory Of(TowerDefinition def)
        {
            if (def == null)
                return TowerRosterCategory.Damaging;

            if (def.HasRole<AuraRoleDefinition>()
                && !def.HasRole<AttackRoleDefinition>()
                && !def.HasRole<TrapRoleDefinition>()
                && !def.HasRole<MineRoleDefinition>())
                return TowerRosterCategory.Aura;

            if (def.HasRole<CurseRoleDefinition>() && !def.HasRole<AttackRoleDefinition>())
                return TowerRosterCategory.Curse;

            return TowerRosterCategory.Damaging;
        }
    }
}
