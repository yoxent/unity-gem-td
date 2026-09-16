using System;
using System.Collections.Generic;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Towers;
using Newtonsoft.Json.Linq;

namespace GemTD.Gameplay.Gems
{
    /// <summary>
    /// Maps one crawled support-gem JSON object to GemDefinition fields.
    /// EditMode-testable. Does not create assets.
    /// </summary>
    public static class SupportGemMap
    {
        public const int ExpectedGemCount = 210;
        public const float ExceptionalNormalMultiplier = 1.4f;
        public const int DefaultLesserSampleLevel = 3;
        public const int DefaultNormalSampleLevel = 5;
        public const int DefaultGreaterSampleLevel = 7;

        public readonly struct RaritySampleLevels
        {
            public readonly int Lesser;
            public readonly int Normal;
            public readonly int Greater;

            public static readonly RaritySampleLevels Default = new RaritySampleLevels(
                DefaultLesserSampleLevel,
                DefaultNormalSampleLevel,
                DefaultGreaterSampleLevel);

            public RaritySampleLevels(int lesser, int normal, int greater)
            {
                Lesser = lesser;
                Normal = normal;
                Greater = greater;
            }
        }

        struct TierValues
        {
            public readonly float Lesser;
            public readonly float Normal;
            public readonly float Greater;

            public TierValues(float lesser, float normal, float greater)
            {
                Lesser = lesser;
                Normal = normal;
                Greater = greater;
            }
        }

        public sealed class UnmappedMod
        {
            public string Text;
            public float Lesser;
            public float Normal;
            public float Greater;
        }

        public sealed class Result
        {
            public string DisplayName;
            public string Slug;
            public GemTag Tags;
            public string Description;
            public GemStatModifier[] Modifiers;
            public string[] FlavorTexts;
            public UnmappedMod[] Unmapped;
            public string SkipReason;
            public bool CanIngest =>
                string.IsNullOrEmpty(SkipReason) && (Unmapped == null || Unmapped.Length == 0);
        }

        public static Result[] FromCatalogJson(string fileJson)
        {
            var root = JObject.Parse(fileJson);
            var rarityLevels = ReadRaritySampleLevels(root);
            var gems = (JArray)root["gems"];
            var results = new Result[gems.Count];
            for (var i = 0; i < gems.Count; i++)
                results[i] = FromObject((JObject)gems[i], rarityLevels);
            return results;
        }

        public static Result FromGemJson(string gemJson)
        {
            return FromObject(JObject.Parse(gemJson));
        }

        public static Result FromObject(JObject gem)
        {
            return FromObject(gem, RaritySampleLevels.Default);
        }

        public static Result FromObject(JObject gem, RaritySampleLevels rarityLevels)
        {
            var name = gem["name"]?.Value<string>() ?? "";
            var upside = gem["upside"]?.Value<string>() ?? "";
            var downside = gem["downside"]?.Value<string>() ?? "";
            var description = upside;
            if (!string.IsNullOrEmpty(downside))
                description = string.IsNullOrEmpty(description) ? downside : upside + " " + downside;

            var modifiers = new List<GemStatModifier>(4);
            var flavor = new List<string>(4);
            var unmapped = new List<UnmappedMod>(4);
            var mods = gem["explicitMods"] as JArray;
            var category = gem["category"]?.Value<string>() ?? "";
            var skipReason = ForceSkipReason(name, category, gem["tags"] as JArray);
            AddHallowBase(name, modifiers);
            if (TryMapExceptional(name, modifiers))
            {
                CollectFlavorOnly(mods, flavor);
            }
            else if (mods != null)
            {
                for (var i = 0; i < mods.Count; i++)
                    ClassifyMod((JObject)mods[i], rarityLevels, modifiers, flavor, unmapped);
            }

            if (string.IsNullOrEmpty(skipReason))
                ApplyWikiCardStatics(name, modifiers, flavor);

            if (unmapped.Count > 0)
                modifiers.Clear();

            if (!string.IsNullOrEmpty(skipReason))
            {
                modifiers.Clear();
                unmapped.Clear();
            }

            return new Result
            {
                DisplayName = TrimSupportSuffix(name),
                Slug = SlugFromName(name),
                Tags = MapTags(gem["tags"] as JArray),
                Description = description,
                Modifiers = modifiers.ToArray(),
                FlavorTexts = flavor.ToArray(),
                Unmapped = unmapped.ToArray(),
                SkipReason = skipReason
            };
        }

