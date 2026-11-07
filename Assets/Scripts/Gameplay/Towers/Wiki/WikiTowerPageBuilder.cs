using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;

namespace GemTD.Gameplay.Towers
{
    public static class WikiTowerPageBuilder
    {
        public static WikiTowerPage From(TowerDefinition tower, WikiTowerCatalogEntry entry)
        {
            var fire = ResolveRole(tower);
            var damage = fire as DamageRoleDefinition;
            var firstLevel = FirstSourceLevel(fire);
            var lastLevel = LastSourceLevel(fire, firstLevel);

            return new WikiTowerPage
            {
                Slug = entry.Slug,
                DisplayName = tower != null && !string.IsNullOrEmpty(tower.DisplayName) ? tower.DisplayName : entry.Slug.Replace('_', ' '),
                Description = tower != null ? tower.Description : null,
                CategoryName = entry.CategoryName,
                CategoryFolder = entry.CategoryFolder,
                StatusLabel = entry.StatusLabel,
                InTowerCatalog = entry.InTowerCatalog,
                Tags = tower != null ? GemTags.Format(tower.Tags) : "—",
                Cost = tower != null ? tower.Cost : 0,
                SocketCount = tower != null ? tower.SocketCount : 0,
                RoleKind = RoleKindName(fire),
                AimMode = damage != null ? damage.AimMode.ToString() : "—",
                DeliveryPattern = damage != null ? damage.DeliveryPattern.ToString() : "—",
                Mix = FormatMix(damage),
                SpreadDegrees = damage != null ? FormatNumber(damage.SpreadDegrees) : "—",
                SequentialIntervalSeconds = damage != null ? FormatNumber(damage.SequentialIntervalSeconds) : "—",
                FirstSourceLevel = firstLevel,
                LastSourceLevel = lastLevel,
                First = Snapshot(tower, fire, firstLevel),
                Last = Snapshot(tower, fire, lastLevel),
                BaseModifiers = FormatModifiers(fire != null ? fire.Modifiers : null),
                EffectLines = FormatEffects(fire),
                PayloadLines = FormatPayloads(fire != null ? fire.EffectPayloads : null)
            };
        }

        static TowerRoleDefinition ResolveRole(TowerDefinition tower)
        {
            if (tower == null)
                return null;

            var fire = tower.FireRole;
            if (fire != null)
                return fire;

            return tower.GetRole<AuraRoleDefinition>();
        }

        static WikiTowerLevelSnapshot Snapshot(TowerDefinition tower, TowerRoleDefinition fire, int sourceLevel)
        {
            if (tower == null)
            {
                return new WikiTowerLevelSnapshot { SourceLevel = sourceLevel };
            }

            var damageRange = tower.GetDamageRange(sourceLevel);
            var usesAttack = fire != null && fire.UsesAttackSpeed;
            var usesCast = fire != null && !fire.UsesAttackSpeed && fire.GetBaseFireInterval(sourceLevel) > 0f;
            var reservation = fire != null ? fire.ResolveStat(RoleStat.ReservationPercent, sourceLevel) : 0f;
            return new WikiTowerLevelSnapshot
            {
                SourceLevel = sourceLevel,
                Damage = FormatStatValue(damageRange),
                TowerRadius = FormatNumber(tower.GetPlacementTowerRadius(sourceLevel)),
                SplashRadius = FormatNumber(tower.GetSplashRadius(sourceLevel)),
                ProjectileCount = tower.GetProjectileCount(sourceLevel),
                ChainCount = tower.GetChainCount(sourceLevel),
                ForkCount = tower.GetForkCount(sourceLevel),
                AttackTime = usesAttack ? FormatNumber(fire.ResolveStat(RoleStat.AttackTime, sourceLevel)) : "—",
                AttackSpeed = usesAttack ? FormatNumber(fire.ResolveStat(RoleStat.AttackSpeed, sourceLevel)) : "—",
                CastTime = usesCast ? FormatNumber(fire.ResolveStat(RoleStat.CastTime, sourceLevel)) : "—",
                CastSpeed = usesCast ? FormatNumber(fire.ResolveStat(RoleStat.CastSpeed, sourceLevel)) : "—",
                ReservationPercent = reservation > 0f ? FormatNumber(reservation) : "—",
                FireInterval = FormatNumber(tower.GetBaseFireInterval(sourceLevel))
            };
        }

        static int FirstSourceLevel(TowerRoleDefinition fire)
        {
            if (fire?.Levels == null || fire.Levels.Length == 0)
                return TowerInstance.DefaultLevel;

            var first = int.MaxValue;
            for (var i = 0; i < fire.Levels.Length; i++)
            {
                var level = fire.Levels[i];
                if (level == null)
                    continue;
                if (level.SourceLevel < first)
                    first = level.SourceLevel;
            }

            return first == int.MaxValue ? TowerInstance.DefaultLevel : first;
        }

        static int LastSourceLevel(TowerRoleDefinition fire, int fallback)
        {
            if (fire?.Levels == null || fire.Levels.Length == 0)
                return fallback;

            var last = int.MinValue;
            for (var i = 0; i < fire.Levels.Length; i++)
            {
                var level = fire.Levels[i];
                if (level == null)
                    continue;
                if (level.SourceLevel > last)
                    last = level.SourceLevel;
            }

            return last == int.MinValue ? fallback : last;
        }

