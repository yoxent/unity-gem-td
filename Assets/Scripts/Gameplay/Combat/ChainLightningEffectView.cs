using UnityEngine;

namespace GemTD.Gameplay.Combat
{
    public sealed class ChainLightningEffectView : EffectView
    {
        static readonly float[] WrinkleOffsets =
        {
            0f,
            0.65f,
            -0.45f,
            0.75f,
            -0.6f,
            0.4f,
            0f
        };

        const int SegmentCount = 6;

        [SerializeField] ParticleSystem lightningParticles;
        [SerializeField] ParticleSystemRenderer lightningRenderer;
        [SerializeField] float wrinkleAmplitude = 0.16f;
        [SerializeField] float beamThickness = 0.12f;
        [SerializeField] float beamStretch = 1f;

        readonly ParticleSystem.Particle[] _beamParticles = new ParticleSystem.Particle[SegmentCount];

        public override bool IsChainLightningEffect => true;
        protected override ParticleSystem AssignedParticles => lightningParticles;

        protected override void OnBind()
        {
            base.OnBind();
            ConfigureParticleBeam();
            ClearParticleBeam();
        }

        protected override void AfterSync()
        {
            // SetParticles owns this beam. PlayAssigned would StopEmittingAndClear it.
        }

        protected override void OnClear()
        {
            base.OnClear();
            ClearParticleBeam();
        }

        protected override void ApplyTransform(Vector3 position, Vector3 direction)
        {
            if (Runtime == null || Runtime.Target == null || !Runtime.Target.IsAlive)
            {
                ClearParticleBeam();
                base.ApplyTransform(position, direction);
                return;
            }

            var start = Runtime.ChainStart;
            var end = Runtime.Target.WorldPosition;
            var beamHeight = (start.y + end.y) * 0.5f;
            start.y = beamHeight;
            end.y = beamHeight;
            var delta = end - start;
            var length = delta.magnitude;
            if (length <= 0.001f)
            {
                ClearParticleBeam();
                return;
            }

            transform.position = (start + end) * 0.5f;
            transform.rotation = Quaternion.identity;

            if (lightningParticles == null)
                return;

            var forward = delta / length;
            var side = Vector3.Cross(forward, Vector3.up);
            if (side.sqrMagnitude <= 0.001f)
                side = Vector3.Cross(forward, Vector3.right);
            side.Normalize();

            for (var i = 0; i < SegmentCount; i++)
            {
                var startT = i / (float)SegmentCount;
                var endT = (i + 1) / (float)SegmentCount;
                var startPoint = BeamPoint(start, end, side, startT, wrinkleAmplitude);
                var endPoint = BeamPoint(start, end, side, endT, wrinkleAmplitude);
                var particle = _beamParticles[i];
                particle.position = ((startPoint + endPoint) * 0.5f) - transform.position;
                particle.velocity = endPoint - startPoint;
                particle.startLifetime = 1f;
                particle.remainingLifetime = 1f;
                particle.startSize = beamThickness;
                particle.startColor = Color.white;
                _beamParticles[i] = particle;
            }

            if (!lightningParticles.isPlaying)
                lightningParticles.Play(false);

            lightningParticles.SetParticles(_beamParticles, SegmentCount);
        }

        void ConfigureParticleBeam()
        {
            if (lightningParticles != null)
            {
                var main = lightningParticles.main;
                main.loop = true;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.simulationSpeed = 0f;
                main.startLifetime = 1f;
                main.startSpeed = 0f;
                main.maxParticles = SegmentCount;

                var emission = lightningParticles.emission;
                emission.enabled = false;
            }

            if (lightningRenderer != null)
            {
                lightningRenderer.renderMode = ParticleSystemRenderMode.Stretch;
                lightningRenderer.velocityScale = 1f;
                lightningRenderer.lengthScale = beamStretch;
            }
        }

        void ClearParticleBeam()
        {
            if (lightningParticles != null)
                lightningParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        static Vector3 BeamPoint(
            Vector3 start,
            Vector3 end,
            Vector3 side,
            float t,
            float amplitude)
        {
            var point = Vector3.Lerp(start, end, t);
            if (t > 0f && t < 1f)
            {
                var wrinkleIndex = Mathf.Clamp(Mathf.RoundToInt(t * (WrinkleOffsets.Length - 1)), 1, WrinkleOffsets.Length - 2);
                point += side * (WrinkleOffsets[wrinkleIndex] * Mathf.Sin(t * Mathf.PI) * amplitude);
            }

            return point;
        }
    }
}
