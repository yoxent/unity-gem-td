using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    /// <summary>
    /// Rows matching the 13 authored gem SOs. Runtime combat uses the assets; EditMode
    /// CreateInstance gems need the same rows bound explicitly.
    /// </summary>
    static class CatalogGemModifiers
    {
        public static void Bind(GemDefinition gem)
        {
            if (gem == null)
                return;
            gem.Modifiers = For(gem.Id);
        }

        public static GemStatModifier[] For(GemId id)
        {
            switch (id)
            {
                case GemId.MultipleProjectiles:
                    return new[]
                    {
                        Mul(GemStat.Damage, 0.8f),
                        Add(GemStat.ProjectileCount, 2f),
                        Set(GemStat.SpreadDegrees, 24f)
                    };
                case GemId.Chain:
                    return new[]
                    {
                        Mul(GemStat.Damage, 0.7f),
                        Add(GemStat.ChainCount, 1f, ProjectileRuntime.DefaultChainHopFalloff)
                    };
                case GemId.Fork:
                    return new[]
                    {
                        Mul(GemStat.Damage, 0.85f),
                        Add(GemStat.ForkCount, 1f)
                    };
                case GemId.IncreasedArea:
                    return new[]
                    {
                        Mul(GemStat.AoeRadius, 1.35f),
                        Mul(GemStat.FireRateMultiplier, 0.9f)
                    };
                case GemId.Pierce:
                    return new[]
                    {
                        Mul(GemStat.Damage, 0.85f),
                        Add(GemStat.PierceCount, 1f)
                    };
                case GemId.ElementalProliferation:
                    return new[]
                    {
                        Mul(GemStat.Damage, 0.75f),
                        Set(GemStat.Proliferate, 1f)
                    };
                case GemId.FasterAttacks:
                    return new[]
                    {
                        Mul(GemStat.AttackSpeedMultiplier, 1.25f)
                    };
                case GemId.SlowerProjectiles:
                    return new[]
                    {
                        Mul(GemStat.Damage, 1.3f),
                        Mul(GemStat.ProjectileSpeedMultiplier, 0.6f)
                    };
                case GemId.Combustion:
                    return new[]
                    {
                        Mul(GemStat.Damage, 1.14f),
                        Set(GemStat.Ignite, 1f)
                    };
                case GemId.AddedFireDamage:
                    return new[]
                    {
                        Mul(GemStat.Damage, 1.31f)
                    };
                case GemId.AddedColdDamage:
                    return new[]
                    {
                        Add(GemStat.Damage, 4f),
                        Set(GemStat.Chill, 1f)
                    };
                case GemId.AddedLightningDamage:
                    return new[]
                    {
                        Add(GemStat.Damage, 4f),
                        Set(GemStat.Shock, 1f)
                    };
                case GemId.Knockback:
                    return new[]
                    {
                        Set(GemStat.KnockbackChance, 0.34f),
                        Set(GemStat.KnockbackDistance, 1f)
                    };
                default:
                    return null;
            }
        }

        static GemStatModifier Set(GemStat stat, float value) =>
            GemStatModifier.Single(stat, RoleModifierOperation.Set, value);

        static GemStatModifier Add(GemStat stat, float value, float falloff = 0f) =>
            GemStatModifier.Single(stat, RoleModifierOperation.Add, value, falloff);

        static GemStatModifier Mul(GemStat stat, float value) =>
            GemStatModifier.Single(stat, RoleModifierOperation.Multiply, value);
    }
}