        static string RoleKindName(TowerRoleDefinition fire)
        {
            if (fire is AttackRoleDefinition)
                return "Attack";
            if (fire is SpellRoleDefinition)
                return "Spell";
            if (fire is CurseRoleDefinition)
                return "Curse";
            if (fire is AuraRoleDefinition)
                return "Aura";
            if (fire is TrapRoleDefinition)
                return "Trap";
            if (fire is MineRoleDefinition)
                return "Mine";
            return "—";
        }

        static string FormatMix(DamageRoleDefinition damage)
        {
            if (damage == null || DamageMix.IsEmpty(damage.Mix))
                return "—";

            var sb = new StringBuilder(64);
            for (var i = 0; i < damage.Mix.Length; i++)
            {
                if (sb.Length > 0)
                    sb.Append(" / ");
                sb.Append(damage.Mix[i].Type);
                sb.Append(' ');
                sb.Append(damage.Mix[i].Percent);
            }

            return sb.ToString();
        }

        static string[] FormatModifiers(RoleStatModifier[] modifiers)
        {
            if (modifiers == null || modifiers.Length == 0)
                return System.Array.Empty<string>();

            var lines = new List<string>(modifiers.Length);
            for (var i = 0; i < modifiers.Length; i++)
            {
                var modifier = modifiers[i];
                lines.Add(
                    modifier.Stat + " " + modifier.Operation + " " +
                    FormatOperand(modifier.ValueKind, modifier.OperandMin, modifier.OperandMax));
            }

            return lines.ToArray();
        }

        static string[] FormatEffects(TowerRoleDefinition fire)
        {
            if (fire == null)
                return System.Array.Empty<string>();

            var lines = new List<string>();
            AppendEffects(lines, "Base", fire.Effects);
            if (fire.Levels != null)
            {
                var first = FirstSourceLevel(fire);
                var last = LastSourceLevel(fire, first);
                var firstLevel = FindLevel(fire, first);
                var lastLevel = FindLevel(fire, last);
                if (firstLevel != null)
                    AppendEffects(lines, "L" + first, firstLevel.Effects);
                if (lastLevel != null && last != first)
                    AppendEffects(lines, "L" + last, lastLevel.Effects);
            }

            return lines.Count == 0 ? System.Array.Empty<string>() : lines.ToArray();
        }

        static void AppendEffects(List<string> lines, string prefix, RoleEffectModifier[] effects)
        {
            if (effects == null)
                return;

            for (var i = 0; i < effects.Length; i++)
            {
                var effect = effects[i];
                lines.Add(
                    prefix + ": " + effect.Kind + " " + effect.Operation + " " +
                    FormatOperand(effect.ValueKind, effect.OperandMin, effect.OperandMax));
            }
        }

        static RoleLevelDefinition FindLevel(TowerRoleDefinition fire, int sourceLevel)
        {
            if (fire.Levels == null)
                return null;

            for (var i = 0; i < fire.Levels.Length; i++)
            {
                var level = fire.Levels[i];
                if (level != null && level.SourceLevel == sourceLevel)
                    return level;
            }

            return null;
        }

        static string[] FormatPayloads(EffectPayloadDefinition[] payloads)
        {
            if (payloads == null || payloads.Length == 0)
                return System.Array.Empty<string>();

            var lines = new List<string>(payloads.Length);
            for (var i = 0; i < payloads.Length; i++)
            {
                var payload = payloads[i];
                if (payload == null)
                    continue;

                var sb = new StringBuilder(96);
                sb.Append(payload.Trigger);
                sb.Append(' ');
                sb.Append(payload.TravelPattern);
                sb.Append(' ');
                sb.Append(payload.ScatterPattern);
                sb.Append(" ×");
                sb.Append(payload.Count);
                sb.Append(", damage ×");
                sb.Append(FormatNumber(payload.DamageMultiplier));
                if (payload.AoeRadius > 0f)
                {
                    sb.Append(", AoE ");
                    sb.Append(FormatNumber(payload.AoeRadius));
                    sb.Append("m");
                }

                if (payload.MaxDistance > 0f)
                {
                    sb.Append(", ring ");
                    sb.Append(FormatNumber(payload.MinDistance));
                    sb.Append("–");
                    sb.Append(FormatNumber(payload.MaxDistance));
                    sb.Append("m");
                }

                if (payload.DelaySeconds > 0f)
                {
                    sb.Append(", delay ");
                    sb.Append(FormatNumber(payload.DelaySeconds));
                    sb.Append("s");
                }

                lines.Add(sb.ToString());
            }

            return lines.Count == 0 ? System.Array.Empty<string>() : lines.ToArray();
        }

        static string FormatOperand(RoleStatValueKind kind, float min, float max)
        {
            if (kind == RoleStatValueKind.Range)
                return FormatNumber(min) + "–" + FormatNumber(max);
            return FormatNumber(min);
        }

        static string FormatStatValue(RoleStatValue value)
        {
            if (value.IsRange)
                return FormatNumber(value.Min) + "–" + FormatNumber(value.Max);
            return FormatNumber(value.Min);
        }

        public static string FormatNumber(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return "0";
            if (Mathf.Abs(value - Mathf.Round(value)) < 0.0001f)
                return ((int)Mathf.Round(value)).ToString();
            return value.ToString("0.##");
        }
    }
}
