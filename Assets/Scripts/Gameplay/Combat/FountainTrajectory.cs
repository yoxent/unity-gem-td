using UnityEngine;

namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// Shared fountain arc from origin to landing point. Used by combat runtime and Skill Lab tracer.
    /// </summary>
    public static class FountainTrajectory
    {
        public const int DefaultSampleCount = 8;

        /// <summary>
        /// Position along the arc at normalized time t in [0, 1].
        /// </summary>
        public static Vector3 Evaluate(Vector3 origin, Vector3 landingPoint, float arcHeight, float t)
        {
            if (t <= 0f)
                return origin;
            if (t >= 1f)
                return landingPoint;

            var flat = Vector3.Lerp(origin, landingPoint, t);
            var lift = 4f * arcHeight * t * (1f - t);
            flat.y += lift;
            return flat;
        }

        /// <summary>
        /// Append sampled polyline points from origin to landing (excluding duplicate endpoints).
        /// </summary>
        public static void SamplePolyline(
            Vector3 origin,
            Vector3 landingPoint,
            float arcHeight,
            int sampleCount,
            System.Collections.Generic.List<Vector3> into)
        {
            if (into == null || sampleCount < 1)
                return;

            into.Add(origin);
            for (var i = 1; i < sampleCount; i++)
            {
                var t = i / (float)sampleCount;
                into.Add(Evaluate(origin, landingPoint, arcHeight, t));
            }
            into.Add(landingPoint);
        }
    }
}
