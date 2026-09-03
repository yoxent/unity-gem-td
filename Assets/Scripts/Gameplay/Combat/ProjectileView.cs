using UnityEngine;
using GemTD.Gameplay.Map;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    /// <summary>Pooled bolt/payload view bound to a <see cref="ProjectileRuntime"/> or effect payload.</summary>
    public sealed class ProjectileView : MonoBehaviour
    {
        [SerializeField] bool slamEffect;
        [SerializeField] bool aftershockEffect;

        Vector3 _defaultScale = Vector3.one;
        bool _capturedScale;

        public bool IsSlamEffect => slamEffect;
        public bool IsAftershockEffect => aftershockEffect;
        bool SitsOnGround => slamEffect || aftershockEffect;

        public static bool WantsSlamEffect(EffectPayloadRuntime payload)
        {
            return payload != null && payload.ShowsSlamVisual;
        }

        public static bool WantsAftershockEffect(EffectPayloadRuntime payload)
        {
            return payload != null && payload.ShowsAftershockVisual;
        }

        public ProjectileRuntime Runtime { get; private set; }
        public EffectPayloadRuntime Payload { get; private set; }

        public void Bind(ProjectileRuntime runtime)
        {
            EnsureDefaultScale();
            Runtime = runtime;
            Payload = null;
            transform.localScale = _defaultScale;
            SyncTransform();
        }

        public void Bind(EffectPayloadRuntime payload)
        {
            EnsureDefaultScale();
            Runtime = null;
            Payload = payload;
            ApplyPayloadVisual();
            SyncTransform();
        }

        public void SnapTo(Vector3 position)
        {
            EnsureDefaultScale();
            Runtime = null;
            Payload = null;
            transform.localScale = _defaultScale;
            ApplyTransform(position, Vector3.zero);
        }

        public void SyncTransform()
        {
            if (Runtime != null)
            {
                ApplyTransform(Runtime.Position, Runtime.Direction);
                return;
            }

            if (Payload != null)
                ApplyTransform(Payload.Position, Payload.Direction);
        }

        void ApplyPayloadVisual()
        {
            if (Payload == null)
            {
                transform.localScale = _defaultScale;
                return;
            }

            switch (Payload.Plan.TravelPattern)
            {
                case EffectPayloadTravelPattern.StationaryPulse:
                    if (SitsOnGround)
                        transform.localScale = SlamEffectVisual.ScaleToDiameter(Payload.Plan.AoeRadius);
                    else
                    {
                        var radius = Payload.Plan.AoeRadius > 0.4f ? Payload.Plan.AoeRadius : 0.4f;
                        transform.localScale = new Vector3(radius * 2f, 0.12f, radius * 2f);
                    }
                    break;
                case EffectPayloadTravelPattern.Fountain:
                    transform.localScale = _defaultScale * 0.65f;
                    break;
                default:
                    transform.localScale = _defaultScale;
                    break;
            }
        }

        void ApplyTransform(Vector3 position, Vector3 direction)
        {
            var stationary = Payload != null
                && Payload.Plan.TravelPattern == EffectPayloadTravelPattern.StationaryPulse;
            if (SitsOnGround && stationary)
            {
                var ground = new Vector3(position.x, position.y, position.z);
                ground.y += TileHeightVisual.PathScaleY * 0.5f;
                transform.position = SlamEffectVisual.SitOnGround(
                    ground,
                    1f,
                    // Parent Y is now the AoE diameter; keep the authored ground lift.
                    _defaultScale.y);
                transform.rotation = Quaternion.identity;
                return;
            }

            var lift = stationary ? 0.08f : 0f;
            transform.position = position + Vector3.up * lift;
            if (direction.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        public void Clear()
        {
            Runtime = null;
            Payload = null;
            if (_capturedScale)
                transform.localScale = _defaultScale;
        }

        void EnsureDefaultScale()
        {
            if (_capturedScale)
                return;
            _defaultScale = transform.localScale;
            _capturedScale = true;
        }
    }
}
