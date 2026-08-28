using GemTD.Gameplay.Combat;

namespace GemTD.Gameplay.Gems
{
    public sealed class GemDefinitionModifier : ISkillModifier
    {
        readonly GemStatModifier[] _modifiers;
        readonly GemRarity _rarity;

        public GemDefinitionModifier(GemInstance gem)
        {
            _modifiers = gem.Def != null ? gem.Def.Modifiers : null;
            _rarity = GemRarityUtility.Normalize(gem.Rarity);
        }

        public GemDefinitionModifier(GemDefinition gem)
            : this(GemInstance.FromDefinition(gem))
        {
        }

        public SkillSpec Modify(SkillSpec spec)
        {
            return GemStatResolver.Apply(spec, _modifiers, _rarity);
        }
    }
}
