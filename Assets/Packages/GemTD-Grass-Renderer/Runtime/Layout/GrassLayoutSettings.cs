using UnityEngine;

namespace GemTD.Grass
{
    public struct GrassLayoutSettings
    {
        public float ClumpsPerSquareUnit;
        public float MinScale;
        public float MaxScale;
        public float EdgeInset;
        public float EdgeThinChance;
        public float CliffDensityMultiplier;
        public float[] VariantWeights;

        public GrassLayoutSettings Sanitized()
        {
            var sanitized = this;
            sanitized.ClumpsPerSquareUnit = SanitizeNonNegative(sanitized.ClumpsPerSquareUnit, 0f);

            var minScale = sanitized.MinScale;
            var maxScale = sanitized.MaxScale;
            if (!IsFinitePositive(minScale))
                minScale = 1f;

            if (!IsFinitePositive(maxScale))
                maxScale = minScale;

            if (minScale > maxScale)
            {
                var swap = minScale;
                minScale = maxScale;
                maxScale = swap;
            }

            sanitized.MinScale = minScale;
            sanitized.MaxScale = maxScale;
            sanitized.EdgeInset = SanitizeNonNegative(sanitized.EdgeInset, 0f);
            sanitized.EdgeThinChance = SanitizeClamped01(sanitized.EdgeThinChance);
            sanitized.CliffDensityMultiplier = SanitizeLowerBound(sanitized.CliffDensityMultiplier, 1f, 1f);

            var sanitizedWeights = SanitizeWeights(sanitized.VariantWeights);
            if (sanitizedWeights == null || !HasPositiveWeight(sanitizedWeights))
            {
                sanitized.VariantWeights = new[] { 1f };
            }
            else
            {
                sanitized.VariantWeights = sanitizedWeights;
            }

            return sanitized;
        }

        static float[] SanitizeWeights(float[] weights)
        {
            if (weights == null)
                return null;

            var cloned = new float[weights.Length];
            for (var i = 0; i < cloned.Length; i++)
                cloned[i] = IsFinite(weights[i]) ? weights[i] : 0f;

            return cloned;
        }

        static bool HasPositiveWeight(float[] weights)
        {
            for (var i = 0; i < weights.Length; i++)
            {
                if (weights[i] > 0f)
                    return true;
            }

            return false;
        }

        static float SanitizeNonNegative(float value, float fallback)
        {
            if (!IsFinite(value))
                return fallback;

            return Mathf.Max(0f, value);
        }

        static float SanitizeClamped01(float value)
        {
            if (!IsFinite(value))
                return 0f;

            return Mathf.Clamp01(value);
        }

        static float SanitizeLowerBound(float value, float minimum, float fallback)
        {
            if (!IsFinite(value))
                return fallback;

            return Mathf.Max(minimum, value);
        }

        static bool IsFinitePositive(float value)
        {
            return IsFinite(value) && value > 0f;
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
