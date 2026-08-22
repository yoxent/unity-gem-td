using System;

namespace GemTD.Gameplay.Gems
{
    /// <summary>
    /// Explicit gem → modifier mapping. Greppable; no reflection.
    /// </summary>
    public static class GemModifierFactory
    {
        public static IAttackModifier Create(GemId id)
        {
            return id switch
            {
                GemId.MultipleProjectiles => new LmpModifier(),
                GemId.Chain => new ChainModifier(),
                GemId.Fork => new ForkModifier(),
                GemId.IncreasedArea => new IncreasedAreaModifier(),
                GemId.Pierce => new PierceModifier(),
                GemId.ElementalProliferation => new ElementalProliferationModifier(),
                GemId.FasterAttacks => new FasterAttacksModifier(),
                GemId.SlowerProjectiles => new SlowerProjectilesModifier(),
                GemId.Combustion => new CombustionModifier(),
                GemId.AddedFireDamage => new AddedFireDamageModifier(),
                GemId.AddedColdDamage => new AddedColdDamageModifier(),
                GemId.AddedLightningDamage => new AddedLightningDamageModifier(),
                GemId.Knockback => new KnockbackModifier(),
                GemId.None => null,
                _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unregistered gem — add a factory case.")
            };
        }
    }
}
