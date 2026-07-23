namespace GemTD.Gameplay.Grid
{
    /// <summary>
    /// Plain tile occupancy model. PathGraph comes in Phase 2.
    /// </summary>
    public sealed class GridBoard
    {
        public int Width { get; }
        public int Height { get; }

        readonly bool[] _buildable;

        public GridBoard(int width, int height)
        {
            Width = width > 0 ? width : 1;
            Height = height > 0 ? height : 1;
            _buildable = new bool[Width * Height];
            for (var i = 0; i < _buildable.Length; i++)
                _buildable[i] = true;
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public bool IsBuildable(int x, int y) => InBounds(x, y) && _buildable[Index(x, y)];

        public void SetBuildable(int x, int y, bool buildable)
        {
            if (!InBounds(x, y)) return;
            _buildable[Index(x, y)] = buildable;
        }

        int Index(int x, int y) => y * Width + x;
    }
}
