using UnityEngine;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// Resolved runtime snapshot for one payload instance (one fountain bolt, one pulse, etc.).
    /// </summary>
    public struct EffectPayloadPlan
    {
        public EffectPayloadTrigger Trigger;
        public EffectPayloadTravelPattern TravelPattern;
        public EffectPayloadHitPolicy HitPolicy;
        public Vector3 Origin;
        public Vector3 LandingPoint;
        public float DamageMin;
        public float DamageMax;
        public float AoeRadius;
        public float ArcHeight;
        public float DelaySeconds;
        public float IntervalSeconds;
        public int RepeatCount;
        public AilmentTune Ailments;
        public bool Proliferate;
        public float KnockbackChance;
        public float KnockbackDistance;

        public float HorizontalDistance =>
            new Vector3(LandingPoint.x - Origin.x, 0f, LandingPoint.z - Origin.z).magnitude;
    }
}
