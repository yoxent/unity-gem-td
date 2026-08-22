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
            spec.SpreadDegrees = spec.SpreadDegrees <= 0f ? 24f : spec.SpreadDegrees;
            return spec;
        }
    }

    public sealed class ChainModifier : IAttackModifier
    {
        readonly float _damageMultiplier;
        readonly int _chains;

        public ChainModifier(float damageMultiplier = 0.7f, int chains = 1)
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

    public sealed class IncreasedAreaModifier : IAttackModifier
    {
        readonly float _aoeMultiplier;
        readonly float _fireRateMultiplier;

        public IncreasedAreaModifier(float aoeMultiplier = 1.35f, float fireRateMultiplier = 0.9f)
        {
            _aoeMultiplier = aoeMultiplier;
            _fireRateMultiplier = fireRateMultiplier;
        }

        public AttackSpec Modify(AttackSpec spec)
        {
            spec.AoeRadius *= _aoeMultiplier;
            spec.FireRateMultiplier *= _fireRateMultiplier;
            return spec;
        }
    }

    public sealed class PierceModifier : IAttackModifier
    {
        readonly float _damageMultiplier;

        public PierceModifier(float damageMultiplier = 0.85f)
        {
            _damageMultiplier = damageMultiplier;
        }

        public AttackSpec Modify(AttackSpec spec)
        {
            spec.Damage *= _damageMultiplier;
            spec.Pierce = true;
            return spec;
        }
    }

    public sealed class ElementalProliferationModifier : IAttackModifier
    {
        readonly float _damageMultiplier;

        public ElementalProliferationModifier(float damageMultiplier = 0.75f)
        {
            _damageMultiplier = damageMultiplier;
        }

        public AttackSpec Modify(AttackSpec spec)
        {
            spec.Damage *= _damageMultiplier;
            spec.Proliferate = true;
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

    public sealed class CombustionModifier : IAttackModifier
    {
        readonly float _moreMultiplier;

        public CombustionModifier(float moreMultiplier = 1.14f)
        {
            _moreMultiplier = moreMultiplier;
        }

        public AttackSpec Modify(AttackSpec spec)
        {
            spec.Damage *= _moreMultiplier;
            spec.Ignite = true;
            return spec;
        }
    }

    public sealed class AddedFireDamageModifier : IAttackModifier
    {
        readonly float _extraFraction;

        public AddedFireDamageModifier(float extraFraction = 0.31f)
        {
            _extraFraction = extraFraction;
        }

        public AttackSpec Modify(AttackSpec spec)
        {
            spec.Damage += spec.Damage * _extraFraction;
            return spec;
        }
    }

    public sealed class AddedColdDamageModifier : IAttackModifier
    {
        readonly float _added;

        public AddedColdDamageModifier(float added = 4f)
        {
            _added = added;
        }

        public AttackSpec Modify(AttackSpec spec)
        {
            spec.Damage += _added;
            spec.Chill = true;
            return spec;
        }
    }

    public sealed class AddedLightningDamageModifier : IAttackModifier
    {
        readonly float _added;

        public AddedLightningDamageModifier(float added = 4f)
        {
            _added = added;
        }

        public AttackSpec Modify(AttackSpec spec)
        {
            spec.Damage += _added;
            spec.Shock = true;
            return spec;
        }
    }

    public sealed class KnockbackModifier : IAttackModifier
    {
        readonly float _chance;
        readonly float _distance;

        public KnockbackModifier(float chance = 0.34f, float distance = 1f)
        {
            _chance = chance;
            _distance = distance;
        }

        public AttackSpec Modify(AttackSpec spec)
        {
            if (_chance > spec.KnockbackChance)
                spec.KnockbackChance = _chance;
            if (_distance > spec.KnockbackDistance)
                spec.KnockbackDistance = _distance;
            return spec;
        }
    }
}
