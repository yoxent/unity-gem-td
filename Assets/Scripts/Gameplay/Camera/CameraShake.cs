using UnityEngine;
using GemTD.Core;

namespace GemTD.Gameplay.CameraControl
{
    /// <summary>
    /// Low-amplitude trauma shake with burst, timed, and looping envelopes.
    /// Transient requests take the stronger remaining amplitude instead of stacking.
    /// A continuous loop is a separate layer so fire/hit bursts can spike over a rumble.
    /// </summary>
    public sealed class CameraShake
    {
        float _defaultIntensity;
        float _maxIntensity;
        float _defaultDuration;
        float _defaultHoldDuration;
        float _frequency;
        float _falloffDistance;
        float _noiseT;
        float _seed;

        CameraShakeKind _transientKind;
        float _transientAmplitude;
        float _transientDuration;
        float _transientElapsed;
        bool _transientHasWorldPosition;
        Vector3 _transientWorldPosition;

        bool _loopActive;
        float _loopAmplitude;
        bool _loopHasWorldPosition;
        Vector3 _loopWorldPosition;
        float _loopFadeAmplitude;
        float _loopFadeDuration;
        float _loopFadeElapsed;
        bool _loopFadeHasWorldPosition;
        Vector3 _loopFadeWorldPosition;

        public CameraShake(
            float defaultIntensity = CameraShakeRequest.DefaultIntensity,
            float maxIntensity = CameraShakeRequest.MaxIntensity,
            float defaultDuration = CameraShakeRequest.DefaultDuration,
            float frequency = CameraShakeRequest.DefaultFrequency,
            float falloffDistance = CameraShakeRequest.DefaultFalloffDistance,
            float defaultHoldDuration = CameraShakeRequest.DefaultHoldDuration)
        {
            Configure(
                defaultIntensity,
                maxIntensity,
                defaultDuration,
                frequency,
                falloffDistance,
                defaultHoldDuration);
        }

        /// <summary>When true, new requests and current offset are ignored.</summary>
        public static bool Suppressed { get; set; }

        static bool IsDisabled => Suppressed || !GameSettings.GetCameraShakeEnabled();

        public Vector3 CurrentOffset { get; private set; }
        public float CurrentAmplitude => CombinedAmplitude();
        public bool IsShaking => CombinedAmplitude() > 0.0001f;
        public bool IsContinuous => _loopActive;

        public static void Request() => GameEvents.RaiseCameraShake();
        public static void Request(float intensity) => GameEvents.RaiseCameraShake(intensity);
        public static void Request(CameraShakeRequest request) => GameEvents.RaiseCameraShake(request);
        public static void RequestBurst(float intensity = -1f, float duration = 0f) =>
            GameEvents.RaiseCameraShake(CameraShakeRequest.Burst(intensity, duration));
        public static void RequestFor(float duration, float intensity = -1f) =>
            GameEvents.RaiseCameraShakeFor(duration, intensity);
        public static void RequestContinuous(float intensity = -1f) =>
            GameEvents.RaiseCameraShakeContinuous(intensity);
        public static void RequestFire() => GameEvents.RaiseCameraShake(CameraShakeRequest.Fire);
        public static void RequestHit() => GameEvents.RaiseCameraShake(CameraShakeRequest.Hit);
        public static void RequestAt(Vector3 worldPosition, float intensity = -1f) =>
            GameEvents.RaiseCameraShakeAt(worldPosition, intensity);
        public static void RequestStop() => GameEvents.RaiseCameraShakeStop();
        public static void RequestStopContinuous() => GameEvents.RaiseCameraShakeStopContinuous();

        public void Configure(
            float defaultIntensity,
            float maxIntensity,
            float defaultDuration,
            float frequency,
            float falloffDistance,
            float defaultHoldDuration = CameraShakeRequest.DefaultHoldDuration)
        {
            _defaultIntensity = Mathf.Max(0f, defaultIntensity);
            _maxIntensity = Mathf.Max(_defaultIntensity, maxIntensity);
            _defaultDuration = Mathf.Max(0.01f, defaultDuration);
            _defaultHoldDuration = Mathf.Max(0.01f, defaultHoldDuration);
            _frequency = Mathf.Max(0.01f, frequency);
            _falloffDistance = Mathf.Max(0.01f, falloffDistance);
        }

        public void Shake() => Shake(CameraShakeRequest.Default);

