using GemTD.Gameplay.Combat;

namespace GemTD.Gameplay.Gems
{
    public interface ISkillModifier
    {
        SkillSpec Modify(SkillSpec spec);
    }
}
