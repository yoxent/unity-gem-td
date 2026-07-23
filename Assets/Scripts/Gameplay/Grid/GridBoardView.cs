using UnityEngine;

namespace GemTD.Gameplay.Grid
{
    /// <summary>
    /// Thin view: binds a <see cref="GridBoard"/> (+ optional path) and builds placeholder tiles.
    /// </summary>
    public sealed class GridBoardView : MonoBehaviour
    {
        [SerializeField] float cellSize = 1f;
        [SerializeField] Material tileMaterial;
        [SerializeField] Material pathMaterial;

        public GridBoard Board { get; private set; }
        public float CellSize => cellSize;

        PathGraph _path;

        /// <summary>Bind domain board (created by composition root). Rebuilds visuals.</summary>
        public void Bind(GridBoard board, PathGraph path = null)
        {
            Board = board;
            _path = path;
            RebuildVisuals();
        }

        public void SetPath(PathGraph path)
        {
            _path = path;
            RebuildVisuals();
        }

        public void RebuildVisuals()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            if (Board == null)
                return;

            var width = Board.Width;
            var height = Board.Height;
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

                    var isPath = _path != null && _path.IsPath(x, y);
                    var mat = isPath && pathMaterial != null ? pathMaterial : tileMaterial;
                    if (mat != null)
                    {
                        var renderer = tile.GetComponent<MeshRenderer>();
                        if (renderer != null)
                            renderer.sharedMaterial = mat;
                    }
                    else if (isPath)
                    {
                        var renderer = tile.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                            var block = new MaterialPropertyBlock();
                            renderer.GetPropertyBlock(block);
                            var pathColor = new Color(0.55f, 0.4f, 0.25f);
                            block.SetColor("_BaseColor", pathColor);
                            block.SetColor("_Color", pathColor);
                            renderer.SetPropertyBlock(block);
                        }
                    }
                }
            }
        }

        public Vector3 CellToWorld(int x, int y) => CellCenterWorld(x, y);

        public Vector3 CellToWorld(Vector2Int cell) => CellCenterWorld(cell.x, cell.y);

        public Vector3 CellCenterWorld(int x, int y)
        {
            var half = cellSize * 0.5f;
            return transform.TransformPoint(new Vector3(x * cellSize + half, 0f, y * cellSize + half));
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            var local = transform.InverseTransformPoint(world);
            var x = Mathf.FloorToInt(local.x / cellSize);
            var y = Mathf.FloorToInt(local.z / cellSize);
            return new Vector2Int(x, y);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            cellSize = Mathf.Max(0.1f, cellSize);
        }
#endif
    }
}
