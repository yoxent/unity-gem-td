using GemTD.Gameplay.Combat;
using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Shared data for roles that can produce direct damage and impact splash.
    /// </summary>
    public abstract class DamageRoleDefinition : TowerRoleDefinition
    {
        [Tooltip("Finite uses SkillSpec.PierceCount (0 = no pierce). Infinite ignores count.")]
        public PierceMode PierceBehavior;

        [Tooltip("Direct aims at the selected enemy. Ground aims at a path intercept of that enemy.")]
        public AimMode AimMode;

        [Tooltip("Straight fires the skill volley. PayloadNova fires one payload, then a radial burst on land. CasterNova is an instant circle around the caster (Ice Nova damages; curse presence applies a hex). Rain falls from above onto a ground aim patch.")]
        public DeliveryPattern DeliveryPattern;

        [Tooltip("Fan width for simultaneous extra bolts. Zero = no fan.")]
        public float SpreadDegrees;

        [Tooltip("When > 0 and ProjectileCount > 1, Straight bolts spawn one after another instead of all at once.")]
        public float SequentialIntervalSeconds;

        [Tooltip("Sparse damage-type shares. Empty = untyped. Non-empty percents must sum to 100.")]
        public DamageTypeShare[] Mix;
    }
}
