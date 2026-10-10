using UnityEngine;

namespace GemTD.Core
{
    /// <summary>
    /// Juice request for a camera shake. Intensity is world-unit displacement after decay.
    /// Negative intensity means "use the listener's default". Zero stops:
    /// <see cref="CameraShakeKind.Continuous"/> stops the loop only; any other kind stops all.
    /// </summary>
    public readonly struct CameraShakeRequest
    {
        public const float DefaultIntensity = 0.03f;
        public const float FireIntensity = 0.02f;
        public const float HitIntensity = 0.035f;
        public const float MaxIntensity = 0.08f;
        public const float DefaultDuration = 0.12f;
        public const float DefaultHoldDuration = 0.4f;
        public const float DefaultFrequency = 14f;
        public const float DefaultFalloffDistance = 40f;

        public readonly float Intensity;
        public readonly float Duration;
        public readonly CameraShakeKind Kind;
        public readonly Vector3 WorldPosition;
        public readonly bool HasWorldPosition;

        public CameraShakeRequest(float intensity, float duration = 0f)
            : this(
                intensity,
                duration,
                duration > 0f ? CameraShakeKind.Duration : CameraShakeKind.Burst,
                default,
                false)
        {
        }

        public CameraShakeRequest(float intensity, Vector3 worldPosition, float duration = 0f)
            : this(
                intensity,
                duration,
                duration > 0f ? CameraShakeKind.Duration : CameraShakeKind.Burst,
                worldPosition,
                true)
        {
        }

        public CameraShakeRequest(
            float intensity,
            float duration,
            CameraShakeKind kind,
            Vector3 worldPosition,
            bool hasWorldPosition)
        {
            Intensity = intensity;
            Duration = duration;
            Kind = kind;
            WorldPosition = worldPosition;
            HasWorldPosition = hasWorldPosition;
        }

        public static CameraShakeRequest Default => Burst(-1f);
        public static CameraShakeRequest Fire => Burst(FireIntensity);
        public static CameraShakeRequest Hit => Burst(HitIntensity);
        public static CameraShakeRequest StopAll => new CameraShakeRequest(0f);
        public static CameraShakeRequest StopContinuous =>
            new CameraShakeRequest(0f, 0f, CameraShakeKind.Continuous, default, false);

        public static CameraShakeRequest Burst(float intensity = -1f, float duration = 0f)
        {
            return new CameraShakeRequest(intensity, duration, CameraShakeKind.Burst, default, false);
        }

        public static CameraShakeRequest ForDuration(float duration, float intensity = -1f)
        {
            return new CameraShakeRequest(intensity, duration, CameraShakeKind.Duration, default, false);
        }

        public static CameraShakeRequest Continuous(float intensity = -1f)
        {
            return new CameraShakeRequest(intensity, 0f, CameraShakeKind.Continuous, default, false);
        }

        public static CameraShakeRequest At(Vector3 worldPosition, float intensity = DefaultIntensity)
        {
            return BurstAt(worldPosition, intensity);
        }

        public static CameraShakeRequest BurstAt(Vector3 worldPosition, float intensity = -1f, float duration = 0f)
        {
            return new CameraShakeRequest(intensity, duration, CameraShakeKind.Burst, worldPosition, true);
        }

        public static CameraShakeRequest ForDurationAt(
            Vector3 worldPosition,
            float duration,
            float intensity = -1f)
        {
            return new CameraShakeRequest(intensity, duration, CameraShakeKind.Duration, worldPosition, true);
        }

        public static CameraShakeRequest ContinuousAt(Vector3 worldPosition, float intensity = -1f)
        {
            return new CameraShakeRequest(intensity, 0f, CameraShakeKind.Continuous, worldPosition, true);
        }
    }
}