        public void Shake(float intensity) => Shake(CameraShakeRequest.Burst(intensity));

        public void ShakeBurst(float intensity = -1f, float duration = 0f) =>
            Shake(CameraShakeRequest.Burst(intensity, duration));

        public void ShakeFor(float duration, float intensity = -1f) =>
            Shake(CameraShakeRequest.ForDuration(duration, intensity));

        public void ShakeContinuous(float intensity = -1f) =>
            Shake(CameraShakeRequest.Continuous(intensity));

        public void ShakeFire() => Shake(CameraShakeRequest.Fire);

        public void ShakeHit() => Shake(CameraShakeRequest.Hit);

        public void ShakeAt(Vector3 worldPosition, float intensity = -1f) =>
            Shake(CameraShakeRequest.At(worldPosition, intensity));

        public void Shake(CameraShakeRequest request)
        {
            var intensity = request.Intensity;
            if (intensity < 0f)
                intensity = _defaultIntensity;
            if (intensity <= 0f)
            {
                if (request.Kind == CameraShakeKind.Continuous)
                    StopContinuous();
                else
                    Stop();
                return;
            }

            if (IsDisabled)
                return;

            intensity = Mathf.Min(intensity, _maxIntensity);
            AdvanceSeed(request);

            if (request.Kind == CameraShakeKind.Continuous)
            {
                if (intensity < _loopAmplitude && _loopActive)
                    return;

                _loopActive = true;
                _loopAmplitude = intensity;
                _loopHasWorldPosition = request.HasWorldPosition;
                _loopWorldPosition = request.WorldPosition;
                _loopFadeAmplitude = 0f;
                _loopFadeDuration = 0f;
                _loopFadeElapsed = 0f;
                return;
            }

            var duration = request.Duration > 0f
                ? request.Duration
                : request.Kind == CameraShakeKind.Duration
                    ? _defaultHoldDuration
                    : _defaultDuration;
            if (intensity < TransientRemaining())
                return;

            _transientKind = request.Kind;
            _transientAmplitude = intensity;
            _transientDuration = duration;
            _transientElapsed = 0f;
            _transientHasWorldPosition = request.HasWorldPosition;
            _transientWorldPosition = request.WorldPosition;
        }

        public void StopContinuous()
        {
            if (_loopActive)
            {
                _loopFadeAmplitude = _loopAmplitude;
                _loopFadeDuration = _defaultDuration;
                _loopFadeElapsed = 0f;
                _loopFadeHasWorldPosition = _loopHasWorldPosition;
                _loopFadeWorldPosition = _loopWorldPosition;
            }

            _loopActive = false;
            _loopAmplitude = 0f;
        }

        public void Stop()
        {
            _transientAmplitude = 0f;
            _transientDuration = 0f;
            _transientElapsed = 0f;
            _loopActive = false;
            _loopAmplitude = 0f;
            _loopFadeAmplitude = 0f;
            _loopFadeDuration = 0f;
            _loopFadeElapsed = 0f;
            CurrentOffset = Vector3.zero;
        }

        public Vector3 Tick(float dt, Quaternion cameraRotation, Vector3 cameraPosition)
        {
            if (IsDisabled)
            {
                Stop();
                return CurrentOffset;
            }

            if (dt > 0f)
            {
                if (_transientAmplitude > 0.0001f)
                    _transientElapsed += dt;
                if (_loopFadeAmplitude > 0.0001f)
                    _loopFadeElapsed += dt;
                _noiseT += dt * _frequency;
            }

            if (_transientAmplitude > 0.0001f && _transientElapsed >= _transientDuration)
            {
                _transientAmplitude = 0f;
                _transientDuration = 0f;
                _transientElapsed = 0f;
            }

            if (_loopFadeAmplitude > 0.0001f && _loopFadeElapsed >= _loopFadeDuration)
            {
                _loopFadeAmplitude = 0f;
                _loopFadeDuration = 0f;
                _loopFadeElapsed = 0f;
            }

            var amp = CombinedEnvelope(cameraPosition);
            if (amp <= 0.0001f)
            {
                CurrentOffset = Vector3.zero;
                return CurrentOffset;
            }

            var nx = Mathf.PerlinNoise(_seed, _noiseT) * 2f - 1f;
            var ny = Mathf.PerlinNoise(_seed + 19.17f, _noiseT) * 2f - 1f;
            CurrentOffset = cameraRotation * new Vector3(nx * amp, ny * amp, 0f);
            return CurrentOffset;
        }

