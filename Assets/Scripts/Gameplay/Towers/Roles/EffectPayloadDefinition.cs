using System;
using UnityEngine;
using GemTD.Gameplay.Gems;

namespace GemTD.Gameplay.Towers
{
    public enum EffectPayloadTrigger
    {
        OnImpact = 0,
        AfterDelay = 1,
        RepeatingPulse = 2
    }

    public enum EffectPayloadAnchor
    {
        PrimaryTarget = 0,
        ImpactPoint = 1,
        Caster = 2,
        GroundTarget = 3
    }

    public enum EffectPayloadTravelPattern
    {
        Straight = 0,
        Fountain = 1,
        StationaryPulse = 2,
        FallFromSky = 3
    }

    public enum EffectPayloadScatterPattern
    {
        None = 0,
        FixedRadial = 1,
        RandomRing = 2
    }

    public enum EffectPayloadHitPolicy
    {
        PerImpact = 0,
        OncePerPayload = 1
    }

    /// <summary>
    /// Authored secondary effect emitted after a primary delivery resolves.
    /// Shared by Attack, Spell, Curse, Trap, and Mine roles.
    /// </summary>
    [Serializable]
    public sealed class EffectPayloadDefinition
    {
        public EffectPayloadTrigger Trigger = EffectPayloadTrigger.OnImpact;
        public EffectPayloadAnchor Anchor = EffectPayloadAnchor.PrimaryTarget;
        public EffectPayloadTravelPattern TravelPattern = EffectPayloadTravelPattern.Fountain;
        public EffectPayloadScatterPattern ScatterPattern = EffectPayloadScatterPattern.RandomRing;
        public EffectPayloadHitPolicy HitPolicy = EffectPayloadHitPolicy.PerImpact;

        [Tooltip("Support gems apply only when their restriction tags overlap these payload tags.")]
        public GemTag Tags = GemTag.None;

        [Min(0)]
        public int Count = 1;

        [Min(0f)]
        public float DamageMultiplier = 1f;

        [Min(0f)]
        public float AoeRadius;

        [Min(0f)]
        public float MinDistance = 1f;

        [Min(0f)]
        public float MaxDistance = 4f;

        [Min(0f)]
        public float ArcHeight = 1.5f;

        [Min(0f)]
        public float DelaySeconds;

        [Min(0f)]
        public float IntervalSeconds;

        [Min(0)]
        public int RepeatCount;

        public bool IsValid =>
            Count > 0
            && DamageMultiplier > 0f
            && AoeRadius > 0f
            && MaxDistance >= MinDistance;
    }
}
