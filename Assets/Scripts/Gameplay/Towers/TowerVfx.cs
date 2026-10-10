using System;
using GemTD.Gameplay.Combat;
using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Prefabs for the tower's own shot. Payload art lives on each <see cref="EffectPayloadDefinition"/>.
    /// </summary>
    [Serializable]
    public sealed class TowerVfx
    {
        public const float CastLifetimeSeconds = 1f;
        public const int UnassignedPayload = -1;

        [Tooltip("Played on the tower when the attack is released.")]
        public EffectView castPrefab;

        [Tooltip("Primary shot while it travels. Warp rise and drop, bolt, payload-nova.")]
        public EffectView flightPrefab;

        [Tooltip("Where the primary shot lands. Warp landing, slam, nova. Empty uses the flight prefab.")]
        public EffectView impactPrefab;

        [Tooltip("Replaces flight while the shot still has chain jumps.")]
        public EffectView chainPrefab;

        public EffectView ResolveFlight(bool chaining)
        {
            if (chaining && chainPrefab != null)
                return chainPrefab;
            return flightPrefab;
        }

        public static EffectView ResolvePayloadView(TowerVfx vfx, in EffectPayloadPlan plan)
        {
            var assigned = AssignedPayloadPrefab(plan);
            if (assigned != null)
                return assigned;

            if (vfx == null)
                return null;

            if (UsesImpactSlot(plan))
                return vfx.impactPrefab != null ? vfx.impactPrefab : vfx.flightPrefab;

            return vfx.flightPrefab;
        }

        static EffectView AssignedPayloadPrefab(in EffectPayloadPlan plan)
        {
            if (plan.PayloadIndex < 0)
                return null;

            if (plan.TravelPattern == EffectPayloadTravelPattern.StationaryPulse)
            {
                if (plan.ImpactPrefab != null)
                    return plan.ImpactPrefab;
                return plan.FlightPrefab;
            }

            return plan.FlightPrefab;
        }

        /// <summary>
        /// Slam, aftershock, nova, and warp used to be their own pools. They are stationary pulses,
        /// and that art now sits on the tower impact slot. A payload row with its own prefab wins first.
        /// </summary>
        static bool UsesImpactSlot(in EffectPayloadPlan plan)
        {
            if (plan.PayloadIndex < 0)
                return true;

            return plan.TravelPattern == EffectPayloadTravelPattern.StationaryPulse;
        }
    }
}
