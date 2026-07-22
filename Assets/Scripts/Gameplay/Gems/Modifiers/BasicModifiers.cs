using GemTD.Gameplay.Combat;

namespace GemTD.Gameplay.Gems
{
    public sealed class LmpModifier : IAttackModifier
    {
        readonly float _damageMultiplier;
        readonly int _extraProjectiles;

        public LmpModifier(float damageMultiplier = 0.8f, int extraProjectiles = 2)
        {
            _damageMultiplier = damageMultiplier;
            _extraProjectiles = extraProjectiles;
        }

        public AttackSpec Modify(AttackSpec spec)
        {
            spec.Damage *= _damageMultiplier;
            spec.ProjectileCount += _extraProjectiles;
            spec.SpreadDegrees = spec.SpreadDegrees <= 0f ? 12f : spec.SpreadDegrees;
            return spec;
        }
    }

    public sealed class ChainModifier : IAttackModifier
    {
        readonly float _damageMultiplier;
        readonly int _chains;

        public ChainModifier(float damageMultiplier = 0.7f, int chains = 2)
        {
            _damageMultiplier = damageMultiplier;
            _chains = chains;
        }

        public AttackSpec Modify(AttackSpec spec)
        {
            spec.Damage *= _damageMultiplier;
            spec.ChainCount += _chains;
            return spec;
        }
    }

    public sealed class ForkModifier : IAttackModifier
    {
        readonly float _damageMultiplier;

        public ForkModifier(float damageMultiplier = 0.85f)
        {
            _damageMultiplier = damageMultiplier;
        }

        public AttackSpec Modify(AttackSpec spec)
        {
            spec.Damage *= _damageMultiplier;
            spec.ForkCount += 1;
            return spec;
        }
    }
}
