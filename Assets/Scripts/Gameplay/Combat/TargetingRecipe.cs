using System;

namespace GemTD.Gameplay.Combat
{
    public struct TargetingRecipe : IEquatable<TargetingRecipe>
    {
        public const int SlotCount = 3;
        const int KeyCount = 8;

        public TargetingKey Priority1;
        public TargetingKey Priority2;
        public TargetingKey Priority3;

        public static TargetingRecipe Default => new TargetingRecipe
        {
            Priority1 = TargetingKey.First,
            Priority2 = TargetingKey.First,
            Priority3 = TargetingKey.First
        };

        public TargetingKey Get(int slot)
        {
            switch (slot)
            {
                case 0: return Priority1;
                case 1: return Priority2;
                case 2: return Priority3;
                default: return TargetingKey.First;
            }
        }

        public TargetingRecipe WithCycled(int slot)
        {
            if (slot < 0 || slot >= SlotCount)
                return this;

            var next = (TargetingKey)(((int)Get(slot) + 1) % KeyCount);
            var copy = this;
            switch (slot)
            {
                case 0: copy.Priority1 = next; break;
                case 1: copy.Priority2 = next; break;
                case 2: copy.Priority3 = next; break;
            }
            return copy;
        }

        public bool Equals(TargetingRecipe other) =>
            Priority1 == other.Priority1 &&
            Priority2 == other.Priority2 &&
            Priority3 == other.Priority3;

        public override bool Equals(object obj) => obj is TargetingRecipe other && Equals(other);

        public override int GetHashCode() =>
            ((int)Priority1 * 397) ^ ((int)Priority2 * 397) ^ (int)Priority3;

        public static bool operator ==(TargetingRecipe a, TargetingRecipe b) => a.Equals(b);
        public static bool operator !=(TargetingRecipe a, TargetingRecipe b) => !a.Equals(b);
    }
}
