using UnityEngine;

namespace GemTD.Gameplay.Combat
{
    public sealed class AftershockEffectView : EffectView
    {
        [SerializeField] ParticleSystem particles;

        public override bool IsAftershockEffect => true;
        protected override bool SitsOnGround => true;
        protected override ParticleSystem AssignedParticles => particles;
    }
}
