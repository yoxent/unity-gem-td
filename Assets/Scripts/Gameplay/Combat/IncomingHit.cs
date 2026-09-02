using GemTD.Gameplay.Enemies;
using UnityEngine;

namespace GemTD.Gameplay.Combat
{
    public static class IncomingHit
    {
        public const float ResistFloor = -2f;
        public const float ResistCeiling = 0.9f;
        public const float ArmourDrCap = 0.9f;

        public static bool IsUntyped(in SkillSpec spec)
        {
            return spec.MixPhysical <= 0f
                && spec.MixFire <= 0f
                && spec.MixCold <= 0f
                && spec.MixLightning <= 0f
                && spec.MixChaos <= 0f;
        }

        public static float ApplyCrit(float damage, in SkillSpec spec, double unitInterval)
        {
            if (damage <= 0f)
                return 0f;

            var chance = spec.CritChance;
            if (chance <= 0f || unitInterval >= chance)
                return damage;

            var multiplier = spec.CritMultiplier > 0f ? spec.CritMultiplier : 1.5f;
            return damage * multiplier;
        }

        public static float Mitigate(
            float damage,
            in SkillSpec spec,
            EnemyRuntime enemy,
            StatusRuntime statuses)
        {
            if (damage <= 0f || enemy == null)
                return 0f;

            if (IsUntyped(spec))
                return Mathf.Max(0f, damage - enemy.Armor);

            var physical = ApplyArmour(damage * spec.MixPhysical, enemy.Armor);
            physical = ApplyVulnerability(physical, enemy, statuses);

            var fire = ApplyResist(
                damage * spec.MixFire,
                enemy.FireResistance,
                enemy,
                statuses,
                StatusId.CurseFlammability,
                elemental: true);
            var cold = ApplyResist(
                damage * spec.MixCold,
                enemy.ColdResistance,
                enemy,
                statuses,
                StatusId.CurseFrostbite,
                elemental: true);
            var lightning = ApplyResist(
                damage * spec.MixLightning,
                enemy.LightningResistance,
                enemy,
                statuses,
                StatusId.CurseConductivity,
                elemental: true);
            var chaos = ApplyResist(
                damage * spec.MixChaos,
                enemy.ChaosResistance,
                enemy,
                statuses,
                StatusId.CurseDespair,
                elemental: false);

            if (enemy.ShieldHp > 0f)
                chaos *= DamageTypeCombat.ChaosVsShieldMultiplier;

            return physical + fire + cold + lightning + chaos;
        }

        static float ApplyArmour(float rawPhysical, int armour)
        {
            if (rawPhysical <= 0f)
                return 0f;
            if (armour <= 0)
                return rawPhysical;

            var dr = armour / (armour + 5f * rawPhysical);
            if (dr > ArmourDrCap)
                dr = ArmourDrCap;
            return rawPhysical * (1f - dr);
        }

        static float ApplyVulnerability(float physical, EnemyRuntime enemy, StatusRuntime statuses)
        {
            if (physical <= 0f || statuses == null)
                return physical;
            if (!statuses.TryGetMagnitude(enemy, StatusId.CurseVulnerability, out var taken))
                return physical;
            return physical * (1f + taken / 100f);
        }

        static float ApplyResist(
            float amount,
            int baseResist,
            EnemyRuntime enemy,
            StatusRuntime statuses,
            StatusId curseId,
            bool elemental)
        {
            if (amount <= 0f)
                return 0f;

            var percent = (float)baseResist;
            if (statuses != null && statuses.TryGetMagnitude(enemy, curseId, out var mag))
                percent += mag;
            if (elemental
                && statuses != null
                && statuses.TryGetMagnitude(enemy, StatusId.CurseElementalWeakness, out var elementalMag))
                percent += elementalMag;

            var fraction = percent / 100f;
            if (fraction < ResistFloor)
                fraction = ResistFloor;
            if (fraction > ResistCeiling)
                fraction = ResistCeiling;
            return amount * (1f - fraction);
        }
    }
}
