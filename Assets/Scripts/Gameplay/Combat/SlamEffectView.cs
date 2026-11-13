using UnityEngine;

namespace GemTD.Gameplay.Combat
{
    public sealed class SlamEffectView : EffectView
    {
        [SerializeField] ParticleSystem particles;

        protected override bool SitsOnGround => true;
        protected override ParticleSystem AssignedParticles => particles;
    }
}
