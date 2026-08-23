using System;
using System.Collections.Generic;
using GemTD.Gameplay.Gems;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Maps one crawled PoE skill-gem JSON object to tower/role field values.
    /// EditMode-testable. Does not create assets or touch the Editor.
    /// </summary>
    public static class SkillGemTowerMap
    {
        public const int ExpectedGemCount = 222;

        public const int CostAttack = 20;
        public const int CostSpell = 25;
        public const int CostCurse = 30;
        public const int CostAura = 30;
        public const int CostTrap = 25;
        public const int CostMine = 25;
        public const int BuildIncrement = 15;

        public const float DamageAttack = 10f;
        public const float DamageSpellTrapMine = 8f;
        public const float DamageAuraCurse = 0f;

        public const float DefaultAttackTime = 1f;
        public const float DefaultAttackSpeed = 100f;
        public const float DefaultCastSpeed = 100f;
        public const float DefaultCastTimeSpell = 0.75f;
        public const float DefaultCastTimeCurse = 0.5f;
        public const float DefaultCastTimeTrap = 1f;
        public const float DefaultCastTimeMine = 0.75f;

        public const float DefaultRadiusAura = 1.5f;
        public const float DefaultRadiusCurse = 4.5f;
        public const float DefaultRadiusTrapMine = 3.5f;
        public const float DefaultReservationPercent = 50f;
        public const float MinPlaceRange = 3.5f;
        public const float DefaultRangeAttackSpell = 5f;

        public sealed class Result
        {
            public string DisplayName;
            public string Slug;
            public string Category;
            public GemTag Tags;
            public float TowerRadius;
            public float AuraTowerRadius;
            public float Damage;
            public int Cost;
            public int BuildIncrement;
            public int SocketCount;
            public bool AllowsHydraEvolution;
            public float AttackTime;
            public float AttackSpeed;
            public float CastTime;
            public float CastSpeed;
            public float ReservationPercent;
            public RoleKind[] RoleKinds;
            public int[] SourceLevels;
            public bool IsActiveCatalogCompatible;
            public string[] UnsupportedEffectKeys;
        }

        public enum RoleKind
        {
            Attack,
            Spell,
            Curse,
            Aura,
            Trap,
            Mine,
        }

        public static Result FromJson(string gemJson)
        {
            if (string.IsNullOrWhiteSpace(gemJson))
                throw new ArgumentException("Gem JSON is empty.", nameof(gemJson));
            return FromObject(JObject.Parse(gemJson));
        }

        public static Result[] FromCatalogJson(string fileJson)
        {
            if (string.IsNullOrWhiteSpace(fileJson))
                return Array.Empty<Result>();
            var root = JObject.Parse(fileJson);
            var gems = root["gems"] as JArray;
            if (gems == null || gems.Count == 0)
                return Array.Empty<Result>();
            var results = new Result[gems.Count];
            for (var i = 0; i < gems.Count; i++)
                results[i] = FromObject((JObject)gems[i]);
            return results;
        }

        public static Result FromObject(JObject gem)
        {
            if (gem == null)
                throw new ArgumentNullException(nameof(gem));

            var name = gem.Value<string>("name") ?? "Unnamed";
            var slug = gem.Value<string>("slug");
            if (string.IsNullOrWhiteSpace(slug))
                slug = Slugify(name);

            var category = (gem.Value<string>("category") ?? "").Trim().ToLowerInvariant();
            var tagsToken = gem["tags"] as JArray;
            var header = gem["header"] as JObject;
            var radiusValue = ReadNumber(gem["radius"]?["value"]);
            var levels = gem["levels"] as JObject;
            var unsupportedEffectKeys = ReadUnsupportedAuraEffectKeys(category, levels);

            var tags = MapTags(tagsToken, category);
            var extraAura = category == "attack" && (tags & GemTag.Aura) != 0;
            var result = new Result
            {
                DisplayName = name,
                Slug = slug,
                Category = category,
                Tags = tags,
                AllowsHydraEvolution = false,
                BuildIncrement = BuildIncrement,
                AttackSpeed = DefaultAttackSpeed,
                CastSpeed = DefaultCastSpeed,
                ReservationPercent = DefaultReservationPercent,
                SourceLevels = ReadSourceLevels(levels),
                IsActiveCatalogCompatible = category != "aura",
                UnsupportedEffectKeys = unsupportedEffectKeys,
            };

            ApplyCategoryDefaults(result, category, radiusValue, extraAura);
            ApplyHeader(result, header);

            if (radiusValue.HasValue && (category == "attack" || category == "spell"))
                result.TowerRadius = Mathf.Max(radiusValue.Value, MinPlaceRange);

            return result;
        }

        static void ApplyCategoryDefaults(Result result, string category, float? radiusValue, bool extraAura)
        {
            switch (category)
            {
                case "attack":
                    result.Cost = CostAttack;
                    result.Damage = DamageAttack;
                    result.SocketCount = 3;
                    result.TowerRadius = DefaultRangeAttackSpell;
                    result.AttackTime = DefaultAttackTime;
                    result.AttackSpeed = DefaultAttackSpeed;
                    result.RoleKinds = extraAura
                        ? new[] { RoleKind.Attack, RoleKind.Aura }
                        : new[] { RoleKind.Attack };
                    if (extraAura)
                    {
                        result.AuraTowerRadius = radiusValue ?? DefaultRadiusAura;
                        result.ReservationPercent = DefaultReservationPercent;
                    }
                    break;
                case "spell":
                    result.Cost = CostSpell;
                    result.Damage = DamageSpellTrapMine;
                    result.SocketCount = 3;
                    result.TowerRadius = DefaultRangeAttackSpell;
                    result.CastTime = DefaultCastTimeSpell;
                    result.RoleKinds = new[] { RoleKind.Spell };
                    break;
                case "curse":
                    result.Cost = CostCurse;
                    result.Damage = DamageAuraCurse;
                    result.SocketCount = 3;
                    result.CastTime = DefaultCastTimeCurse;
                    result.TowerRadius = radiusValue ?? DefaultRadiusCurse;
                    result.RoleKinds = new[] { RoleKind.Curse };
                    break;
                case "aura":
                    result.Cost = CostAura;
                    result.Damage = DamageAuraCurse;
                    result.SocketCount = 1;
                    result.TowerRadius = radiusValue ?? DefaultRadiusAura;
                    result.ReservationPercent = DefaultReservationPercent;
                    result.RoleKinds = new[] { RoleKind.Aura };
                    break;
                case "trap":
                    result.Cost = CostTrap;
                    result.Damage = DamageSpellTrapMine;
                    result.SocketCount = 3;
                    result.CastTime = DefaultCastTimeTrap;
                    result.TowerRadius = radiusValue ?? DefaultRadiusTrapMine;
                    result.RoleKinds = new[] { RoleKind.Trap };
                    break;
                case "mine":
                    result.Cost = CostMine;
                    result.Damage = DamageSpellTrapMine;
                    result.SocketCount = 3;
                    result.CastTime = DefaultCastTimeMine;
                    result.TowerRadius = radiusValue ?? DefaultRadiusTrapMine;
                    result.RoleKinds = new[] { RoleKind.Mine };
                    break;
                default:
                    throw new ArgumentException($"Unknown skill-gem category '{category}'.", nameof(category));
            }
        }

        static void ApplyHeader(Result result, JObject header)
        {
            if (header == null)
                return;

            var attackTime = HeaderNumber(header, "attack_time");
            if (attackTime.HasValue)
                result.AttackTime = attackTime.Value;

            var attackSpeed = HeaderNumber(header, "attack_speed");
            if (attackSpeed.HasValue)
                result.AttackSpeed = attackSpeed.Value;

            var castTime = HeaderNumber(header, "cast_time");
            if (castTime.HasValue)
                result.CastTime = castTime.Value;

            var reservation = ReadNumber(header["reservation"]?["value"]?["amount"]);
            if (reservation.HasValue)
                result.ReservationPercent = reservation.Value;
        }

        static GemTag MapTags(JArray tagsToken, string category)
        {
            var tags = GemTag.None;
            if (tagsToken != null)
            {
                for (var i = 0; i < tagsToken.Count; i++)
                    tags |= MapTag(tagsToken[i]?.Value<string>());
            }

            switch (category)
            {
                case "attack":
                    tags |= GemTag.Attack;
                    break;
                case "spell":
                    tags |= GemTag.Spell;
                    break;
                case "aura":
                    tags |= GemTag.Aura;
                    break;
            }

            return tags;
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
                default: return GemTag.None;
            }
        }

        static float? HeaderNumber(JObject header, string key)
        {
            var node = header[key];
            if (node == null || node.Type == JTokenType.Null)
                return null;
            return ReadNumber(node["value"]) ?? ReadNumber(node);
        }

        static int[] ReadSourceLevels(JObject levels)
        {
            if (levels == null)
                return Array.Empty<int>();

            var sourceLevels = new List<int>();
            foreach (var level in levels.Properties())
            {
                if (int.TryParse(level.Name, out var sourceLevel))
                    sourceLevels.Add(sourceLevel);
            }

            return sourceLevels.ToArray();
        }

        static string[] ReadUnsupportedAuraEffectKeys(string category, JObject levels)
        {
            if (category != "aura" || levels == null)
                return Array.Empty<string>();

            var keys = new List<string>();
            foreach (var level in levels.Properties())
            {
                var values = level.Value as JObject;
                if (values == null)
                    continue;

                foreach (var effect in values.Properties())
                {
                    if (effect.Name == "base_damage_effectiveness")
                        continue;

                    var alreadyAdded = false;
                    for (var i = 0; i < keys.Count; i++)
                    {
                        if (keys[i] == effect.Name)
                        {
                            alreadyAdded = true;
                            break;
                        }
                    }

                    if (!alreadyAdded)
                        keys.Add(effect.Name);
                }
            }

            return keys.ToArray();
        }

        static float? ReadNumber(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;
            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                return token.Value<float>();
            if (token.Type == JTokenType.Object)
            {
                var min = token["min"];
                if (min != null && min.Type != JTokenType.Null)
                    return min.Value<float>();
                var value = token["value"];
                if (value != null)
                    return ReadNumber(value);
                var amount = token["amount"];
                if (amount != null)
                    return ReadNumber(amount);
            }

            return null;
        }

        static string Slugify(string name)
        {
            var chars = name.Trim().ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                chars[i] = char.IsLetterOrDigit(c) ? c : '_';
            }

            return new string(chars);
        }
    }
}
