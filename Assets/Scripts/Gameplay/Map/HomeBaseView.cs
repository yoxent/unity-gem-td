using UnityEngine;

namespace GemTD.Gameplay.Map
{
    /// <summary>Greybox home-base marker (enemies path toward this cell).</summary>
    public sealed class HomeBaseView : MonoBehaviour
    {
        public Vector2Int Cell { get; private set; }

        public void Bind(Vector2Int cell, Vector3 worldPosition)
        {
            Cell = cell;
            transform.position = worldPosition + Vector3.up * 0.6f;
            gameObject.SetActive(true);
        }
    }
}
