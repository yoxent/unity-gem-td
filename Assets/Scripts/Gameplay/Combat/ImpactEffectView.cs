using UnityEngine;

namespace GemTD.Gameplay.Combat
{
    public sealed class ImpactEffectView : EffectView
    {
        public const float LifetimeSeconds = 1.05f;

        [SerializeField] ParticleSystem impactParticles;

        float _age;

        public bool IsFinished => _age >= LifetimeSeconds;
        protected override ParticleSystem AssignedParticles => impactParticles;

        public void Begin(Vector3 position)
        {
            _age = 0f;
            SnapTo(position);
            BoltEffectView.StripAutoDestroy(impactParticles);
        }

        public void Tick(float dt)
        {
            if (dt > 0f)
                _age += dt;
        }

        protected override void OnClear()
        {
            base.OnClear();
            _age = LifetimeSeconds;
        }

        protected override void ApplyTransform(Vector3 position, Vector3 direction)
        {
            transform.position = position;
            transform.rotation = Quaternion.identity;
        }
    }
}
