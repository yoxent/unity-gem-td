using UnityEngine;
using GemTD.Gameplay.Enemies;

namespace GemTD.Gameplay.Combat
{
    public sealed class ChainLightningEffectView : EffectView
    {
        [SerializeField] ParticleSystem strikeParticles;

        EnemyRuntime _struckTarget;

        public override bool IsChainLightningEffect => true;
        protected override ParticleSystem AssignedParticles => strikeParticles;

        protected override void OnBind()
        {
            base.OnBind();
            _struckTarget = null;
            StripAutoDestroy();
        }

        protected override void AfterSync()
        {
            var target = Runtime != null ? Runtime.Target : null;
            if (target == null || !target.IsAlive)
            {
                StopStrike();
                _struckTarget = null;
                return;
            }

            if (ReferenceEquals(_struckTarget, target))
                return;

            _struckTarget = target;
            PlayStrike();
        }

        protected override void OnClear()
        {
            base.OnClear();
            _struckTarget = null;
            StopStrike();
        }

        protected override void ApplyTransform(Vector3 position, Vector3 direction)
        {
            var target = Runtime != null ? Runtime.Target : null;
            if (target == null || !target.IsAlive)
            {
                base.ApplyTransform(position, direction);
                return;
            }

            transform.position = target.WorldPosition;
            transform.rotation = Quaternion.identity;
        }

        void PlayStrike()
        {
            if (strikeParticles == null)
                return;

            strikeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            strikeParticles.Play(true);
        }

        void StopStrike()
        {
            if (strikeParticles == null)
                return;

            strikeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void StripAutoDestroy()
        {
            if (strikeParticles == null)
                return;

            var behaviours = strikeParticles.GetComponentsInParent<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null || behaviour.GetType().Name != "AllIn1VfxAutoDestroy")
                    continue;

                if (Application.isPlaying)
                    Destroy(behaviour);
                else
                    DestroyImmediate(behaviour);
            }
        }
    }
}
