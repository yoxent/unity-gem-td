namespace GemTD.Gameplay.Enemies
{
    /// <summary>
    /// Labels on an enemy. They stack. They do not write stats.
    /// A tag answers whether the body or behavior is present.
    /// Numbers stay on <see cref="EnemyDefinition"/>.
    /// Append only. Serialized bit values must not move.
    /// </summary>
    [System.Flags]
    public enum EnemyTag
    {
        None = 0,
        Runner = 1 << 0,
        Armored = 1 << 1,
        Arrow = 1 << 2,
        Shield = 1 << 3,
        Blink = 1 << 4,
        Bloody = 1 << 5,
        Hunting = 1 << 6,
        Ironclad = 1 << 7,
        Shaded = 1 << 8,
    }

    public static class EnemyTags
    {
        public static readonly EnemyTag[] AuraOrder =
        {
            EnemyTag.Bloody,
            EnemyTag.Hunting,
            EnemyTag.Ironclad,
            EnemyTag.Shaded,
        };

        public static bool Has(EnemyTag tags, EnemyTag flag)
        {
            if (flag == EnemyTag.None)
                return false;
            return (tags & flag) == flag;
        }

        public static bool TryGetAuraAffix(EnemyTag tag, out EnemyAffix affix)
        {
            switch (tag)
            {
                case EnemyTag.Bloody:
                    affix = EnemyAffix.Bloody;
                    return true;
                case EnemyTag.Hunting:
                    affix = EnemyAffix.Hunting;
                    return true;
                case EnemyTag.Ironclad:
                    affix = EnemyAffix.Ironclad;
                    return true;
                case EnemyTag.Shaded:
                    affix = EnemyAffix.Shaded;
                    return true;
                default:
                    affix = default;
                    return false;
            }
        }

        public static string AuraWord(EnemyAffix affix)
        {
            switch (affix)
            {
                case EnemyAffix.Bloody: return "Bloody";
                case EnemyAffix.Hunting: return "Hunting";
                case EnemyAffix.Ironclad: return "Ironclad";
                case EnemyAffix.Shaded: return "Shaded";
                default: return "";
            }
        }
    }
}
