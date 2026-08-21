using UnityEngine;

namespace GemTD.Gameplay.Combat
{
    /// <summary>Pooled bolt view bound to a <see cref="ProjectileRuntime"/>.</summary>
    public sealed class ProjectileView : MonoBehaviour
    {
        public ProjectileRuntime Runtime { get; private set; }

        public void Bind(ProjectileRuntime runtime)
        {
            Runtime = runtime;
            SyncTransform();
        }

        public void SyncTransform()
        {
            if (Runtime == null)
                return;
            transform.position = Runtime.Position + Vector3.up * 0.5f;
            if (Runtime.Direction.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(Runtime.Direction, Vector3.up);
        }

        public void Clear()
        {
            Runtime = null;
        }
    }
}
