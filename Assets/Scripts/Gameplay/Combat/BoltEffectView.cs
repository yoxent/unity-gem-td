using UnityEngine;

namespace GemTD.Gameplay.Combat
{
    public sealed class BoltEffectView : EffectView
    {
        [SerializeField] ParticleSystem particles;

        protected override ParticleSystem AssignedParticles => particles;
    }
}
