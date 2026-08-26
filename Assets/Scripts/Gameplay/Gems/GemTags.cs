using System.Text;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Gems
{
    /// <summary>
    /// PoE-style tags from sources/poe1_inspired_tower_support_gems.json.
    /// Socket rule: the tower must have every restriction tag on the gem.
    /// Support / Chaining / damage-type tags are not restrictions.
    /// </summary>
    [System.Flags]
    public enum GemTag
    {
        None = 0,
        Projectile = 1 << 0,
        Aoe = 1 << 1,
        Slam = 1 << 2,
        Attack = 1 << 3,
        Spell = 1 << 4,
        Aura = 1 << 5,
        Melee = 1 << 6,
        Chaining = 1 << 7,
        Support = 1 << 8,
    }

    public static class GemTags
    {
        public const GemTag RestrictionMask =
            GemTag.Projectile
            | GemTag.Aoe
            | GemTag.Slam
            | GemTag.Attack
            | GemTag.Spell
            | GemTag.Aura
            | GemTag.Melee;

        public static GemTag EffectiveTowerTags(TowerDefinition def)
        {
            if (def == null)
                return GemTag.None;
            if (def.Tags != GemTag.None)
                return def.Tags;

            var tags = GemTag.None;
            if (def.HasRole<AttackRoleDefinition>())
                tags |= GemTag.Attack | GemTag.Projectile;
            if (def.HasRole<SpellRoleDefinition>())
                tags |= GemTag.Spell;
            if (def.HasRole<AuraRoleDefinition>())
                tags |= GemTag.Aura;
            return tags;
        }

        public static GemTag EffectiveGemTags(GemDefinition gem)
        {
            if (gem == null)
                return GemTag.None;
            if (gem.Tags != GemTag.None)
                return gem.Tags;
            return InferGemTags(gem.Id);
        }

        public static GemTag EffectiveRequiredTags(GemDefinition gem)
        {
            return EffectiveGemTags(gem) & RestrictionMask;
        }

        public static bool CanSocket(TowerDefinition tower, GemDefinition gem)
        {
            return CanSocket(EffectiveTowerTags(tower), EffectiveRequiredTags(gem));
        }

        public static bool CanSocket(GemTag towerTags, GemTag required)
        {
            if (required == GemTag.None)
                return true;
            return (towerTags & required) == required;
        }

        public static string Format(GemTag tags)
        {
            if (tags == GemTag.None)
                return "—";

            var sb = new StringBuilder(48);
            Append(sb, tags, GemTag.Attack, "Attack");
            Append(sb, tags, GemTag.Projectile, "Projectile");
            Append(sb, tags, GemTag.Aoe, "AoE");
            Append(sb, tags, GemTag.Slam, "Slam");
            Append(sb, tags, GemTag.Spell, "Spell");
            Append(sb, tags, GemTag.Aura, "Aura");
            Append(sb, tags, GemTag.Melee, "Melee");
            Append(sb, tags, GemTag.Chaining, "Chaining");
            Append(sb, tags, GemTag.Support, "Support");
            return sb.Length > 0 ? sb.ToString() : "—";
        }

        static GemTag InferGemTags(GemId id)
        {
            switch (id)
            {
                case GemId.MultipleProjectiles:
                case GemId.Fork:
                case GemId.Pierce:
                case GemId.SlowerProjectiles:
                    return GemTag.Support | GemTag.Projectile;
                case GemId.Chain:
                    return GemTag.Support | GemTag.Chaining | GemTag.Projectile;
                case GemId.IncreasedArea:
                case GemId.ElementalProliferation:
                    return GemTag.Support | GemTag.Aoe;
                case GemId.FasterAttacks:
                    return GemTag.Attack | GemTag.Support;
                case GemId.Combustion:
                case GemId.AddedFireDamage:
                case GemId.AddedColdDamage:
                case GemId.AddedLightningDamage:
                case GemId.Knockback:
                    return GemTag.Support;
                default:
                    return GemTag.Support;
            }
        }

        static void Append(StringBuilder sb, GemTag tags, GemTag flag, string label)
        {
            if ((tags & flag) == 0)
                return;
            if (sb.Length > 0)
                sb.Append(", ");
            sb.Append(label);
        }
    }
}
