using UnityEngine;

namespace GemTD.Gameplay.Map
{
    /// <summary>
    /// 1x1 cell marker on an occupied chunk's open edge. Visual placeholder
    /// (yaw = outward) for a future arrow. Click target via collider.
    /// </summary>
    public sealed class ExpandMarkerView : MonoBehaviour
    {
        public Vector2Int ChunkCoord { get; private set; }
        public EdgeFlags Outward { get; private set; }

        public void Bind(Vector2Int chunkCoord, Vector3 world, float cellSize, EdgeFlags outward)
        {
            ChunkCoord = chunkCoord;
            Outward = outward;
            // Slight lift to avoid being visually/selection-occluded by neighboring chunk meshes.
            transform.position = world + new Vector3(0f, 0.02f, 0f);
            transform.localScale = new Vector3(cellSize, 1f, cellSize);
            transform.rotation = Quaternion.Euler(0f, outward.YawTurnsCW() * 90f, 0f);
            gameObject.SetActive(true);
        }
    }
}
