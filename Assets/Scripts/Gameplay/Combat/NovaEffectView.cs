using UnityEngine;

namespace GemTD.Gameplay.Combat
{
    public sealed class NovaEffectView : EffectView
    {
        [SerializeField] ParticleSystem particles;

        public override bool IsNovaEffect => true;
        protected override bool SitsOnGround => true;
        protected override ParticleSystem AssignedParticles => particles;
    }
}
