using UnityEngine;

namespace GemTD.Gameplay.Combat
{
    public sealed class SlamEffectView : EffectView
    {
        [SerializeField] ParticleSystem particles;

        public override bool IsSlamEffect => true;
        protected override bool SitsOnGround => true;
        protected override ParticleSystem AssignedParticles => particles;
    }
}