        static void ClassifyMod(
            JObject mod,
            RaritySampleLevels rarityLevels,
            List<GemStatModifier> modifiers,
            List<string> flavor,
            List<UnmappedMod> unmapped)
        {
            var text = mod["text"]?.Value<string>() ?? "";
            var valuesObject = mod["values"];
            if (valuesObject == null || valuesObject.Type == JTokenType.Null)
            {
                flavor.Add(text);
                return;
            }

            var values = ReadValues(valuesObject as JObject, rarityLevels);
            if (TryMapNumbered(text, values, modifiers))
                return;

            unmapped.Add(new UnmappedMod
            {
                Text = text,
                Lesser = values.Lesser,
                Normal = values.Normal,
                Greater = values.Greater
            });
        }

        static RaritySampleLevels ReadRaritySampleLevels(JObject root)
        {
            var obj = root["rarity_sample_levels"] as JObject;
            if (obj == null)
                return RaritySampleLevels.Default;
            return new RaritySampleLevels(
                ReadInt(obj, "lesser", DefaultLesserSampleLevel),
                ReadInt(obj, "normal", DefaultNormalSampleLevel),
                ReadInt(obj, "greater", DefaultGreaterSampleLevel));
        }

        static int ReadInt(JObject obj, string key, int fallback)
        {
            var token = obj[key];
            if (token == null || token.Type == JTokenType.Null)
                return fallback;
            return token.Value<int>();
        }

        static TierValues ReadValues(JObject values, RaritySampleLevels rarityLevels)
        {
            if (values == null)
                return default;

            if (HasNamedRarityKeys(values))
            {
                var normal = ReadValue(values, "normal", 0f);
                var lesser = ReadValue(values, "lesser", normal);
                var greater = ReadValue(values, "greater", normal);
                return new TierValues(lesser, normal, greater);
            }

            return new TierValues(
                ReadSampledLevel(values, rarityLevels.Lesser),
                ReadSampledLevel(values, rarityLevels.Normal),
                ReadSampledLevel(values, rarityLevels.Greater));
        }

        static bool HasNamedRarityKeys(JObject values)
        {
            return values["lesser"] != null
                || values["normal"] != null
                || values["greater"] != null;
        }

        static float ReadSampledLevel(JObject values, int level)
        {
            for (var l = level; l >= 1; l--)
            {
                if (TryReadValue(values, l.ToString(), out var found))
                    return found;
            }

            for (var l = level + 1; l <= 10; l++)
            {
                if (TryReadValue(values, l.ToString(), out var found))
                    return found;
            }

            return 0f;
        }

        static float ReadValue(JObject values, string key, float fallback)
        {
            if (TryReadValue(values, key, out var found))
                return found;
            return fallback;
        }

        static bool TryReadValue(JObject values, string key, out float found)
        {
            found = 0f;
            var token = values[key];
            if (token == null || token.Type == JTokenType.Null)
                return false;
            if (token.Type == JTokenType.Array)
            {
                if (token.First == null || token.First.Type == JTokenType.Null)
                    return false;
                found = token.First.Value<float>();
                return true;
            }

            found = token.Value<float>();
            return true;
        }

        static GemStatModifier Tiered(
            GemStat stat,
            RoleModifierOperation operation,
            TierValues values,
            float falloff = 0f)
        {
            return GemStatModifier.TieredSingle(
                stat,
                operation,
                values.Lesser,
                values.Normal,
                values.Greater,
                falloff);
        }

        static TierValues PercentFactor(TierValues values, bool more)
        {
            var sign = more ? 1f : -1f;
            return new TierValues(
                1f + sign * values.Lesser / 100f,
                1f + sign * values.Normal / 100f,
                1f + sign * values.Greater / 100f);
        }

        static TierValues Fraction(TierValues values)
        {
            return new TierValues(
                values.Lesser / 100f,
                values.Normal / 100f,
                values.Greater / 100f);
        }

        static bool TryMapNumbered(string text, TierValues values, List<GemStatModifier> modifiers)
        {
            if (TryMapHitsAndAilments(text, values, modifiers))
                return true;

            if (text == "Supported Skills have #% increased Effect of non-Damaging Ailments on Enemies")
            {
                var factor = PercentFactor(values, more: true);
                modifiers.Add(Tiered(
                    GemStat.ChillEffect,
                    RoleModifierOperation.Multiply,
                    factor));
                modifiers.Add(Tiered(
                    GemStat.ShockEffect,
                    RoleModifierOperation.Multiply,
                    factor));
                return true;
            }

            if (TryMapNumberedSingle(text, values, out var mapped))
            {
                modifiers.Add(mapped);
                return true;
            }

            return false;
        }

