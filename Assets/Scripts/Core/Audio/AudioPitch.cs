namespace GemTD.Core
{
    public static class AudioPitch
    {
        public static float Resolve(SfxData data, float unit01)
        {
            if (!data.randomPitch)
                return data.pitch;

            var min = data.pitchMin;
            var max = data.pitchMax;
            if (min > max)
            {
                var tmp = min;
                min = max;
                max = tmp;
            }

            var t = unit01;
            if (t < 0f) t = 0f;
            else if (t > 1f) t = 1f;
            return min + (max - min) * t;
        }
    }
}
