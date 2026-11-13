using UnityEngine;
using GemTD.Core;

namespace GemTD.Gameplay.CameraControl
{
    /// <summary>
    /// Applies <see cref="CameraShake"/> after <see cref="RunCameraController"/> writes pose.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraShakeDriver : MonoBehaviour
    {
        [SerializeField, Tooltip("World-unit amplitude used when a request does not supply intensity.")]
        float defaultIntensity = CameraShakeRequest.DefaultIntensity;
        [SerializeField, Tooltip("Hard cap so overlapping fire/hit requests cannot grow nauseating.")]
        float maxIntensity = CameraShakeRequest.MaxIntensity;
        [SerializeField, Tooltip("Seconds for a burst shake to decay to zero.")]
        float defaultDuration = CameraShakeRequest.DefaultDuration;
        [SerializeField, Tooltip("Seconds a duration shake holds when the request omits a time.")]
        float defaultHoldDuration = CameraShakeRequest.DefaultHoldDuration;
        [SerializeField, Tooltip("Perlin noise speed. Higher is jitterier.")]
        float frequency = CameraShakeRequest.DefaultFrequency;
        [SerializeField, Tooltip("World-position requests fade to zero beyond this XZ distance from the camera.")]
        float falloffDistance = CameraShakeRequest.DefaultFalloffDistance;

        CameraShake _shake;

        void Awake()
        {
            _shake = new CameraShake(
                defaultIntensity,
                maxIntensity,
                defaultDuration,
                frequency,
                falloffDistance,
                defaultHoldDuration);
        }

        void OnEnable()
        {
            GameEvents.CameraShake += OnCameraShake;
        }

        void OnDisable()
        {
            GameEvents.CameraShake -= OnCameraShake;
            _shake?.Stop();
        }

        void LateUpdate()
        {
            if (_shake == null)
                return;

            transform.position += _shake.Tick(Time.deltaTime, transform.rotation, transform.position);
        }

        void OnCameraShake(CameraShakeRequest request)
        {
            _shake?.Shake(request);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            defaultIntensity = Mathf.Max(0f, defaultIntensity);
            maxIntensity = Mathf.Max(defaultIntensity, maxIntensity);
            defaultDuration = Mathf.Max(0.01f, defaultDuration);
            defaultHoldDuration = Mathf.Max(0.01f, defaultHoldDuration);
            frequency = Mathf.Max(0.01f, frequency);
            falloffDistance = Mathf.Max(0.01f, falloffDistance);
            _shake?.Configure(
                defaultIntensity,
                maxIntensity,
                defaultDuration,
                frequency,
                falloffDistance,
                defaultHoldDuration);
        }
#endif
    }
}