        static bool TryMapNumberedSingle(string text, TierValues values, out GemStatModifier mapped)
        {
            mapped = default;
            if (text == "Supported Skills deal #% more Damage"
                || text == "Supported Attacks deal #% more Damage")
            {
                mapped = MoreDamage(values);
                return true;
            }

            if (text == "Supported Skills deal #% less Damage"
                || text == "Supported Attacks deal #% less Damage")
            {
                mapped = LessDamage(values);
                return true;
            }

            if (IsConditional(text))
                return false;

            if (TryMapHitDamageLine(text, values, out mapped))
                return true;

            if (TryMapAilmentDamageLine(text, values, out mapped))
                return true;

            if (text == "Supported Attacks have #% chance to cause Bleeding"
                || text == "Supported Skills have #% chance to cause Bleeding")
            {
                mapped = Tiered(
                    GemStat.BleedChance,
                    RoleModifierOperation.Set,
                    Fraction(values));
                return true;
            }

            if (TryMapChance(text, values, out mapped))
                return true;

            if (text == "#% increased Effect of Chill inflicted with Supported Skills")
            {
                mapped = Tiered(
                    GemStat.ChillEffect,
                    RoleModifierOperation.Multiply,
                    PercentFactor(values, more: true));
                return true;
            }

            if (text == "#% increased Effect of Shock inflicted with Supported Skills")
            {
                mapped = Tiered(
                    GemStat.ShockEffect,
                    RoleModifierOperation.Multiply,
                    PercentFactor(values, more: true));
                return true;
            }

            if (text == "Supported Skills gain #% of Physical Damage as Extra Fire Damage"
                || text == "Supported Attacks gain #% of Physical Damage as Extra Fire Damage")
            {
                mapped = Tiered(
                    GemStat.PhysAsExtraFire,
                    RoleModifierOperation.Set,
                    Fraction(values));
                return true;
            }

            if (text == "Supported Skills gain #% of Physical Damage as Extra Cold Damage"
                || text == "Supported Attacks gain #% of Physical Damage as Extra Cold Damage")
            {
                mapped = Tiered(
                    GemStat.PhysAsExtraCold,
                    RoleModifierOperation.Set,
                    Fraction(values));
                return true;
            }

            if (text == "Supported Skills gain #% of Physical Damage as Extra Lightning Damage"
                || text == "Supported Attacks gain #% of Physical Damage as Extra Lightning Damage")
            {
                mapped = Tiered(
                    GemStat.PhysAsExtraLightning,
                    RoleModifierOperation.Set,
                    Fraction(values));
                return true;
            }

            if (text == "Supported Skills gain #% of Physical Damage as Extra Chaos Damage"
                || text == "Supported Attacks gain #% of Physical Damage as Extra Chaos Damage")
            {
                mapped = Tiered(
                    GemStat.PhysAsExtraChaos,
                    RoleModifierOperation.Set,
                    Fraction(values));
                return true;
            }

            if (text == "#% increased magnitude of Hallowing Flame inflicted by Supported Skills")
            {
                mapped = Tiered(
                    GemStat.PhysAsExtraFire,
                    RoleModifierOperation.Multiply,
                    PercentFactor(values, more: true));
                return true;
            }

            if (TryMapConversion(text, values, out mapped))
                return true;

            if (TryMapSpeedAoeDelivery(text, values, out mapped))
                return true;

            if (text == "Supported Skills have #% chance to Knock Enemies Back on hit")
            {
                mapped = Tiered(
                    GemStat.KnockbackChance,
                    RoleModifierOperation.Set,
                    Fraction(values));
                return true;
            }

            if (text.Contains("added Chaos Damage")
                || text.Contains("added Cold Damage")
                || text.Contains("added Lightning Damage")
                || text.Contains("added Fire Damage")
                || text.Contains("added Physical Damage"))
            {
                mapped = Tiered(GemStat.Damage, RoleModifierOperation.Add, values);
                return true;
            }

            if (text == "#% increased Duration of Ailments inflicted with Supported Skills"
                || text == "#% increased Duration of Elemental Ailments on Enemies")
            {
                mapped = Tiered(
                    GemStat.AilmentDuration,
                    RoleModifierOperation.Multiply,
                    PercentFactor(values, more: true));
                return true;
            }

            if (text == "Elemental Ailments inflicted by Supported Skills spread to other enemies within # metres")
            {
                mapped = GemStatModifier.Single(
                    GemStat.Proliferate,
                    RoleModifierOperation.Set,
                    1f);
                return true;
            }

            return false;
        }

        static bool TryMapHitsAndAilments(string text, TierValues values, List<GemStatModifier> modifiers)
        {
            if (IsConditional(text))
                return false;
            if (text.IndexOf("Hits and Ailments", StringComparison.Ordinal) < 0)
                return false;
            if (text.IndexOf("more", StringComparison.Ordinal) >= 0)
            {
                modifiers.Add(MoreDamage(values));
                modifiers.Add(MoreAilment(values));
                return true;
            }

            if (text.IndexOf("less", StringComparison.Ordinal) >= 0)
            {
                modifiers.Add(LessDamage(values));
                modifiers.Add(LessAilment(values));
                return true;
            }

            return false;
        }

