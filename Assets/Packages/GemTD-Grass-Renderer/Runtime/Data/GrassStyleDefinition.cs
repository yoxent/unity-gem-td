using UnityEngine;
using UnityEngine.Rendering;

namespace GemTD.Grass
{
    [CreateAssetMenu(menuName = "Gem TD/Grass/Style")]
    public class GrassStyleDefinition : ScriptableObject
    {
        const float DefaultClumpsPerSquareUnit = 10f;
        const float DefaultMinScale = 0.9f;
        const float DefaultMaxScale = 1.15f;
        const float DefaultEdgeInset = 0.08f;
        const float DefaultEdgeThinChance = 0.35f;
        const float DefaultCliffDensityMultiplier = 1.15f;
        const float DefaultTowerClearanceRadius = 0.38f;
        const float DefaultColorNoiseScale = 3f;
        const float DefaultColorNoiseStrength = 0.07f;
        const float DefaultWindScale = 2.5f;
        const float DefaultWindSpeed = 0.22f;
        const float DefaultWindStrength = 0.05f;
        const float MinimumNoiseScale = 0.01f;

        static readonly Color DefaultRootColor = new Color(0.28f, 0.39f, 0.21f, 1f);
        static readonly Color DefaultBodyColor = new Color(0.40f, 0.53f, 0.29f, 1f);
        static readonly Color DefaultTipColor = new Color(0.55f, 0.66f, 0.38f, 1f);
        static readonly Vector2 DefaultWindDirection = new Vector2(1f, 0.35f);

        [SerializeField] Mesh[] clumpMeshes;
        [SerializeField] float[] variantWeights;
        [SerializeField] Material material;
        [SerializeField, Min(0f)] float clumpsPerSquareUnit = DefaultClumpsPerSquareUnit;
        [SerializeField] Vector2 scaleRange = new Vector2(DefaultMinScale, DefaultMaxScale);
        [SerializeField, Min(0f)] float edgeInset = DefaultEdgeInset;
        [SerializeField, Range(0f, 1f)] float edgeThinChance = DefaultEdgeThinChance;
        [SerializeField, Min(1f)] float cliffDensityMultiplier = DefaultCliffDensityMultiplier;
        [SerializeField, Min(0f)] float towerClearanceRadius = DefaultTowerClearanceRadius;
        [SerializeField] Color rootColor = DefaultRootColor;
        [SerializeField] Color bodyColor = DefaultBodyColor;
        [SerializeField] Color tipColor = DefaultTipColor;
        [SerializeField, Min(MinimumNoiseScale)] float colorNoiseScale = DefaultColorNoiseScale;
        [SerializeField, Range(0f, 1f)] float colorNoiseStrength = DefaultColorNoiseStrength;
        [SerializeField] Vector2 windDirection = DefaultWindDirection;
        [SerializeField, Min(MinimumNoiseScale)] float windScale = DefaultWindScale;
        [SerializeField, Min(0f)] float windSpeed = DefaultWindSpeed;
        [SerializeField, Min(0f)] float windStrength = DefaultWindStrength;
        [SerializeField] ShadowCastingMode shadowCasting = ShadowCastingMode.Off;
        [SerializeField] bool receiveShadows = true;

        public int VariantCount => clumpMeshes == null ? 0 : clumpMeshes.Length;
        public Material Material => material;
        public float ClumpsPerSquareUnit => clumpsPerSquareUnit;
        public Vector2 ScaleRange => scaleRange;
        public float EdgeInset => edgeInset;
        public float EdgeThinChance => edgeThinChance;
        public float CliffDensityMultiplier => cliffDensityMultiplier;
        public float TowerClearanceRadius => towerClearanceRadius;
        public Color RootColor => rootColor;
        public Color BodyColor => bodyColor;
        public Color TipColor => tipColor;
        public float ColorNoiseScale => colorNoiseScale;
        public float ColorNoiseStrength => colorNoiseStrength;
        public Vector2 WindDirection => windDirection;
        public float WindScale => windScale;
        public float WindSpeed => windSpeed;
        public float WindStrength => windStrength;
        public ShadowCastingMode ShadowCasting => shadowCasting;
        public bool ReceiveShadows => receiveShadows;

