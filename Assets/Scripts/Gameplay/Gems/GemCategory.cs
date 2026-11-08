namespace GemTD.Gameplay.Gems
{
    /// <summary>
    /// Player-facing gem category. Authored upstream in the support-gem source JSON and
    /// carried onto <see cref="GemDefinition"/>. It answers "what does this gem do to my
    /// build", which the tag chips cannot: Support, Critical, and the damage-type tags are
    /// not chips, so a gem whose only tags are those would otherwise show nothing.
    /// </summary>
    public enum GemCategory
    {
        /// <summary>No authored category. Displays nothing.</summary>
        None = 0,

        /// <summary>Damage and direct power scaling.</summary>
        Offensive = 1,

        /// <summary>Projectile, targeting, attack pattern, and area-placement changes.</summary>
        ProjectileTargeting = 2,

        /// <summary>Crowd control, sustain, defense, resource, duration, and support effects.</summary>
        UtilityAndControl = 3,

        /// <summary>Fundamentally changes the attack, deployment, trigger, summon, or damage model.</summary>
        Transformation = 4,
    }

    public static class GemCategories
    {
        /// <summary>Maps a source-JSON category string. Unknown or empty maps to <see cref="GemCategory.None"/>.</summary>
        public static GemCategory FromSource(string source)
        {
            if (string.IsNullOrEmpty(source))
                return GemCategory.None;
            switch (source)
            {
                case "offensive": return GemCategory.Offensive;
                case "projectile_targeting": return GemCategory.ProjectileTargeting;
                case "utility_control": return GemCategory.UtilityAndControl;
                case "transformation": return GemCategory.Transformation;
                default: return GemCategory.None;
            }
        }

        /// <summary>Player-facing category name. Empty for None so a view can hide the row.</summary>
        public static string Label(GemCategory category)
        {
            switch (category)
            {
                case GemCategory.Offensive: return "Offensive";
                case GemCategory.ProjectileTargeting: return "Projectile";
                case GemCategory.UtilityAndControl: return "Utility";
                case GemCategory.Transformation: return "Transformation";
                default: return "";
            }
        }
    }
}