        static bool TryMapHitDamageLine(string text, TierValues values, out GemStatModifier mapped)
        {
            mapped = default;
            var more = text.IndexOf(" more ", StringComparison.Ordinal) >= 0
                || text.IndexOf("deal #% more", StringComparison.Ordinal) >= 0;
            var less = text.IndexOf(" less ", StringComparison.Ordinal) >= 0
                || text.IndexOf("deal #% less", StringComparison.Ordinal) >= 0;
            var increased = text.IndexOf("increased", StringComparison.Ordinal) >= 0;
            var reduced = text.IndexOf("reduced", StringComparison.Ordinal) >= 0;
            if (!more && !less && !increased && !reduced)
                return false;
            if (text.IndexOf("Ailment", StringComparison.Ordinal) >= 0)
                return false;
            if (text.IndexOf("Ignite", StringComparison.Ordinal) >= 0)
                return false;
            if (text.IndexOf("Bleed", StringComparison.Ordinal) >= 0)
                return false;
            if (text.IndexOf("Poison", StringComparison.Ordinal) >= 0)
                return false;
            if (text.IndexOf("Burning", StringComparison.Ordinal) >= 0)
                return false;
            if (text.IndexOf("over Time", StringComparison.Ordinal) >= 0)
                return false;
            if (text.IndexOf("Damage", StringComparison.Ordinal) < 0)
                return false;
            if (text.IndexOf("Leech", StringComparison.Ordinal) >= 0)
                return false;
            if (text.IndexOf("Minion", StringComparison.Ordinal) >= 0)
                return false;
            if (text.IndexOf("Speed", StringComparison.Ordinal) >= 0)
                return false;
            if (text.IndexOf("Area of Effect", StringComparison.Ordinal) >= 0)
                return false;

            if (more || increased)
            {
                mapped = MoreDamage(values);
                return true;
            }

            mapped = LessDamage(values);
            return true;
        }

