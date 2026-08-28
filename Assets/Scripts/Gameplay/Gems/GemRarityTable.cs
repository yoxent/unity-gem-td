using System;
using UnityEngine;

namespace GemTD.Gameplay.Gems
{
    [CreateAssetMenu(menuName = "Gem TD/Gem Rarity Table", fileName = "GemRarityTable")]
    public sealed class GemRarityTable : ScriptableObject
    {
        [Min(0f)] public float LesserWeight = 60f;
        [Min(0f)] public float NormalWeight = 30f;
        [Min(0f)] public float GreaterWeight = 10f;

        public GemRarity Roll(System.Random rng)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            var lesser = SafeWeight(LesserWeight);
            var normal = SafeWeight(NormalWeight);
            var greater = SafeWeight(GreaterWeight);
            var total = lesser + normal + greater;
            if (total <= 0f)
                return GemRarity.Normal;

            var roll = (float)rng.NextDouble() * total;
            if (roll < lesser)
                return GemRarity.Lesser;
            roll -= lesser;
            if (roll < normal)
                return GemRarity.Normal;
            return GemRarity.Greater;
        }

        static float SafeWeight(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value) ? value : 0f;
        }
    }
}
