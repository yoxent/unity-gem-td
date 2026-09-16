using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Run
{
    public readonly struct DraftOfferCard
    {
        public readonly GemInstance Gem;
        public readonly TowerDefinition Tower;

        DraftOfferCard(GemInstance gem, TowerDefinition tower)
        {
            Gem = gem;
            Tower = tower;
        }

        public static DraftOfferCard FromGem(GemInstance gem) =>
            new DraftOfferCard(gem, null);

        public static DraftOfferCard FromTower(TowerDefinition tower) =>
            new DraftOfferCard(default, tower);

        public bool IsGem => !Gem.IsEmpty;
        public bool IsTower => Tower != null;
        public bool IsFilled => IsGem || IsTower;

        public string DisplayName =>
            IsGem ? Gem.DisplayName : IsTower ? Tower.DisplayName : "";

        public string Description
        {
            get
            {
                if (IsGem)
                    return Gem.Def != null ? Gem.Def.Description ?? "" : "";
                if (IsTower)
                    return Tower.Description ?? "";
                return "";
            }
        }
    }
}
