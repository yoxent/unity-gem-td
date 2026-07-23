using UnityEngine;

namespace GemTD.Gameplay.Enemies
{
    /// <summary>Pooled sphere view bound to an <see cref="EnemyRuntime"/>.</summary>
    public sealed class EnemyView : MonoBehaviour
    {
        public EnemyRuntime Runtime { get; private set; }

        public void Bind(EnemyRuntime runtime)
        {
            Runtime = runtime;
            SyncTransform();
        }

        public void SyncTransform()
        {
            if (Runtime == null)
                return;
            transform.position = Runtime.WorldPosition + Vector3.up * 0.4f;
        }

        public void Clear()
        {
            Runtime = null;
        }
    }
}
