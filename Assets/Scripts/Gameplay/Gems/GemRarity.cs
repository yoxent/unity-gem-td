namespace GemTD.Gameplay.Gems
{
    public enum GemRarity
    {
        Normal = 0,
        Lesser = 1,
        Greater = 2
    }

    public static class GemRarityUtility
    {
        public static GemRarity Normalize(GemRarity rarity)
        {
            switch (rarity)
            {
                case GemRarity.Lesser:
                case GemRarity.Greater:
                    return rarity;
                case GemRarity.Normal:
                default:
                    return GemRarity.Normal;
            }
        }

        public static string Prefix(GemRarity rarity)
        {
            switch (Normalize(rarity))
            {
                case GemRarity.Lesser:
                    return "Lesser ";
                case GemRarity.Greater:
                    return "Greater ";
                case GemRarity.Normal:
                default:
                    return "";
            }
        }
    }
}
