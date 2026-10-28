namespace GemTD.Gameplay.Map
{
    public static class TileHeightRules
    {
        public const byte MaxLayer = 2;

        public static float RangeMultiplier(int layer)
        {
            if (layer <= 0)
                return 1f;
            if (layer == 1)
                return 1.2f;
            return 1.3f;
        }
    }
}
