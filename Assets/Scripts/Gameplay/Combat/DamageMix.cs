using System;
using UnityEngine;

namespace GemTD.Gameplay.Combat
{
    [Serializable]
    public struct DamageTypeShare
    {
        public DamageType Type;
        [Tooltip("1–100. Non-empty mix must sum to 100.")]
        public int Percent;
    }

    public static class DamageMix
    {
        public const int TypeCount = 5;

        public static bool IsEmpty(DamageTypeShare[] shares)
        {
            return shares == null || shares.Length == 0;
        }

        public static bool TryValidate(DamageTypeShare[] shares, out string reason)
        {
            reason = null;
            if (IsEmpty(shares))
                return true;

            var seen = 0;
            var sum = 0;
            for (var i = 0; i < shares.Length; i++)
            {
                var p = shares[i].Percent;
                if (p < 1 || p > 100)
                {
                    reason = "Each share percent must be 1–100.";
                    return false;
                }

                var bit = 1 << (int)shares[i].Type;
                if ((seen & bit) != 0)
                {
                    reason = "Duplicate damage type in mix.";
                    return false;
                }

                seen |= bit;
                sum += p;
            }

            if (sum != 100)
            {
                reason = "Mix percents must sum to 100.";
                return false;
            }

            return true;
        }

        public static void ToFractions(
            DamageTypeShare[] shares,
            out float physical,
            out float fire,
            out float cold,
            out float lightning,
            out float chaos)
        {
            physical = 0f;
            fire = 0f;
            cold = 0f;
            lightning = 0f;
            chaos = 0f;
            if (IsEmpty(shares))
                return;

            if (!TryValidate(shares, out _))
            {
                var sum = 0;
                for (var i = 0; i < shares.Length; i++)
                    sum += Mathf.Max(0, shares[i].Percent);
                if (sum <= 0)
                    return;
                for (var i = 0; i < shares.Length; i++)
                    Add(shares[i].Type, shares[i].Percent / (float)sum, ref physical, ref fire, ref cold, ref lightning, ref chaos);
                return;
            }

            for (var i = 0; i < shares.Length; i++)
                Add(shares[i].Type, shares[i].Percent / 100f, ref physical, ref fire, ref cold, ref lightning, ref chaos);
        }

        static void Add(
            DamageType type,
            float fraction,
            ref float physical,
            ref float fire,
            ref float cold,
            ref float lightning,
            ref float chaos)
        {
            switch (type)
            {
                case DamageType.Physical: physical += fraction; break;
                case DamageType.Fire: fire += fraction; break;
                case DamageType.Cold: cold += fraction; break;
                case DamageType.Lightning: lightning += fraction; break;
                case DamageType.Chaos: chaos += fraction; break;
            }
        }
    }
}
