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

    public sealed class FasterAttacksModifier : IAttackModifier
    {
        readonly float _fireRateMultiplier;

        public FasterAttacksModifier(float fireRateMultiplier = 1.25f)
        {
            _fireRateMultiplier = fireRateMultiplier;
        }

        public AttackSpec Modify(AttackSpec spec)
        {
            spec.FireRateMultiplier *= _fireRateMultiplier;
            return spec;
        }
    }

    public sealed class IncreasedAccuracyModifier : IAttackModifier
    {
        readonly float _rangeMultiplier;

        public IncreasedAccuracyModifier(float rangeMultiplier = 1.2f)
        {
            _rangeMultiplier = rangeMultiplier;
        }

        public AttackSpec Modify(AttackSpec spec)
        {
            spec.RangeMultiplier *= _rangeMultiplier;
            return spec;
        }
    }

    public sealed class SlowerProjectilesModifier : IAttackModifier
    {
        readonly float _damageMultiplier;
        readonly float _projectileSpeedMultiplier;

        public SlowerProjectilesModifier(float damageMultiplier = 1.3f, float projectileSpeedMultiplier = 0.6f)
        {
            _damageMultiplier = damageMultiplier;
            _projectileSpeedMultiplier = projectileSpeedMultiplier;
        }

        public AttackSpec Modify(AttackSpec spec)
        {
            spec.Damage *= _damageMultiplier;
            spec.ProjectileSpeedMultiplier *= _projectileSpeedMultiplier;
            return spec;
        }
    }

    public sealed class AttackEchoModifier : IAttackModifier
    {
        readonly int _volleyCount;
        readonly float _damageFactor;

        public AttackEchoModifier(int volleyCount = 2, float damageFactor = 0.6f)
        {
            _volleyCount = volleyCount;
            _damageFactor = damageFactor;
        }

        public AttackSpec Modify(AttackSpec spec)
        {
            spec.EchoVolleyCount = _volleyCount;
            spec.EchoDamageFactor = _damageFactor;
            return spec;
        }
    }
}