        static bool TryMapAilmentDamageLine(string text, TierValues values, out GemStatModifier mapped)
        {
            mapped = default;
            var ailment = text.IndexOf("Ailment", StringComparison.Ordinal) >= 0
                || text.IndexOf("Ignite", StringComparison.Ordinal) >= 0
                || text.IndexOf("Bleed", StringComparison.Ordinal) >= 0
                || text.IndexOf("Poison", StringComparison.Ordinal) >= 0
                || text.IndexOf("Burning", StringComparison.Ordinal) >= 0
                || text.IndexOf("over Time", StringComparison.Ordinal) >= 0;
            if (!ailment)
                return false;
            if (text.IndexOf("chance", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (text.IndexOf("Duration", StringComparison.Ordinal) >= 0)
                return false;
            if (text.IndexOf("Effect", StringComparison.Ordinal) >= 0)
                return false;
            if (text.IndexOf("Damage", StringComparison.Ordinal) < 0)
                return false;
            if (text.IndexOf("Hits for each", StringComparison.Ordinal) >= 0)
                return false;

            var less = text.IndexOf(" less ", StringComparison.Ordinal) >= 0;
            mapped = less ? LessAilment(values) : MoreAilment(values);
            return true;
        }

        static bool TryMapSpeedAoeDelivery(string text, TierValues values, out GemStatModifier mapped)
        {
            mapped = default;
            if (text == "Supported Skills have #% increased Attack Speed"
                || text == "Supported Attacks have #% increased Attack Speed")
            {
                mapped = Tiered(
                    GemStat.AttackSpeedMultiplier,
                    RoleModifierOperation.Multiply,
                    PercentFactor(values, more: true));
                return true;
            }

            if (text == "Supported Skills have #% increased Cast Speed")
            {
                mapped = Tiered(
                    GemStat.CastSpeedMultiplier,
                    RoleModifierOperation.Multiply,
                    PercentFactor(values, more: true));
                return true;
            }

            if (text == "Supported Skills have #% more Cast Speed"
                || text == "Supported Skills have #% more Melee Attack Speed")
            {
                var stat = text.IndexOf("Cast", StringComparison.Ordinal) >= 0
                    ? GemStat.CastSpeedMultiplier
                    : GemStat.AttackSpeedMultiplier;
                mapped = Tiered(
                    stat,
                    RoleModifierOperation.Multiply,
                    PercentFactor(values, more: true));
                return true;
            }

            if (text == "Supported Skills have #% less Attack Speed"
                || text == "Supported Skills have #% less Attack Damage")
            {
                if (text.IndexOf("Damage", StringComparison.Ordinal) >= 0)
                {
                    mapped = LessDamage(values);
                    return true;
                }

                mapped = Tiered(
                    GemStat.AttackSpeedMultiplier,
                    RoleModifierOperation.Multiply,
                    PercentFactor(values, more: false));
                return true;
            }

            if (text == "Supported Skills have #% increased Area of Effect"
                || text == "Supported Skills have #% more Area of Effect"
                || text == "Supported Skills have #% more Melee Splash Area of Effect")
            {
                mapped = Tiered(
                    GemStat.AoeRadius,
                    RoleModifierOperation.Multiply,
                    PercentFactor(values, more: true));
                return true;
            }

            if (text == "Supported Skills have #% less Area of Effect")
            {
                mapped = Tiered(
                    GemStat.AoeRadius,
                    RoleModifierOperation.Multiply,
                    PercentFactor(values, more: false));
                return true;
            }

            if (text == "Supported Skills have #% increased Range")
            {
                mapped = Tiered(
                    GemStat.RangeMultiplier,
                    RoleModifierOperation.Multiply,
                    PercentFactor(values, more: true));
                return true;
            }

            if (text == "Supported Skills have #% increased Projectile Speed")
            {
                mapped = Tiered(
                    GemStat.ProjectileSpeedMultiplier,
                    RoleModifierOperation.Multiply,
                    PercentFactor(values, more: true));
                return true;
            }

            if (text == "Supported Skills have #% less Projectile Speed")
            {
                mapped = Tiered(
                    GemStat.ProjectileSpeedMultiplier,
                    RoleModifierOperation.Multiply,
                    PercentFactor(values, more: false));
                return true;
            }

            if (text == "Projectiles from Supported Skills Pierce # additional Targets")
            {
                mapped = Tiered(GemStat.PierceCount, RoleModifierOperation.Add, values);
                return true;
            }

            if (text == "Supported Skills fire # additional Projectiles")
            {
                mapped = Tiered(
                    GemStat.ProjectileCount,
                    RoleModifierOperation.Add,
                    values);
                return true;
            }

            if (text == "Supported Skills Chain # times")
            {
                mapped = Tiered(GemStat.ChainCount, RoleModifierOperation.Add, values);
                return true;
            }

            return false;
        }

        static GemStatModifier MoreDamage(TierValues values)
        {
            return Tiered(
                GemStat.Damage,
                RoleModifierOperation.Multiply,
                PercentFactor(values, more: true));
        }

        static GemStatModifier LessDamage(TierValues values)
        {
            return Tiered(
                GemStat.Damage,
                RoleModifierOperation.Multiply,
                PercentFactor(values, more: false));
        }

        static GemStatModifier MoreAilment(TierValues values)
        {
            return Tiered(
                GemStat.AilmentDamage,
                RoleModifierOperation.Multiply,
                PercentFactor(values, more: true));
        }

        static GemStatModifier LessAilment(TierValues values)
        {
            return Tiered(
                GemStat.AilmentDamage,
                RoleModifierOperation.Multiply,
                PercentFactor(values, more: false));
        }

        static bool IsConditional(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            if (text.IndexOf("Ruthless Blows", StringComparison.Ordinal) >= 0)
                return true;
            if (text.IndexOf("against", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (text.IndexOf("while ", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (text.IndexOf("when ", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (text.IndexOf(" if ", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (text.IndexOf(" per ", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (text.IndexOf("Recently", StringComparison.Ordinal) >= 0)
                return true;
            if (text.IndexOf("based on", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (text.IndexOf("for each", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (text.IndexOf("Ancestrally", StringComparison.Ordinal) >= 0)
                return true;
            if (text.IndexOf("as though", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (text.IndexOf("Critical Strike", StringComparison.Ordinal) >= 0)
                return true;
            return false;
        }

        static string ForceSkipReason(string name, string category, JArray tags)
        {
            if (name == "Ruthless Support")
                return "PoEDB: every-third-hit Ruthless Blow; skipped for later";
            if (name == "Ancestral Call Support")
                return "PoEDB: strike 2 extra nearby enemies; no extra-target melee";
            if (name == "Multistrike Support")
                return "PoEDB: repeats melee attacks; no repeat-attack combat";
            if (name == "Barrage Support")
                return "PoEDB: fire projectiles sequentially; no barrage sequence";
            if (name == "Point Blank Support")
                return "PoEDB: damage by projectile travel distance; no distance falloff";
            if (name == "Culling Strike Support")
                return "PoEDB: kill at 10% life; no execute threshold";
            if (name == "Volatility Support")
                return "PoEDB: less minimum / more maximum hit; no damage range";
            if (name == "Spell Cascade Support")
                return "PoEDB: extra area impacts in a line; no cascade targeting";
            if (name == "Overcharge Support")
                return "PoEDB: shock as though more damage; not generic more damage";
            if (name == "Critical Strike Affliction Support")
                return "PoEDB: ailment DoT multiplier on crits only; no crits";
            if (name == "Withering Touch Support")
                return "PoEDB: Wither stacks; no Wither";
            if (name == "Maim Support")
                return "PoEDB: Maim debuff; no Maim";
            if (name == "Bloodthirst Support")
                return "PoEDB: added damage from life on low life; no life pool";
            if (name == "Infused Channelling Support")
                return "PoEDB: channelling infusion; no channel";
            if (name == "Pinpoint Support")
                return "PoEDB: projectile intensity/spread; no intensity";
            if (name == "Lethal Dose Support")
                return "PoEDB: extra poison stacks on first poison; no extra stacks";
            if (name == "Coursing Current Support")
                return "PoEDB: chill-when-shocked / shock-when-chilled; no cross-apply";
            if (name == "Swift Affliction Support")
                return "PoEDB: less duration of damaging ailments; duration line was flavor-only";
            if (name == "Hallow Support")
                return null;
            if (category == "transformation")
                return "PoEDB: transformation (totem/trap/mine/trigger/minion/skill replace)";
            if (HasUnsupportedIdentityTag(tags))
                return "PoEDB tag Totem/Trap/Mine/Trigger/Minion/Warcry/Brand/Hex/Blessing/Retaliation/Vaal";
            return null;
        }

        static bool HasUnsupportedIdentityTag(JArray tags)
        {
            if (tags == null)
                return false;
            for (var i = 0; i < tags.Count; i++)
            {
                switch (tags[i]?.Value<string>())
                {
                    case "Totem":
                    case "Trap":
                    case "Mine":
                    case "Trigger":
                    case "Minion":
                    case "Warcry":
                    case "Brand":
                    case "Hex":
                    case "Blessing":
                    case "Retaliation":
                    case "Vaal":
                        return true;
                }
            }

            return false;
        }

        static void ApplyWikiCardStatics(
            string name,
            List<GemStatModifier> modifiers,
            List<string> flavor)
        {
            if (name == "Chain Support")
            {
                modifiers.Add(GemStatModifier.TieredSingle(
                    GemStat.ChainCount,
                    RoleModifierOperation.Add,
                    lesser: 1f,
                    normal: 1f,
                    greater: 1f,
                    lesserFalloff: ProjectileRuntime.LesserChainHopFalloff,
                    normalFalloff: ProjectileRuntime.DefaultChainHopFalloff,
                    greaterFalloff: ProjectileRuntime.GreaterChainHopFalloff));
                RemoveFlavorContaining(flavor, "Chain # times");
            }

            if (name == "Fork Support")
            {
                modifiers.Add(GemStatModifier.Single(
                    GemStat.ForkCount,
                    RoleModifierOperation.Add,
                    2f));
                RemoveFlavorContaining(flavor, "Skills Fork");
            }

            if (name == "Multiple Projectiles Support")
            {
                if (!HasStat(modifiers, GemStat.ProjectileCount))
                {
                    modifiers.Add(GemStatModifier.TieredSingle(
                        GemStat.ProjectileCount,
                        RoleModifierOperation.Add,
                        lesser: 2f,
                        normal: 3f,
                        greater: 4f));
                }

                if (!HasStat(modifiers, GemStat.SpreadDegrees))
                {
                    modifiers.Add(GemStatModifier.Single(
                        GemStat.SpreadDegrees,
                        RoleModifierOperation.Set,
                        ProjectileRuntime.DefaultVolleySpreadDegrees));
                }

                RemoveFlavorContaining(flavor, "additional Projectiles");
            }

            if (name == "Combustion Support")
            {
                if (!HasStat(modifiers, GemStat.Ignite))
                {
                    modifiers.Add(GemStatModifier.Single(
                        GemStat.Ignite,
                        RoleModifierOperation.Set,
                        1f));
                }

                RemoveFlavorContaining(flavor, "chance to Ignite");
            }

            if (name == "Knockback Support")
            {
                if (!HasStat(modifiers, GemStat.KnockbackDistance))
                {
                    modifiers.Add(GemStatModifier.Single(
                        GemStat.KnockbackDistance,
                        RoleModifierOperation.Set,
                        1f));
                }

                RemoveFlavorContaining(flavor, "Knockback Distance");
            }

            if (name == "Added Cold Damage Support")
            {
                if (!HasStat(modifiers, GemStat.Chill))
                {
                    modifiers.Add(GemStatModifier.Single(
                        GemStat.Chill,
                        RoleModifierOperation.Set,
                        1f));
                }
            }

            if (name == "Added Lightning Damage Support")
            {
                if (!HasStat(modifiers, GemStat.Shock))
                {
                    modifiers.Add(GemStatModifier.Single(
                        GemStat.Shock,
                        RoleModifierOperation.Set,
                        1f));
                }
            }

            if (name == "Elemental Proliferation Support")
            {
                if (!HasStat(modifiers, GemStat.Proliferate))
                {
                    modifiers.Add(GemStatModifier.Single(
                        GemStat.Proliferate,
                        RoleModifierOperation.Set,
                        1f));
                }

                RemoveFlavorContaining(flavor, "Freeze, Shock and Ignite");
            }

            if (name == "Chance to Poison Support")
            {
                modifiers.Add(GemStatModifier.Single(
                    GemStat.PoisonChance,
                    RoleModifierOperation.Set,
                    0.4f));
                RemoveFlavorContaining(flavor, "chance to Poison");
            }

            if (name == "Chance to Bleed Support"
                && FlavorContains(flavor, "chance to cause Bleeding"))
            {
                modifiers.Add(GemStatModifier.Single(
                    GemStat.BleedChance,
                    RoleModifierOperation.Set,
                    0.25f));
                RemoveFlavorContaining(flavor, "chance to cause Bleeding");
            }

            if (name == "Deadly Ailments Support")
            {
                modifiers.Add(GemStatModifier.Single(
                    GemStat.Damage,
                    RoleModifierOperation.Multiply,
                    0.2f));
                RemoveFlavorContaining(flavor, "less Damage with Hits");
            }

            if (name == "Arrow Nova Support")
            {
                modifiers.Add(GemStatModifier.Single(
                    GemStat.AimMode,
                    RoleModifierOperation.Set,
                    (float)AimMode.Ground));
                modifiers.Add(GemStatModifier.Single(
                    GemStat.DeliveryPattern,
                    RoleModifierOperation.Set,
                    (float)DeliveryPattern.PayloadNova));
                RemoveFlavorContaining(flavor, "Payload Arrow");
                RemoveFlavorContaining(flavor, "in a circle");
            }

            if (name == "Melee Splash Support")
            {
                // PoE base radius 1.4m; numbered "more Melee Splash AoE" multiplies after Set.
                modifiers.Insert(0, GemStatModifier.Single(
                    GemStat.AoeRadius,
                    RoleModifierOperation.Set,
                    1.4f));
                RemoveFlavorContaining(flavor, "Splash Damage to surrounding");
            }
        }

        static bool HasStat(List<GemStatModifier> modifiers, GemStat stat)
        {
            for (var i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].Stat == stat)
                    return true;
            }

            return false;
        }

        static bool FlavorContains(List<string> flavor, string fragment)
        {
            for (var i = 0; i < flavor.Count; i++)
            {
                if (flavor[i].IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        static void RemoveFlavorContaining(List<string> flavor, string fragment)
        {
            for (var i = flavor.Count - 1; i >= 0; i--)
            {
                if (flavor[i].IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    flavor.RemoveAt(i);
            }
        }

        static bool TryMapConversion(string text, TierValues values, out GemStatModifier mapped)
        {
            mapped = default;
            var fraction = Fraction(values);
            if (text == "Supported Skills have #% of Fire Damage Converted to Cold Damage"
                || text == "#% of Fire Damage Converted to Cold Damage")
            {
                mapped = Tiered(GemStat.ConvertFireToCold, RoleModifierOperation.Set, fraction);
                return true;
            }

            if (text == "Supported Skills have #% of Cold Damage Converted to Lightning Damage"
                || text == "#% of Cold Damage Converted to Lightning Damage")
            {
                mapped = Tiered(GemStat.ConvertColdToLightning, RoleModifierOperation.Set, fraction);
                return true;
            }

            if (text == "Supported Skills have #% of Lightning Damage Converted to Physical Damage"
                || text == "#% of Lightning Damage Converted to Physical Damage")
            {
                mapped = Tiered(
                    GemStat.ConvertLightningToPhysical,
                    RoleModifierOperation.Set,
                    fraction);
                return true;
            }

            if (text == "Supported Skills have #% of Fire Damage Converted to Chaos Damage"
                || text == "#% of Fire Damage Converted to Chaos Damage")
            {
                mapped = Tiered(GemStat.ConvertFireToChaos, RoleModifierOperation.Set, fraction);
                return true;
            }

            if (text == "Supported Skills have #% of Lightning Damage Converted to Chaos Damage"
                || text == "#% of Lightning Damage Converted to Chaos Damage")
            {
                mapped = Tiered(
                    GemStat.ConvertLightningToChaos,
                    RoleModifierOperation.Set,
                    fraction);
                return true;
            }

            if (text == "Supported Skills have #% of Cold Damage Converted to Chaos Damage"
                || text == "#% of Cold Damage Converted to Chaos Damage")
            {
                mapped = Tiered(GemStat.ConvertColdToChaos, RoleModifierOperation.Set, fraction);
                return true;
            }

            return false;
        }

        static void AddHallowBase(string name, List<GemStatModifier> modifiers)
        {
            if (name != "Hallow Support")
                return;
            modifiers.Add(GemStatModifier.Single(
                GemStat.HallowingFlame,
                RoleModifierOperation.Set,
                1f));
            modifiers.Add(GemStatModifier.Single(
                GemStat.PhysAsExtraFire,
                RoleModifierOperation.Set,
                0.25f));
        }

        static bool TryMapExceptional(string name, List<GemStatModifier> modifiers)
        {
            if (name == "Empower Support")
            {
                modifiers.Add(GemStatModifier.Single(
                    GemStat.Damage,
                    RoleModifierOperation.Multiply,
                    ExceptionalNormalMultiplier));
                return true;
            }

            if (name == "Enhance Support")
            {
                modifiers.Add(GemStatModifier.Single(
                    GemStat.AttackSpeedMultiplier,
                    RoleModifierOperation.Multiply,
                    ExceptionalNormalMultiplier));
                modifiers.Add(GemStatModifier.Single(
                    GemStat.CastSpeedMultiplier,
                    RoleModifierOperation.Multiply,
                    ExceptionalNormalMultiplier));
                return true;
            }

            if (name == "Enlighten Support")
            {
                modifiers.Add(GemStatModifier.Single(
                    GemStat.RangeMultiplier,
                    RoleModifierOperation.Multiply,
                    ExceptionalNormalMultiplier));
                return true;
            }

            return false;
        }

        static void CollectFlavorOnly(JArray mods, List<string> flavor)
        {
            if (mods == null)
                return;
            for (var i = 0; i < mods.Count; i++)
            {
                var mod = (JObject)mods[i];
                var values = mod["values"];
                if (values == null || values.Type == JTokenType.Null)
                    flavor.Add(mod["text"]?.Value<string>() ?? "");
            }
        }

        static bool TryMapChance(string text, TierValues values, out GemStatModifier mapped)
        {
            mapped = default;
            var fraction = Fraction(values);
            if (text == "Supported Skills have #% chance to Ignite"
                || text == "Supported Attacks have #% chance to Ignite")
            {
                mapped = Tiered(GemStat.IgniteChance, RoleModifierOperation.Set, fraction);
                return true;
            }

            if (text == "Supported Skills have #% chance to Shock"
                || text == "Supported Attacks have #% chance to Shock")
            {
                mapped = Tiered(GemStat.ShockChance, RoleModifierOperation.Set, fraction);
                return true;
            }

            if (text == "Supported Skills have #% chance to Freeze"
                || text == "Supported Attacks have #% chance to Freeze")
            {
                mapped = Tiered(GemStat.FreezeChance, RoleModifierOperation.Set, fraction);
                return true;
            }

            if (text == "Supported Skills have #% chance to Poison on Hit"
                || text == "Supported Attacks have #% chance to Poison on Hit")
            {
                mapped = Tiered(GemStat.PoisonChance, RoleModifierOperation.Set, fraction);
                return true;
            }

            return false;
        }

        static GemTag MapTags(JArray tags)
        {
            var result = GemTag.None;
            if (tags == null)
                return result;
            for (var i = 0; i < tags.Count; i++)
                result |= MapTag(tags[i]?.Value<string>());
            return result;
        }

        static GemTag MapTag(string poe) => GemTags.FromPoe(poe);

        public static string TrimSupportSuffix(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            const string suffix = " Support";
            if (name.Length > suffix.Length
                && name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return name.Substring(0, name.Length - suffix.Length);
            return name;
        }

        public static string SlugFromName(string name)
        {
            var trimmed = TrimSupportSuffix(name);
            if (string.IsNullOrEmpty(trimmed))
                return "gem";
            var chars = trimmed.ToCharArray();
            var written = 0;
            var pendingUnderscore = false;
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (c >= 'A' && c <= 'Z')
                    c = (char)(c + 32);
                if (c >= 'a' && c <= 'z' || c >= '0' && c <= '9')
                {
                    if (pendingUnderscore && written > 0)
                    {
                        chars[written] = '_';
                        written++;
                    }

                    pendingUnderscore = false;
                    chars[written] = c;
                    written++;
                    continue;
                }

                pendingUnderscore = true;
            }

            return written == 0 ? "gem" : new string(chars, 0, written);
        }
    }
}
