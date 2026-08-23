using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Run
{
    public readonly struct DraftOfferCard
    {
        public readonly GemDefinition Gem;
        public readonly TowerDefinition Tower;

        DraftOfferCard(GemDefinition gem, TowerDefinition tower)
        {
            Gem = gem;
            Tower = tower;
        }

        public static DraftOfferCard FromGem(GemDefinition gem) =>
            new DraftOfferCard(gem, null);

        public static DraftOfferCard FromTower(TowerDefinition tower) =>
            new DraftOfferCard(null, tower);

        public bool IsGem => Gem != null;

        public bool IsTower => Tower != null;

        public bool IsFilled => IsGem || IsTower;

        public string DisplayName
        {
            get
            {
                if (Gem != null)
                    return Gem.DisplayName;
                if (Tower != null)
                    return Tower.DisplayName;
                return "";
            }
        }
    }
}
