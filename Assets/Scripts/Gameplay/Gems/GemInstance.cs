namespace GemTD.Gameplay.Gems
{
    public readonly struct GemInstance
    {
        public readonly GemDefinition Def;
        public readonly GemRarity Rarity;

        public GemInstance(GemDefinition def, GemRarity rarity)
        {
            Def = def;
            Rarity = GemRarityUtility.Normalize(rarity);
        }

        public static GemInstance FromDefinition(GemDefinition def)
        {
            return def == null ? default : new GemInstance(def, GemRarity.Normal);
        }

        public bool IsEmpty => Def == null;

        public GemId Id => Def != null ? Def.Id : GemId.None;

        public string DisplayName
        {
            get
            {
                if (Def == null)
                    return "";

                var familyName = !string.IsNullOrEmpty(Def.DisplayName) ? Def.DisplayName : Def.name;
                return GemRarityUtility.Prefix(Rarity) + familyName;
            }
        }
    }
}
