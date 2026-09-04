using UnityEngine;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    public sealed class FallEffectView : EffectView
    {
        [SerializeField] ParticleSystem fallDrop;
        [SerializeField] ParticleSystem[] fallImpacts;

        bool _fallDropPlayed;
        bool _fallImpactPlayed;

        public override bool IsFallEffect => true;

        protected override void OnBind()
        {
            ResetFallVfxFlags();
        }

        protected override void AfterSync()
        {
            SyncFallVfx();
        }

        protected override void OnClear()
        {
            StopFallVfx();
        }

        protected override void ApplyTransform(Vector3 position, Vector3 direction)
        {
            transform.position = position;
            transform.rotation = Quaternion.identity;
        }

        protected override void ApplyColliderState()
        {
            EnsureColliders();
            var enable = Payload == null || !Payload.HasResolvedImpact;
            var colliders = Colliders;
            var defaults = ColliderDefaults;
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null)
                    continue;
                colliders[i].enabled = enable && defaults[i];
            }
        }

        void SyncFallVfx()
        {
            if (fallDrop == null)
                return;

            if (Payload == null || Payload.Plan.TravelPattern != EffectPayloadTravelPattern.FallFromSky)
            {
                StopFallVfx();
                return;
            }

            if (Payload.HasResolvedImpact)
                PlayFallImpact();
            else
                PlayFallDrop();
        }

        void PlayFallDrop()
        {
            if (_fallDropPlayed)
                return;

            _fallDropPlayed = true;
            _fallImpactPlayed = false;
            StopFallImpacts();
            PlayIsolated(fallDrop);
        }

        void PlayFallImpact()
        {
            if (_fallImpactPlayed)
                return;

            _fallImpactPlayed = true;
            _fallDropPlayed = false;
            StopIsolated(fallDrop);
            if (fallImpacts == null)
                return;
            for (var i = 0; i < fallImpacts.Length; i++)
                PlayIsolated(fallImpacts[i]);
        }

        void StopFallVfx()
        {
            ResetFallVfxFlags();
            StopIsolated(fallDrop);
            StopFallImpacts();
        }

        void ResetFallVfxFlags()
        {
            _fallDropPlayed = false;
            _fallImpactPlayed = false;
        }

        void StopFallImpacts()
        {
            if (fallImpacts == null)
                return;
            for (var i = 0; i < fallImpacts.Length; i++)
                StopIsolated(fallImpacts[i]);
        }

        static void PlayIsolated(ParticleSystem system)
        {
            if (system == null)
                return;
            system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.Play(false);
        }

        static void StopIsolated(ParticleSystem system)
        {
            if (system == null)
                return;
            system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
