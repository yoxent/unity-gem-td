using GemTD.Gameplay.Gems;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Inspector folder under <c>Assets/Data/Towers/Catalog</c>. Herald skills
    /// live with Aura. Attack/Spell leftovers stay at the category root.
    /// </summary>
    public static class TowerCatalogLayout
    {
        public static string RelativeFolder(string category, GemTag tags)
        {
            if ((tags & GemTag.Herald) != 0)
                return "Aura";

            var root = CategoryFolder(category);
            if (root == "Attack")
            {
                if ((tags & GemTag.Slam) != 0)
                    return "Attack/Slam";
                if ((tags & GemTag.Strike) != 0)
                    return "Attack/Strike";
                if ((tags & GemTag.Bow) != 0)
                    return "Attack/Bow";
                return "Attack";
            }

            if (root == "Spell")
            {
                if ((tags & GemTag.Channeling) != 0)
                    return "Spell/Channeling";
                if ((tags & GemTag.Projectile) != 0)
                    return "Spell/Projectile";
                if ((tags & GemTag.Aoe) != 0)
                    return "Spell/AOE";
                return "Spell";
            }

            return root;
        }

        public static string CategoryFolder(string category)
        {
            if (string.IsNullOrEmpty(category))
                return "Attack";
            return char.ToUpperInvariant(category[0]) + category.Substring(1);
        }
    }
}
