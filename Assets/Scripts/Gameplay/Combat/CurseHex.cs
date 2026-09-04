using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    public static class CurseHex
    {
        public const float PresenceDuration = 1f;

        public static float ScaleMagnitude(EnemyRuntime enemy, float magnitude)
        {
            var affixes = enemy != null ? enemy.Affixes : null;
            return magnitude * EnemyAffixRules.CurseEffectiveness(affixes);
        }

        public static bool IsCurseStatus(StatusId id)
        {
            return id == StatusId.CurseFlammability
                || id == StatusId.CurseFrostbite
                || id == StatusId.CurseConductivity
                || id == StatusId.CurseDespair
                || id == StatusId.CurseVulnerability
                || id == StatusId.CurseTemporalChains
                || id == StatusId.CurseElementalWeakness;
        }

        public static bool TryResolve(
            CurseRoleDefinition role,
            int sourceLevel,
            out StatusId id,
            out float magnitude)
        {
            id = StatusId.Ignite;
            magnitude = 0f;
            if (role == null)
                return false;

            var less = role.ResolveEffect(RoleEffectKind.EnemyActionSpeedLessNormal, sourceLevel);
            if (less > 0.01f)
            {
                id = StatusId.CurseTemporalChains;
                magnitude = less;
                return true;
            }

            var fire = role.ResolveEffect(RoleEffectKind.EnemyFireResistance, sourceLevel);
            var cold = role.ResolveEffect(RoleEffectKind.EnemyColdResistance, sourceLevel);
            var lightning = role.ResolveEffect(RoleEffectKind.EnemyLightningResistance, sourceLevel);
            if (fire < -0.01f && cold < -0.01f && lightning < -0.01f)
            {
                id = StatusId.CurseElementalWeakness;
                magnitude = fire;
                return true;
            }

            if (fire < -0.01f)
            {
                id = StatusId.CurseFlammability;
                magnitude = fire;
                return true;
            }

            if (cold < -0.01f)
            {
                id = StatusId.CurseFrostbite;
                magnitude = cold;
                return true;
            }

            if (lightning < -0.01f)
            {
                id = StatusId.CurseConductivity;
                magnitude = lightning;
                return true;
            }

            var chaos = role.ResolveEffect(RoleEffectKind.EnemyChaosResistance, sourceLevel);
            if (chaos < -0.01f)
            {
                id = StatusId.CurseDespair;
                magnitude = chaos;
                return true;
            }

            var taken = role.ResolveEffect(RoleEffectKind.EnemyPhysicalDamageTakenIncreased, sourceLevel);
            if (taken > 0.01f)
            {
                id = StatusId.CurseVulnerability;
                magnitude = taken;
                return true;
            }

            return false;
        }
    }
}
