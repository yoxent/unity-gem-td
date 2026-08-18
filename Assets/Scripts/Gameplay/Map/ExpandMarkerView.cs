using UnityEngine;

namespace GemTD.Gameplay.Map
{
    /// <summary>World-space outline for a legal chunk-slot expand. Visual only (no collider).</summary>
    public sealed class ExpandMarkerView : MonoBehaviour
    {
        public Vector2Int ChunkCoord { get; private set; }

        public void Bind(Vector2Int chunkCoord, Vector3 world, float chunkWorldSize)
        {
            ChunkCoord = chunkCoord;
            transform.position = world + Vector3.up * 0.35f;
            transform.localScale = new Vector3(chunkWorldSize, 1f, chunkWorldSize);
            gameObject.SetActive(true);
        }
    }
}
