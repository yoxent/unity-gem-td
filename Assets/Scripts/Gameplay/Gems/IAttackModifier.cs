using GemTD.Gameplay.Combat;

namespace GemTD.Gameplay.Gems
{
    public interface IAttackModifier
    {
        AttackSpec Modify(AttackSpec spec);
    }
}
