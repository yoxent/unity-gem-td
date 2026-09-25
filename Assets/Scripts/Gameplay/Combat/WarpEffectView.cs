using UnityEngine;

namespace GemTD.Gameplay.Combat
{
    /// <summary>WarpStrike travel and landing visual. Landing payloads are ground-sited.</summary>
    public sealed class WarpEffectView : EffectView
    {
        [SerializeField] ParticleSystem particles;

        public override bool IsWarpEffect => true;
        protected override bool SitsOnGround => true;
        protected override ParticleSystem AssignedParticles => particles;
    }
}
