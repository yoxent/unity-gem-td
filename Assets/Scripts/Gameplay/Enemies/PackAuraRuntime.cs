using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Combat;

namespace GemTD.Gameplay.Enemies
{
    public static class PackAuraRuntime
    {
        public static void Apply(List<EnemyRuntime> living)
        {
            if (living == null)
                return;

            var radiusSq = DamageTypeCombat.PackAuraRadius * DamageTypeCombat.PackAuraRadius;
            for (var t = 0; t < living.Count; t++)
            {
                var target = living[t];
                if (target == null || !target.IsAlive)
                    continue;

                var health = 0f;
                var speed = 0f;
                var armor = 0f;
                var shield = 0f;
                var spawnHp = target.SpawnMaxHealth;

                for (var s = 0; s < living.Count; s++)
                {
                    var source = living[s];
                    if (source == null || !source.IsAlive)
                        continue;

                    var self = ReferenceEquals(source, target);
                    if (!self)
                    {
                        var delta = source.WorldPosition - target.WorldPosition;
                        if (delta.sqrMagnitude > radiusSq)
                            continue;
                    }

                    var mul = self
                        ? DamageTypeCombat.PackAuraSelfMultiplier
                        : DamageTypeCombat.PackAuraAllyMultiplier;
                    var affixes = source.Affixes;
                    if (EnemyAffixRules.Contains(affixes, EnemyAffix.Bloody))
                    {
                        var bonus = spawnHp * DamageTypeCombat.PackAuraHealthFraction * mul;
                        if (bonus > health)
                            health = bonus;
                    }

                    if (EnemyAffixRules.Contains(affixes, EnemyAffix.Hunting))
                    {
                        var bonus = DamageTypeCombat.PackAuraSpeedFraction * mul;
                        if (bonus > speed)
                            speed = bonus;
                    }

                    if (EnemyAffixRules.Contains(affixes, EnemyAffix.Ironclad))
                    {
                        var bonus = DamageTypeCombat.PackAuraArmor * mul;
                        if (bonus > armor)
                            armor = bonus;
                    }

                    if (EnemyAffixRules.Contains(affixes, EnemyAffix.Shaded))
                    {
                        var bonus = spawnHp * DamageTypeCombat.PackAuraShieldFraction * mul;
                        if (bonus > shield)
                            shield = bonus;
                    }
                }

                target.ApplyPackBonuses(
                    Mathf.RoundToInt(armor),
                    health,
                    shield,
                    speed);
            }
        }
    }
}
