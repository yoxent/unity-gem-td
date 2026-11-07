namespace GemTD.Gameplay.Towers
{
    public readonly struct TowerRosterCaps
    {
        public static TowerRosterCaps Default => new TowerRosterCaps(5, 2, 2);

        public int MaxDamaging { get; }
        public int MaxCurse { get; }
        public int MaxAura { get; }
        public int MaxSlots { get; }

        public TowerRosterCaps(int maxDamaging, int maxCurse, int maxAura)
        {
            MaxDamaging = maxDamaging < 0 ? 0 : maxDamaging;
            MaxCurse = maxCurse < 0 ? 0 : maxCurse;
            MaxAura = maxAura < 0 ? 0 : maxAura;
            MaxSlots = MaxDamaging + MaxCurse + MaxAura;
        }

        public int Cap(TowerRosterCategory category)
        {
            switch (category)
            {
                case TowerRosterCategory.Curse:
                    return MaxCurse;
                case TowerRosterCategory.Aura:
                    return MaxAura;
                default:
                    return MaxDamaging;
            }
        }
    }
}