        public Mesh GetClumpMesh(int index)
        {
            if (clumpMeshes == null || index < 0 || index >= clumpMeshes.Length)
                return null;

            return clumpMeshes[index];
        }

        public float GetVariantWeight(int index)
        {
            if (index < 0 || index >= VariantCount)
                return 0f;

            if (variantWeights == null || index >= variantWeights.Length)
                return index == 0 ? 1f : 0f;

            var weight = variantWeights[index];
            return IsFinite(weight) ? weight : 0f;
        }

        public GrassLayoutSettings CreateLayoutSettings()
        {
            var settings = new GrassLayoutSettings
            {
                ClumpsPerSquareUnit = clumpsPerSquareUnit,
                MinScale = scaleRange.x,
                MaxScale = scaleRange.y,
                EdgeInset = edgeInset,
                EdgeThinChance = edgeThinChance,
                CliffDensityMultiplier = cliffDensityMultiplier,
                VariantWeights = CloneWeights()
            };

            return settings.Sanitized();
        }

        void OnValidate()
        {
            SanitizeFields();
        }

        void SanitizeFields()
        {
            clumpsPerSquareUnit = SanitizeNonNegative(clumpsPerSquareUnit, 0f);

            var minScale = scaleRange.x;
            var maxScale = scaleRange.y;
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

            scaleRange = new Vector2(minScale, maxScale);
            edgeInset = SanitizeNonNegative(edgeInset, 0f);
            edgeThinChance = SanitizeClamped01(edgeThinChance);
            cliffDensityMultiplier = SanitizeLowerBound(
                cliffDensityMultiplier,
                1f,
                1f);
            towerClearanceRadius = SanitizeNonNegative(towerClearanceRadius, 0f);

            rootColor = SanitizeColor(rootColor);
            bodyColor = SanitizeColor(bodyColor);
            tipColor = SanitizeColor(tipColor);
            colorNoiseScale = SanitizePositive(colorNoiseScale, DefaultColorNoiseScale, MinimumNoiseScale);
            colorNoiseStrength = SanitizeNonNegative(colorNoiseStrength, 0f);
            colorNoiseStrength = Mathf.Clamp01(colorNoiseStrength);

            windDirection = new Vector2(
                SanitizeFinite(windDirection.x, DefaultWindDirection.x),
                SanitizeFinite(windDirection.y, DefaultWindDirection.y));
            windScale = SanitizePositive(windScale, DefaultWindScale, MinimumNoiseScale);
            windSpeed = SanitizeNonNegative(windSpeed, 0f);
            windStrength = SanitizeNonNegative(windStrength, 0f);

            variantWeights = SanitizeWeights(variantWeights);
        }

        float[] CloneWeights()
        {
            if (variantWeights == null)
                return null;

            var clone = new float[variantWeights.Length];
            for (var i = 0; i < clone.Length; i++)
                clone[i] = variantWeights[i];
            return clone;
        }

        static float[] SanitizeWeights(float[] weights)
        {
            if (weights == null)
                return new[] { 1f };

            var sanitized = new float[weights.Length];
            var hasPositiveWeight = false;
            for (var i = 0; i < sanitized.Length; i++)
            {
                sanitized[i] = IsFinite(weights[i]) ? weights[i] : 0f;
                if (sanitized[i] > 0f)
                    hasPositiveWeight = true;
            }

            if (!hasPositiveWeight)
                return new[] { 1f };

            return sanitized;
        }

        static Color SanitizeColor(Color value)
        {
            return new Color(
                SanitizeFinite(value.r, 0f),
                SanitizeFinite(value.g, 0f),
                SanitizeFinite(value.b, 0f),
                SanitizeFinite(value.a, 0f));
        }

        static float SanitizeNonNegative(float value, float fallback)
        {
            if (!IsFinite(value))
                return fallback;

            return Mathf.Max(0f, value);
        }

        static float SanitizePositive(float value, float fallback, float minimum)
        {
            if (!IsFinite(value))
                return fallback;

            return Mathf.Max(minimum, value);
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

        static float SanitizeFinite(float value, float fallback)
        {
            return IsFinite(value) ? value : fallback;
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
