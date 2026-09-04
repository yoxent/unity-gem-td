using UnityEngine;
using GemTD.Gameplay.Map;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    /// <summary>Pooled world effect view. Kind-specific fields live on subclasses.</summary>
    public abstract class EffectView : MonoBehaviour
    {
        [SerializeField] Transform scaleRoot;

        Vector3 _defaultScale = Vector3.one;
        bool _capturedScale;
        Collider[] _colliders;
        bool[] _colliderDefaults;

        public virtual bool IsSlamEffect => false;
        public virtual bool IsAftershockEffect => false;
        public virtual bool IsFallEffect => false;
        protected virtual bool SitsOnGround => false;

        public static bool WantsSlamEffect(EffectPayloadRuntime payload)
        {
            return payload != null && payload.ShowsSlamVisual;
        }

        public static bool WantsAftershockEffect(EffectPayloadRuntime payload)
        {
            return payload != null && payload.ShowsAftershockVisual;
        }

        public static bool WantsFallEffect(EffectPayloadRuntime payload)
        {
            return payload != null && payload.ShowsFallVisual;
        }

        public ProjectileRuntime Runtime { get; private set; }
        public EffectPayloadRuntime Payload { get; private set; }

        public void Bind(ProjectileRuntime runtime)
        {
            EnsureDefaultScale();
            Runtime = runtime;
            Payload = null;
            SetVisualScale(_defaultScale);
            OnBind();
            SyncTransform();
        }

        public void Bind(EffectPayloadRuntime payload)
        {
            EnsureDefaultScale();
            Runtime = null;
            Payload = payload;
            ApplyPayloadVisual();
            OnBind();
            SyncTransform();
        }

        public void SnapTo(Vector3 position)
        {
            EnsureDefaultScale();
            Runtime = null;
            Payload = null;
            SetVisualScale(_defaultScale);
            ApplyColliderState();
            ApplyTransform(position, Vector3.zero);
            OnBind();
            AfterSync();
        }

        public void SyncTransform()
        {
            if (Runtime != null)
                ApplyTransform(Runtime.Position, Runtime.Direction);
            else if (Payload != null)
                ApplyTransform(Payload.Position, Payload.Direction);

            ApplyColliderState();
            AfterSync();
        }

        public void Clear()
        {
            Runtime = null;
            Payload = null;
            if (_capturedScale)
                SetVisualScale(_defaultScale);
            ApplyColliderState();
            OnClear();
        }

        protected virtual void OnBind()
        {
        }

        protected virtual void AfterSync()
        {
        }

        protected virtual void OnClear()
        {
        }

        void ApplyPayloadVisual()
        {
            if (Payload == null)
            {
                SetVisualScale(_defaultScale);
                return;
            }

            switch (Payload.Plan.TravelPattern)
            {
                case EffectPayloadTravelPattern.StationaryPulse:
                    if (SitsOnGround)
                        SetVisualScale(Vector3.Scale(_defaultScale, SlamEffectVisual.ScaleToDiameter(Payload.Plan.AoeRadius)));
                    else
                    {
                        var radius = Payload.Plan.AoeRadius > 0.4f ? Payload.Plan.AoeRadius : 0.4f;
                        SetVisualScale(new Vector3(radius * 2f, 0.12f, radius * 2f));
                    }
                    break;
                case EffectPayloadTravelPattern.Fountain:
                    SetVisualScale(_defaultScale * 0.65f);
                    break;
                default:
                    SetVisualScale(_defaultScale);
                    break;
            }
        }

        protected virtual void ApplyTransform(Vector3 position, Vector3 direction)
        {
            var stationary = Payload != null
                && Payload.Plan.TravelPattern == EffectPayloadTravelPattern.StationaryPulse;
            if (SitsOnGround && stationary)
            {
                var ground = new Vector3(position.x, position.y, position.z);
                ground.y += TileHeightVisual.PathScaleY * 0.5f;
                transform.position = SlamEffectVisual.SitOnGround(ground, 1f, 1f);
                transform.rotation = Quaternion.identity;
                return;
            }

            var lift = stationary ? 0.08f : 0f;
            transform.position = position + Vector3.up * lift;
            if (direction.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        protected virtual void ApplyColliderState()
        {
            EnsureColliders();
            for (var i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] == null)
                    continue;
                _colliders[i].enabled = _colliderDefaults[i];
            }
        }

        protected void EnsureColliders()
        {
            if (_colliders != null)
                return;

            _colliders = GetComponentsInChildren<Collider>(true);
            _colliderDefaults = new bool[_colliders.Length];
            for (var i = 0; i < _colliders.Length; i++)
                _colliderDefaults[i] = _colliders[i] != null && _colliders[i].enabled;
        }

        protected Collider[] Colliders => _colliders;
        protected bool[] ColliderDefaults => _colliderDefaults;

        void SetVisualScale(Vector3 scale)
        {
            transform.localScale = Vector3.one;
            if (scaleRoot == null)
                return;
            scaleRoot.localScale = scale;
        }

        void EnsureDefaultScale()
        {
            if (_capturedScale)
                return;
            if (scaleRoot == null)
                Debug.LogError("EffectView: scaleRoot is not assigned.", this);
            else
                _defaultScale = scaleRoot.localScale;
            _capturedScale = true;
        }
    }
}
