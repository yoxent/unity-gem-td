using System;
using GemTD.Gameplay.Gems;

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
                GemId.Lmp => new LmpModifier(),
                GemId.Chain => new ChainModifier(),
                GemId.Fork => new ForkModifier(),
                GemId.None => null,
                _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unregistered gem — add a factory case.")
            };
        }
    }
}