        float CombinedAmplitude()
        {
            var transient = TransientRemaining();
            var loop = _loopActive ? _loopAmplitude : LoopFadeRemaining();
            return transient > loop ? transient : loop;
        }

        float CombinedEnvelope(Vector3 cameraPosition)
        {
            var transient = ApplyFalloff(
                TransientEnvelope(),
                _transientHasWorldPosition,
                _transientWorldPosition,
                cameraPosition);
            float loop;
            bool loopWorld;
            Vector3 loopPos;
            if (_loopActive)
            {
                loop = _loopAmplitude;
                loopWorld = _loopHasWorldPosition;
                loopPos = _loopWorldPosition;
            }
            else
            {
                loop = LoopFadeEnvelope();
                loopWorld = _loopFadeHasWorldPosition;
                loopPos = _loopFadeWorldPosition;
            }

            loop = ApplyFalloff(loop, loopWorld, loopPos, cameraPosition);
            return transient > loop ? transient : loop;
        }

        float TransientRemaining()
        {
            if (_transientAmplitude <= 0.0001f || _transientDuration <= 0f || _transientElapsed >= _transientDuration)
                return 0f;
            return RemainingForKind(_transientKind, _transientAmplitude, _transientDuration, _transientElapsed);
        }

        float TransientEnvelope()
        {
            if (_transientAmplitude <= 0.0001f || _transientDuration <= 0f || _transientElapsed >= _transientDuration)
                return 0f;
            return Envelope(_transientKind, _transientAmplitude, _transientDuration, _transientElapsed);
        }

        float LoopFadeRemaining()
        {
            if (_loopFadeAmplitude <= 0.0001f || _loopFadeDuration <= 0f || _loopFadeElapsed >= _loopFadeDuration)
                return 0f;
            var life = 1f - (_loopFadeElapsed / _loopFadeDuration);
            return _loopFadeAmplitude * life;
        }

        float LoopFadeEnvelope()
        {
            if (_loopFadeAmplitude <= 0.0001f || _loopFadeDuration <= 0f || _loopFadeElapsed >= _loopFadeDuration)
                return 0f;
            var life = 1f - (_loopFadeElapsed / _loopFadeDuration);
            return _loopFadeAmplitude * life * life;
        }

        float ApplyFalloff(float amp, bool hasWorldPosition, Vector3 worldPosition, Vector3 cameraPosition)
        {
            if (amp <= 0.0001f || !hasWorldPosition)
                return amp;

            var delta = worldPosition - cameraPosition;
            delta.y = 0f;
            var distT = 1f - Mathf.Clamp01(delta.magnitude / _falloffDistance);
            return amp * distT * distT;
        }

        void AdvanceSeed(CameraShakeRequest request)
        {
            _seed += 1.173f;
            if (request.HasWorldPosition)
                _seed += request.WorldPosition.x * 0.13f + request.WorldPosition.z * 0.27f;
        }

        static float Envelope(CameraShakeKind kind, float amplitude, float duration, float elapsed)
        {
            if (kind == CameraShakeKind.Duration)
            {
                var fade = HoldFade(duration);
                var holdEnd = duration - fade;
                if (elapsed <= holdEnd)
                    return amplitude;
                var life = 1f - ((elapsed - holdEnd) / fade);
                return amplitude * life * life;
            }

            var burstLife = 1f - (elapsed / duration);
            return amplitude * burstLife * burstLife;
        }

        static float RemainingForKind(CameraShakeKind kind, float amplitude, float duration, float elapsed)
        {
            if (kind == CameraShakeKind.Duration)
            {
                var fade = HoldFade(duration);
                var holdEnd = duration - fade;
                if (elapsed <= holdEnd)
                    return amplitude;
                return amplitude * (1f - ((elapsed - holdEnd) / fade));
            }

            return amplitude * (1f - (elapsed / duration));
        }

        static float HoldFade(float duration)
        {
            var fade = duration * 0.25f;
            if (fade > CameraShakeRequest.DefaultDuration)
                fade = CameraShakeRequest.DefaultDuration;
            if (fade < 0.01f)
                fade = 0.01f;
            if (fade > duration)
                fade = duration;
            return fade;
        }
    }
}
