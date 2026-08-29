using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Authored curse proof numbers. See docs
    /// planning/sdd/2026-08-29-damage-types-curses-enemies-design.md.
    /// </summary>
    public static class CurseProofNumbers
    {
        public const float RadiusLevel1 = 3f;
        public const float RadiusPerLevel = 1.15f;
        public const float ResistLevel1 = -30f;
        public const float ResistPerLevel = -5f;

        static readonly float[] VulnerabilityTaken =
        {
            27f, 31f, 34f, 38f, 42f, 45f, 49f, 53f, 56f, 60f
        };

        public static float Radius(int sourceLevel)
        {
            var level = Mathf.Clamp(sourceLevel, TowerInstance.DefaultLevel, TowerInstance.MaxLevel);
            return RoundMetres(RadiusLevel1 * Mathf.Pow(RadiusPerLevel, level - 1));
        }

        public static float Resist(int sourceLevel)
        {
            var level = Mathf.Clamp(sourceLevel, TowerInstance.DefaultLevel, TowerInstance.MaxLevel);
            return ResistLevel1 + ResistPerLevel * (level - 1);
        }

        public static float Vulnerability(int sourceLevel)
        {
            var level = Mathf.Clamp(sourceLevel, TowerInstance.DefaultLevel, TowerInstance.MaxLevel);
            return VulnerabilityTaken[level - 1];
        }

        public static bool IsProofSlug(string slug)
        {
            return IsResistSlug(slug)
                || IsVulnerability(slug)
                || IsTemporalChains(slug);
        }

        public static bool IsResistSlug(string slug)
        {
            return IsFlammability(slug)
                || IsFrostbite(slug)
                || IsConductivity(slug)
                || IsDespair(slug);
        }

        public static bool IsFlammability(string slug) =>
            string.Equals(slug, "Flammability", System.StringComparison.OrdinalIgnoreCase);

        public static bool IsFrostbite(string slug) =>
            string.Equals(slug, "Frostbite", System.StringComparison.OrdinalIgnoreCase);

        public static bool IsConductivity(string slug) =>
            string.Equals(slug, "Conductivity", System.StringComparison.OrdinalIgnoreCase);

        public static bool IsDespair(string slug) =>
            string.Equals(slug, "Despair", System.StringComparison.OrdinalIgnoreCase);

        public static bool IsVulnerability(string slug) =>
            string.Equals(slug, "Vulnerability", System.StringComparison.OrdinalIgnoreCase);

        public static bool IsTemporalChains(string slug) =>
            string.Equals(slug, "Temporal_Chains", System.StringComparison.OrdinalIgnoreCase);

        public static RoleEffectKind ResistKind(string slug)
        {
            if (IsFlammability(slug))
                return RoleEffectKind.EnemyFireResistance;
            if (IsFrostbite(slug))
                return RoleEffectKind.EnemyColdResistance;
            if (IsConductivity(slug))
                return RoleEffectKind.EnemyLightningResistance;
            return RoleEffectKind.EnemyChaosResistance;
        }

        static float RoundMetres(float value)
        {
            return Mathf.Round(value * 100f) / 100f;
        }
    }
}
