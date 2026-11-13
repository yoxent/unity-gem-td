namespace GemTD.Gameplay.Combat
{
    public enum BoltElement
    {
        Water = 0,
        Fire = 1,
        Ice = 2,
        Lightning = 3
    }

    /// <summary>Maps the resolved damage mix to a readable Bolt presentation.</summary>
    public static class BoltElementVisual
    {
        public static BoltElement Resolve(in SkillSpec spec)
        {
            var element = BoltElement.Water;
            var strongest = 0f;

            if (spec.MixFire > strongest)
            {
                strongest = spec.MixFire;
                element = BoltElement.Fire;
            }

            if (spec.MixCold > strongest)
            {
                strongest = spec.MixCold;
                element = BoltElement.Ice;
            }

            if (spec.MixLightning > strongest)
                element = BoltElement.Lightning;

            return element;
        }
    }
}
