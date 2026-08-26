using GemTD.Gameplay.Combat;

namespace GemTD.Gameplay.Gems
{
    public sealed class GemDefinitionModifier : ISkillModifier
    {
        readonly GemStatModifier[] _modifiers;

        public GemDefinitionModifier(GemDefinition gem)
        {
            _modifiers = gem != null ? gem.Modifiers : null;
        }

        public SkillSpec Modify(SkillSpec spec)
        {
            return GemStatResolver.Apply(spec, _modifiers);
        }
    }
}
