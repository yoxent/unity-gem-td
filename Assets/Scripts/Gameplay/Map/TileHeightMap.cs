namespace GemTD.Gameplay.Map
{
    public sealed class TileHeightMap
    {
        readonly byte[] _layers;
        readonly bool[] _has;

        public int Width { get; }
        public int Height { get; }

        public TileHeightMap(int width, int height)
        {
            Width = width > 0 ? width : 1;
            Height = height > 0 ? height : 1;
            var n = Width * Height;
            _layers = new byte[n];
            _has = new bool[n];
        }

        public bool InBounds(int x, int y) =>
            x >= 0 && y >= 0 && x < Width && y < Height;

        public bool Has(int x, int y) =>
            InBounds(x, y) && _has[y * Width + x];

        public byte Get(int x, int y)
        {
            if (!InBounds(x, y))
                return 0;
            return _layers[y * Width + x];
        }

        public void Set(int x, int y, byte layer)
        {
            if (!InBounds(x, y))
                return;
            if (layer > TileHeightRules.MaxLayer)
                layer = TileHeightRules.MaxLayer;
            var i = y * Width + x;
            _layers[i] = layer;
            _has[i] = true;
        }
    }
}
