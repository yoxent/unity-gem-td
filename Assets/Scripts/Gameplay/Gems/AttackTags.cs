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
    public enum AttackTag
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

    public static class AttackTags
    {
        public const AttackTag RestrictionMask =
            AttackTag.Projectile
            | AttackTag.Aoe
            | AttackTag.Slam
            | AttackTag.Attack
            | AttackTag.Spell
            | AttackTag.Aura
            | AttackTag.Melee;

        public static AttackTag EffectiveTowerTags(TowerDefinition def)
        {
            if (def == null)
                return AttackTag.None;
            if (def.Tags != AttackTag.None)
                return def.Tags;

            switch (def.Kind)
            {
                case TowerKind.Projectile:
                    return AttackTag.Attack | AttackTag.Projectile;
                case TowerKind.Splash:
                    return AttackTag.Attack | AttackTag.Projectile | AttackTag.Aoe;
                case TowerKind.Aura:
                    return AttackTag.Aura;
                default:
                    return AttackTag.None;
            }
        }

        public static AttackTag EffectiveGemTags(GemDefinition gem)
        {
            if (gem == null)
                return AttackTag.None;
            if (gem.Tags != AttackTag.None)
                return gem.Tags;
            return InferGemTags(gem.Id);
        }

        public static AttackTag EffectiveRequiredTags(GemDefinition gem)
        {
            if (gem == null)
                return AttackTag.None;
            if (gem.RequiredTags != AttackTag.None)
                return gem.RequiredTags;
            return EffectiveGemTags(gem) & RestrictionMask;
        }

        public static bool CanSocket(TowerDefinition tower, GemDefinition gem)
        {
            return CanSocket(EffectiveTowerTags(tower), EffectiveRequiredTags(gem));
        }

        public static bool CanSocket(AttackTag towerTags, AttackTag required)
        {
            if (required == AttackTag.None)
                return true;
            return (towerTags & required) == required;
        }

        public static string Format(AttackTag tags)
        {
            if (tags == AttackTag.None)
                return "—";

            var sb = new StringBuilder(48);
            Append(sb, tags, AttackTag.Attack, "Attack");
            Append(sb, tags, AttackTag.Projectile, "Projectile");
            Append(sb, tags, AttackTag.Aoe, "AoE");
            Append(sb, tags, AttackTag.Slam, "Slam");
            Append(sb, tags, AttackTag.Spell, "Spell");
            Append(sb, tags, AttackTag.Aura, "Aura");
            Append(sb, tags, AttackTag.Melee, "Melee");
            Append(sb, tags, AttackTag.Chaining, "Chaining");
            Append(sb, tags, AttackTag.Support, "Support");
            return sb.Length > 0 ? sb.ToString() : "—";
        }

        static AttackTag InferGemTags(GemId id)
        {
            switch (id)
            {
                case GemId.Lmp:
                case GemId.Fork:
                case GemId.Pierce:
                case GemId.Gmp:
                case GemId.SlowerProjectiles:
                    return AttackTag.Support | AttackTag.Projectile;
                case GemId.Chain:
                    return AttackTag.Support | AttackTag.Chaining | AttackTag.Projectile;
                case GemId.IncreasedArea:
                case GemId.ElementalProliferation:
                    return AttackTag.Support | AttackTag.Aoe;
                case GemId.FasterAttacks:
                case GemId.IncreasedAccuracy:
                case GemId.AttackEcho:
                    return AttackTag.Attack | AttackTag.Support;
                default:
                    return AttackTag.Support;
            }
        }

        static void Append(StringBuilder sb, AttackTag tags, AttackTag flag, string label)
        {
            if ((tags & flag) == 0)
                return;
            if (sb.Length > 0)
                sb.Append(", ");
            sb.Append(label);
        }
    }
}
