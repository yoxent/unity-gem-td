using UnityEngine;

namespace GemTD.Gameplay.Map
{
    /// <summary>World-space + marker for legal expand cells.</summary>
    public sealed class ExpandMarkerView : MonoBehaviour
    {
        public Vector2Int Cell { get; private set; }

        public void Bind(Vector2Int cell, Vector3 worldPosition)
        {
            Cell = cell;
            transform.position = worldPosition + Vector3.up * 0.35f;
            gameObject.SetActive(true);
        }
    }
}
