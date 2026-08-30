using System.Text;
using UnityEngine;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Gems
{
    /// <summary>
    /// PoE skill-gem tags. Socket rule: the tower must have every restriction
    /// tag on the gem. Support, Chaining, and damage-type tags are not
    /// restrictions. Append only — serialized bit values must not move.
    /// Underlying type is long: the full PoE set does not fit in 32 bits.
    /// </summary>
    [System.Flags]
    public enum GemTag : long
    {
        None = 0,
        Projectile = 1L << 0,
        Aoe = 1L << 1,
        Slam = 1L << 2,
        Attack = 1L << 3,
        Spell = 1L << 4,
        Aura = 1L << 5,
        Melee = 1L << 6,
        Chaining = 1L << 7,
        Support = 1L << 8,
        Strike = 1L << 9,
        Arcane = 1L << 10,
        Blink = 1L << 11,
        Bow = 1L << 12,
        Brand = 1L << 13,
        Channeling = 1L << 14,
        Chaos = 1L << 15,
        Cold = 1L << 16,
        Critical = 1L << 17,
        Curse = 1L << 18,
        Duration = 1L << 19,
        Exceptional = 1L << 20,
        Fire = 1L << 21,
        Golem = 1L << 22,
        Guard = 1L << 23,
        Herald = 1L << 24,
        Hex = 1L << 25,
        Lightning = 1L << 26,
        Link = 1L << 27,
        Mark = 1L << 28,
        Mine = 1L << 29,
        Minion = 1L << 30,
        Movement = 1L << 31,
        Nova = 1L << 32,
        Orb = 1L << 33,
        Pact = 1L << 34,
        Physical = 1L << 35,
        Prismatic = 1L << 36,
        Retaliation = 1L << 37,
        Stance = 1L << 38,
        Totem = 1L << 39,
        Trap = 1L << 40,
        Travel = 1L << 41,
        Trigger = 1L << 42,
        Vaal = 1L << 43,
        Warcry = 1L << 44,
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
            | GemTag.Melee
            | GemTag.Strike;

        public static GemTag FromPoe(string poe)
        {
            if (string.IsNullOrEmpty(poe))
                return GemTag.None;
            switch (poe)
            {
                case "AoE": return GemTag.Aoe;
                case "Arcane": return GemTag.Arcane;
                case "Attack": return GemTag.Attack;
                case "Aura": return GemTag.Aura;
                case "Blink": return GemTag.Blink;
                case "Bow": return GemTag.Bow;
                case "Brand": return GemTag.Brand;
                case "Chaining": return GemTag.Chaining;
                case "Channelling":
                case "Channeling": return GemTag.Channeling;
                case "Chaos": return GemTag.Chaos;
                case "Cold": return GemTag.Cold;
                case "Critical": return GemTag.Critical;
                case "Curse": return GemTag.Curse;
                case "Duration": return GemTag.Duration;
                case "Exceptional": return GemTag.Exceptional;
                case "Fire": return GemTag.Fire;
                case "Golem": return GemTag.Golem;
                case "Guard": return GemTag.Guard;
                case "Herald": return GemTag.Herald;
                case "Hex": return GemTag.Hex;
                case "Lightning": return GemTag.Lightning;
                case "Link": return GemTag.Link;
                case "Mark": return GemTag.Mark;
                case "Melee": return GemTag.Melee;
                case "Mine": return GemTag.Mine;
                case "Minion": return GemTag.Minion;
                case "Movement": return GemTag.Movement;
                case "Nova": return GemTag.Nova;
                case "Orb": return GemTag.Orb;
                case "Pact": return GemTag.Pact;
                case "Physical": return GemTag.Physical;
                case "Prismatic": return GemTag.Prismatic;
                case "Projectile": return GemTag.Projectile;
                case "Retaliation": return GemTag.Retaliation;
                case "Slam": return GemTag.Slam;
                case "Spell": return GemTag.Spell;
                case "Stance": return GemTag.Stance;
                case "Strike": return GemTag.Strike;
                case "Support": return GemTag.Support;
                case "Totem": return GemTag.Totem;
                case "Trap": return GemTag.Trap;
                case "Travel": return GemTag.Travel;
                case "Trigger": return GemTag.Trigger;
                case "Vaal": return GemTag.Vaal;
                case "Warcry": return GemTag.Warcry;
                default: return GemTag.None;
            }
        }

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

        public static bool CanSocket(TowerDefinition tower, GemInstance gem)
        {
            return !gem.IsEmpty && CanSocket(tower, gem.Def);
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

            var sb = new StringBuilder(128);
            Append(sb, tags, GemTag.Attack, "Attack");
            Append(sb, tags, GemTag.Projectile, "Projectile");
            Append(sb, tags, GemTag.Aoe, "AoE");
            Append(sb, tags, GemTag.Slam, "Slam");
            Append(sb, tags, GemTag.Spell, "Spell");
            Append(sb, tags, GemTag.Aura, "Aura");
            Append(sb, tags, GemTag.Melee, "Melee");
            Append(sb, tags, GemTag.Strike, "Strike");
            Append(sb, tags, GemTag.Chaining, "Chaining");
            Append(sb, tags, GemTag.Support, "Support");
            Append(sb, tags, GemTag.Arcane, "Arcane");
            Append(sb, tags, GemTag.Blink, "Blink");
            Append(sb, tags, GemTag.Bow, "Bow");
            Append(sb, tags, GemTag.Brand, "Brand");
            Append(sb, tags, GemTag.Channeling, "Channeling");
            Append(sb, tags, GemTag.Chaos, "Chaos");
            Append(sb, tags, GemTag.Cold, "Cold");
            Append(sb, tags, GemTag.Critical, "Critical");
            Append(sb, tags, GemTag.Curse, "Curse");
            Append(sb, tags, GemTag.Duration, "Duration");
            Append(sb, tags, GemTag.Exceptional, "Exceptional");
            Append(sb, tags, GemTag.Fire, "Fire");
            Append(sb, tags, GemTag.Golem, "Golem");
            Append(sb, tags, GemTag.Guard, "Guard");
            Append(sb, tags, GemTag.Herald, "Herald");
            Append(sb, tags, GemTag.Hex, "Hex");
            Append(sb, tags, GemTag.Lightning, "Lightning");
            Append(sb, tags, GemTag.Link, "Link");
            Append(sb, tags, GemTag.Mark, "Mark");
            Append(sb, tags, GemTag.Mine, "Mine");
            Append(sb, tags, GemTag.Minion, "Minion");
            Append(sb, tags, GemTag.Movement, "Movement");
            Append(sb, tags, GemTag.Nova, "Nova");
            Append(sb, tags, GemTag.Orb, "Orb");
            Append(sb, tags, GemTag.Pact, "Pact");
            Append(sb, tags, GemTag.Physical, "Physical");
            Append(sb, tags, GemTag.Prismatic, "Prismatic");
            Append(sb, tags, GemTag.Retaliation, "Retaliation");
            Append(sb, tags, GemTag.Stance, "Stance");
            Append(sb, tags, GemTag.Totem, "Totem");
            Append(sb, tags, GemTag.Trap, "Trap");
            Append(sb, tags, GemTag.Travel, "Travel");
            Append(sb, tags, GemTag.Trigger, "Trigger");
            Append(sb, tags, GemTag.Vaal, "Vaal");
            Append(sb, tags, GemTag.Warcry, "Warcry");
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

    public sealed class GemTagMaskAttribute : PropertyAttribute
    {
    }
}
