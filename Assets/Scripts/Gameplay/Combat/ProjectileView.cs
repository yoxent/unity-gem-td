using UnityEngine;

namespace GemTD.Gameplay.Combat
{
    /// <summary>Pooled bolt view bound to a <see cref="ProjectileRuntime"/>.</summary>
    public sealed class ProjectileView : MonoBehaviour
    {
        public ProjectileRuntime Runtime { get; private set; }
        public EffectPayloadRuntime Payload { get; private set; }

        public void Bind(ProjectileRuntime runtime)
        {
            Runtime = runtime;
            Payload = null;
            SyncTransform();
        }

        public void Bind(EffectPayloadRuntime payload)
        {
            Runtime = null;
            Payload = payload;
            SyncTransform();
        }

        public void SnapTo(Vector3 position)
        {
            Runtime = null;
            Payload = null;
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

        void ApplyTransform(Vector3 position, Vector3 direction)
        {
            transform.position = position + Vector3.up * 0.5f;
            if (direction.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        public void Clear()
        {
            Runtime = null;
            Payload = null;
        }
    }
}
