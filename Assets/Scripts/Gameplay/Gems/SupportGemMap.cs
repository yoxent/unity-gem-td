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
        public const int ExpectedGemCount = 212;
        public const float ExceptionalNormalMultiplier = 1.4f;

        public sealed class UnmappedMod
        {
            public string Text;
            public float Normal;
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
            var gems = (JArray)root["gems"];
            var results = new Result[gems.Count];
            for (var i = 0; i < gems.Count; i++)
                results[i] = FromObject((JObject)gems[i]);
            return results;
        }

        public static Result FromGemJson(string gemJson)
        {
            return FromObject(JObject.Parse(gemJson));
        }

        public static Result FromObject(JObject gem)
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
                    ClassifyMod((JObject)mods[i], modifiers, flavor, unmapped);
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
            List<GemStatModifier> modifiers,
            List<string> flavor,
            List<UnmappedMod> unmapped)
        {
            var text = mod["text"]?.Value<string>() ?? "";
            var values = mod["values"];
            if (values == null || values.Type == JTokenType.Null)
            {
                flavor.Add(text);
                return;
            }

            var normal = ReadNormal(values as JObject);
            if (TryMapNumbered(text, normal, modifiers))
                return;

            unmapped.Add(new UnmappedMod { Text = text, Normal = normal });
        }

        static bool TryMapNumbered(string text, float normal, List<GemStatModifier> modifiers)
        {
            if (TryMapHitsAndAilments(text, normal, modifiers))
                return true;

            if (text == "Supported Skills have #% increased Effect of non-Damaging Ailments on Enemies")
            {
                var factor = 1f + normal / 100f;
                modifiers.Add(GemStatModifier.Single(
                    GemStat.ChillEffect,
                    RoleModifierOperation.Multiply,
                    factor));
                modifiers.Add(GemStatModifier.Single(
                    GemStat.ShockEffect,
                    RoleModifierOperation.Multiply,
                    factor));
                return true;
            }

            if (TryMapNumberedSingle(text, normal, out var mapped))
            {
                modifiers.Add(mapped);
                return true;
            }

            return false;
        }

        static bool TryMapNumberedSingle(string text, float normal, out GemStatModifier mapped)
        {
            mapped = default;
            if (text == "Supported Skills deal #% more Damage"
                || text == "Supported Attacks deal #% more Damage")
            {
                mapped = MoreDamage(normal);
                return true;
            }

            if (text == "Supported Skills deal #% less Damage"
                || text == "Supported Attacks deal #% less Damage")
            {
                mapped = LessDamage(normal);
                return true;
            }

            if (IsConditional(text))
                return false;

            if (TryMapHitDamageLine(text, normal, out mapped))
                return true;

            if (TryMapAilmentDamageLine(text, normal, out mapped))
                return true;

            if (text == "Supported Attacks have #% chance to cause Bleeding"
                || text == "Supported Skills have #% chance to cause Bleeding")
            {
                mapped = GemStatModifier.Single(
                    GemStat.BleedChance,
                    RoleModifierOperation.Set,
                    normal / 100f);
                return true;
            }

            if (TryMapChance(text, normal, out mapped))
                return true;

            if (text == "#% increased Effect of Chill inflicted with Supported Skills")
            {
                mapped = GemStatModifier.Single(
                    GemStat.ChillEffect,
                    RoleModifierOperation.Multiply,
                    1f + normal / 100f);
                return true;
            }

            if (text == "#% increased Effect of Shock inflicted with Supported Skills")
            {
                mapped = GemStatModifier.Single(
                    GemStat.ShockEffect,
                    RoleModifierOperation.Multiply,
                    1f + normal / 100f);
                return true;
            }

            if (text == "Supported Skills gain #% of Physical Damage as Extra Fire Damage"
                || text == "Supported Attacks gain #% of Physical Damage as Extra Fire Damage")
            {
                mapped = GemStatModifier.Single(
                    GemStat.PhysAsExtraFire,
                    RoleModifierOperation.Set,
                    normal / 100f);
                return true;
            }

            if (text == "Supported Skills gain #% of Physical Damage as Extra Cold Damage"
                || text == "Supported Attacks gain #% of Physical Damage as Extra Cold Damage")
            {
                mapped = GemStatModifier.Single(
                    GemStat.PhysAsExtraCold,
                    RoleModifierOperation.Set,
                    normal / 100f);
                return true;
            }

            if (text == "Supported Skills gain #% of Physical Damage as Extra Lightning Damage"
                || text == "Supported Attacks gain #% of Physical Damage as Extra Lightning Damage")
            {
                mapped = GemStatModifier.Single(
                    GemStat.PhysAsExtraLightning,
                    RoleModifierOperation.Set,
                    normal / 100f);
                return true;
            }

            if (text == "Supported Skills gain #% of Physical Damage as Extra Chaos Damage"
                || text == "Supported Attacks gain #% of Physical Damage as Extra Chaos Damage")
            {
                mapped = GemStatModifier.Single(
                    GemStat.PhysAsExtraChaos,
                    RoleModifierOperation.Set,
                    normal / 100f);
                return true;
            }

            if (text == "#% increased magnitude of Hallowing Flame inflicted by Supported Skills")
            {
                mapped = GemStatModifier.Single(
                    GemStat.PhysAsExtraFire,
                    RoleModifierOperation.Multiply,
                    1f + normal / 100f);
                return true;
            }

            if (TryMapConversion(text, normal, out mapped))
                return true;

            if (TryMapSpeedAoeDelivery(text, normal, out mapped))
                return true;

            if (text == "Supported Skills have #% chance to Knock Enemies Back on hit")
            {
                mapped = GemStatModifier.Single(
                    GemStat.KnockbackChance,
                    RoleModifierOperation.Set,
                    normal / 100f);
                return true;
            }

            if (text.Contains("added Chaos Damage")
                || text.Contains("added Cold Damage")
                || text.Contains("added Lightning Damage")
                || text.Contains("added Fire Damage")
                || text.Contains("added Physical Damage"))
            {
                mapped = GemStatModifier.Single(GemStat.Damage, RoleModifierOperation.Add, normal);
                return true;
            }

            if (text == "#% increased Duration of Ailments inflicted with Supported Skills")
            {
                mapped = GemStatModifier.Single(
                    GemStat.AilmentDuration,
                    RoleModifierOperation.Multiply,
                    1f + normal / 100f);
                return true;
            }

            return false;
        }

        static bool TryMapHitsAndAilments(string text, float normal, List<GemStatModifier> modifiers)
        {
            if (IsConditional(text))
                return false;
            if (text.IndexOf("Hits and Ailments", StringComparison.Ordinal) < 0)
                return false;
            if (text.IndexOf("more", StringComparison.Ordinal) >= 0)
            {
                modifiers.Add(MoreDamage(normal));
                modifiers.Add(MoreAilment(normal));
                return true;
            }

            if (text.IndexOf("less", StringComparison.Ordinal) >= 0)
            {
                modifiers.Add(LessDamage(normal));
                modifiers.Add(LessAilment(normal));
                return true;
            }

            return false;
        }

        static bool TryMapHitDamageLine(string text, float normal, out GemStatModifier mapped)
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
                mapped = MoreDamage(normal);
                return true;
            }

            mapped = LessDamage(normal);
            return true;
        }

        static bool TryMapAilmentDamageLine(string text, float normal, out GemStatModifier mapped)
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
            mapped = less ? LessAilment(normal) : MoreAilment(normal);
            return true;
        }

        static bool TryMapSpeedAoeDelivery(string text, float normal, out GemStatModifier mapped)
        {
            mapped = default;
            if (text == "Supported Skills have #% increased Attack Speed"
                || text == "Supported Attacks have #% increased Attack Speed")
            {
                mapped = GemStatModifier.Single(
                    GemStat.AttackSpeedMultiplier,
                    RoleModifierOperation.Multiply,
                    1f + normal / 100f);
                return true;
            }

            if (text == "Supported Skills have #% increased Cast Speed")
            {
                mapped = GemStatModifier.Single(
                    GemStat.CastSpeedMultiplier,
                    RoleModifierOperation.Multiply,
                    1f + normal / 100f);
                return true;
            }

            if (text == "Supported Skills have #% more Cast Speed"
                || text == "Supported Skills have #% more Melee Attack Speed")
            {
                var stat = text.IndexOf("Cast", StringComparison.Ordinal) >= 0
                    ? GemStat.CastSpeedMultiplier
                    : GemStat.AttackSpeedMultiplier;
                mapped = GemStatModifier.Single(
                    stat,
                    RoleModifierOperation.Multiply,
                    1f + normal / 100f);
                return true;
            }

            if (text == "Supported Skills have #% less Attack Speed"
                || text == "Supported Skills have #% less Attack Damage")
            {
                if (text.IndexOf("Damage", StringComparison.Ordinal) >= 0)
                {
                    mapped = LessDamage(normal);
                    return true;
                }

                mapped = GemStatModifier.Single(
                    GemStat.AttackSpeedMultiplier,
                    RoleModifierOperation.Multiply,
                    1f - normal / 100f);
                return true;
            }

            if (text == "Supported Skills have #% increased Area of Effect"
                || text == "Supported Skills have #% more Area of Effect"
                || text == "Supported Skills have #% more Melee Splash Area of Effect")
            {
                mapped = GemStatModifier.Single(
                    GemStat.AoeRadius,
                    RoleModifierOperation.Multiply,
                    1f + normal / 100f);
                return true;
            }

            if (text == "Supported Skills have #% less Area of Effect")
            {
                mapped = GemStatModifier.Single(
                    GemStat.AoeRadius,
                    RoleModifierOperation.Multiply,
                    1f - normal / 100f);
                return true;
            }

            if (text == "Supported Skills have #% increased Range")
            {
                mapped = GemStatModifier.Single(
                    GemStat.RangeMultiplier,
                    RoleModifierOperation.Multiply,
                    1f + normal / 100f);
                return true;
            }

            if (text == "Supported Skills have #% increased Projectile Speed")
            {
                mapped = GemStatModifier.Single(
                    GemStat.ProjectileSpeedMultiplier,
                    RoleModifierOperation.Multiply,
                    1f + normal / 100f);
                return true;
            }

            if (text == "Supported Skills have #% less Projectile Speed")
            {
                mapped = GemStatModifier.Single(
                    GemStat.ProjectileSpeedMultiplier,
                    RoleModifierOperation.Multiply,
                    1f - normal / 100f);
                return true;
            }

            if (text == "Projectiles from Supported Skills Pierce # additional Targets")
            {
                mapped = GemStatModifier.Single(GemStat.PierceCount, RoleModifierOperation.Add, normal);
                return true;
            }

            if (text == "Supported Skills fire # additional Projectiles")
            {
                mapped = GemStatModifier.Single(
                    GemStat.ProjectileCount,
                    RoleModifierOperation.Add,
                    normal);
                return true;
            }

            if (text == "Supported Skills Chain # times")
            {
                mapped = GemStatModifier.Single(GemStat.ChainCount, RoleModifierOperation.Add, normal);
                return true;
            }

            return false;
        }

        static GemStatModifier MoreDamage(float percent)
        {
            return GemStatModifier.Single(
                GemStat.Damage,
                RoleModifierOperation.Multiply,
                1f + percent / 100f);
        }

        static GemStatModifier LessDamage(float percent)
        {
            return GemStatModifier.Single(
                GemStat.Damage,
                RoleModifierOperation.Multiply,
                1f - percent / 100f);
        }

        static GemStatModifier MoreAilment(float percent)
        {
            return GemStatModifier.Single(
                GemStat.AilmentDamage,
                RoleModifierOperation.Multiply,
                1f + percent / 100f);
        }

        static GemStatModifier LessAilment(float percent)
        {
            return GemStatModifier.Single(
                GemStat.AilmentDamage,
                RoleModifierOperation.Multiply,
                1f - percent / 100f);
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
            if (name == "Chance to Poison Support")
            {
                modifiers.Add(GemStatModifier.Single(
                    GemStat.PoisonChance,
                    RoleModifierOperation.Set,
                    0.4f));
                RemoveFlavorContaining(flavor, "chance to Poison");
            }

            if (name == "Deadly Ailments Support")
            {
                modifiers.Add(LessDamage(80f));
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

        static void RemoveFlavorContaining(List<string> flavor, string fragment)
        {
            for (var i = flavor.Count - 1; i >= 0; i--)
            {
                if (flavor[i].IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    flavor.RemoveAt(i);
            }
        }

        static bool TryMapConversion(string text, float normal, out GemStatModifier mapped)
        {
            mapped = default;
            var fraction = normal / 100f;
            if (text == "Supported Skills have #% of Fire Damage Converted to Cold Damage"
                || text == "#% of Fire Damage Converted to Cold Damage")
            {
                mapped = GemStatModifier.Single(GemStat.ConvertFireToCold, RoleModifierOperation.Set, fraction);
                return true;
            }

            if (text == "Supported Skills have #% of Cold Damage Converted to Lightning Damage"
                || text == "#% of Cold Damage Converted to Lightning Damage")
            {
                mapped = GemStatModifier.Single(GemStat.ConvertColdToLightning, RoleModifierOperation.Set, fraction);
                return true;
            }

            if (text == "Supported Skills have #% of Lightning Damage Converted to Physical Damage"
                || text == "#% of Lightning Damage Converted to Physical Damage")
            {
                mapped = GemStatModifier.Single(
                    GemStat.ConvertLightningToPhysical,
                    RoleModifierOperation.Set,
                    fraction);
                return true;
            }

            if (text == "Supported Skills have #% of Fire Damage Converted to Chaos Damage"
                || text == "#% of Fire Damage Converted to Chaos Damage")
            {
                mapped = GemStatModifier.Single(GemStat.ConvertFireToChaos, RoleModifierOperation.Set, fraction);
                return true;
            }

            if (text == "Supported Skills have #% of Lightning Damage Converted to Chaos Damage"
                || text == "#% of Lightning Damage Converted to Chaos Damage")
            {
                mapped = GemStatModifier.Single(
                    GemStat.ConvertLightningToChaos,
                    RoleModifierOperation.Set,
                    fraction);
                return true;
            }

            if (text == "Supported Skills have #% of Cold Damage Converted to Chaos Damage"
                || text == "#% of Cold Damage Converted to Chaos Damage")
            {
                mapped = GemStatModifier.Single(GemStat.ConvertColdToChaos, RoleModifierOperation.Set, fraction);
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

        static bool TryMapChance(string text, float normal, out GemStatModifier mapped)
        {
            mapped = default;
            var fraction = normal / 100f;
            if (text == "Supported Skills have #% chance to Ignite"
                || text == "Supported Attacks have #% chance to Ignite")
            {
                mapped = GemStatModifier.Single(GemStat.IgniteChance, RoleModifierOperation.Set, fraction);
                return true;
            }

            if (text == "Supported Skills have #% chance to Shock"
                || text == "Supported Attacks have #% chance to Shock")
            {
                mapped = GemStatModifier.Single(GemStat.ShockChance, RoleModifierOperation.Set, fraction);
                return true;
            }

            if (text == "Supported Skills have #% chance to Freeze"
                || text == "Supported Attacks have #% chance to Freeze")
            {
                mapped = GemStatModifier.Single(GemStat.FreezeChance, RoleModifierOperation.Set, fraction);
                return true;
            }

            if (text == "Supported Skills have #% chance to Poison on Hit"
                || text == "Supported Attacks have #% chance to Poison on Hit")
            {
                mapped = GemStatModifier.Single(GemStat.PoisonChance, RoleModifierOperation.Set, fraction);
                return true;
            }

            return false;
        }

        static float ReadNormal(JObject values)
        {
            if (values == null)
                return 0f;
            var token = values["normal"];
            if (token == null || token.Type == JTokenType.Null)
                return 0f;
            return token.Value<float>();
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

        static GemTag MapTag(string poe)
        {
            if (string.IsNullOrEmpty(poe))
                return GemTag.None;
            switch (poe)
            {
                case "Attack": return GemTag.Attack;
                case "Spell": return GemTag.Spell;
                case "Aura": return GemTag.Aura;
                case "AoE": return GemTag.Aoe;
                case "Melee": return GemTag.Melee;
                case "Projectile": return GemTag.Projectile;
                case "Slam": return GemTag.Slam;
                case "Chaining": return GemTag.Chaining;
                case "Support": return GemTag.Support;
                default: return GemTag.None;
            }
        }

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
