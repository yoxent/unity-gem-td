using UnityEngine;

namespace GemTD.Gameplay.Grid
{
    /// <summary>
    /// Thin view: builds a flat placeholder grid under this transform.
    /// </summary>
    public sealed class GridBoardView : MonoBehaviour
    {
        [SerializeField] int width = 8;
        [SerializeField] int height = 8;
        [SerializeField] float cellSize = 1f;
        [SerializeField] Material tileMaterial;

        public GridBoard Board { get; private set; }

        void Awake()
        {
            Board = new GridBoard(width, height);
            RebuildVisuals();
        }

        public void RebuildVisuals()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            var half = cellSize * 0.5f;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Tile_{x}_{y}";
                    tile.transform.SetParent(transform, false);
                    tile.transform.localPosition = new Vector3(x * cellSize + half, -0.05f, y * cellSize + half);
                    tile.transform.localScale = new Vector3(cellSize * 0.95f, 0.1f, cellSize * 0.95f);

                    var col = tile.GetComponent<Collider>();
                    if (col != null)
                        Destroy(col);

                    if (tileMaterial != null)
                    {
                        var renderer = tile.GetComponent<MeshRenderer>();
                        if (renderer != null)
                            renderer.sharedMaterial = tileMaterial;
                    }
                }
            }
        }

        public Vector3 CellCenterWorld(int x, int y)
        {
            var half = cellSize * 0.5f;
            return transform.TransformPoint(new Vector3(x * cellSize + half, 0f, y * cellSize + half));
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            cellSize = Mathf.Max(0.1f, cellSize);
        }
#endif
    }
}
