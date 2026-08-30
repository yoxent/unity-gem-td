using System;
using System.Collections.Generic;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using Newtonsoft.Json.Linq;

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
        public const float DefaultRadiusAttackMelee = 3.5f;
        public const float DefaultRadiusAttackSpell = 5f;
        public const float DefaultReservationPercent = 50f;
        public const float DefaultProjectileSpeed = 1f;
        public const int MoltenStrikeMagmaCount = 4;
        public const float MoltenStrikeMagmaDamageMultiplier = 0.4f;
        public const float MoltenStrikeMagmaAoeRadius = 1f;
        public const float MoltenStrikeMagmaMinDistance = 1.5f;
        public const float MoltenStrikeMagmaMaxDistance = 2.5f;
        public const float LightningArrowSplashRadius = 2f;
        public const int FirestormImpactCount = 10;
        public const float FirestormIntervalSeconds = 0.15f;
        public const float FirestormStormRadius = 2.5f;
        public const float FirestormExplosionRadius = 1.3f;
        public const float FirestormDropHeight = 3f;

        public static void ResolveFireBehavior(
            GemTag tags,
            out AimMode aim,
            out DeliveryPattern delivery)
        {
            ResolveFireBehavior(tags, slug: null, out aim, out delivery);
        }

        public static void ResolveFireBehavior(
            GemTag tags,
            string slug,
            out AimMode aim,
            out DeliveryPattern delivery)
        {
            if (string.Equals(slug, "Ice_Nova", StringComparison.OrdinalIgnoreCase))
            {
                aim = AimMode.Direct;
                delivery = DeliveryPattern.CasterNova;
                return;
            }

            if (string.Equals(slug, "Firestorm", StringComparison.OrdinalIgnoreCase))
            {
                aim = AimMode.Ground;
                delivery = DeliveryPattern.Rain;
                return;
            }

            if ((tags & GemTag.Melee) != 0 && (tags & GemTag.Strike) != 0)
            {
                aim = AimMode.Direct;
                delivery = DeliveryPattern.WarpStrike;
                return;
            }

            if ((tags & GemTag.Slam) != 0)
            {
                aim = AimMode.Ground;
                delivery = DeliveryPattern.GroundPulse;
                return;
            }

            aim = AimMode.Direct;
            delivery = DeliveryPattern.Straight;
        }

        public static void ResolveFireBehavior(
            GemTag tags,
            string slug,
            RoleKind kind,
            out AimMode aim,
            out DeliveryPattern delivery)
        {
            if (kind == RoleKind.Curse)
            {
                aim = AimMode.Direct;
                delivery = DeliveryPattern.CasterNova;
                return;
            }

            ResolveFireBehavior(tags, slug, out aim, out delivery);
        }

        public static DamageTypeShare[] ResolveProofMix(string slug)
        {
            if (string.Equals(slug, "Fireball", StringComparison.OrdinalIgnoreCase))
                return SingleShare(DamageType.Fire);
            if (string.Equals(slug, "Frostbolt", StringComparison.OrdinalIgnoreCase))
                return SingleShare(DamageType.Cold);
            if (IsArcSpell(slug))
                return SingleShare(DamageType.Lightning);
            if (string.Equals(slug, "Heavy_Strike", StringComparison.OrdinalIgnoreCase))
                return SingleShare(DamageType.Physical);
            return null;
        }

        static DamageTypeShare[] SingleShare(DamageType type)
        {
            return new[] { new DamageTypeShare { Type = type, Percent = 100 } };
        }

        public sealed class Result
        {
            public string DisplayName;
            public string Description;
            public string Slug;
            public string Category;
            public GemTag Tags;
            public float Damage;
            public int Cost;
            public int BuildIncrement;
            public int SocketCount;
            public bool AllowsHydraEvolution;
            public RoleKind[] RoleKinds;
            public RolePayload[] RolePayloads;
            public int[] SourceLevels;
            public bool IsActiveCatalogCompatible;
            public string[] UnsupportedEffectKeys;

            public RolePayload GetRolePayload(RoleKind kind)
            {
                if (RolePayloads == null)
                    return null;

                for (var i = 0; i < RolePayloads.Length; i++)
                {
                    var payload = RolePayloads[i];
                    if (payload != null && payload.Kind == kind)
                        return payload;
                }

                return null;
            }
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

        public sealed class RolePayload
        {
            public RoleKind Kind;
            public RoleStatModifier[] Modifiers;
            public RoleEffectModifier[] Effects;
            public RoleLevelDefinition[] Levels;
            public EffectPayloadDefinition[] EffectPayloads;
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
                Description = gem.Value<string>("description") ?? "",
                Slug = slug,
                Category = category,
                Tags = tags,
                AllowsHydraEvolution = false,
                BuildIncrement = BuildIncrement,
                SourceLevels = ReadSourceLevels(levels),
                IsActiveCatalogCompatible = category != "aura",
                UnsupportedEffectKeys = unsupportedEffectKeys,
            };

            ApplyCategoryDefaults(result, category, extraAura);
            result.RolePayloads = MapRolePayloads(
                levels,
                gem["radius"]?["by_level"] as JObject,
                tags,
                header,
                radiusValue,
                result);

            return result;
        }

        static void ApplyCategoryDefaults(Result result, string category, bool extraAura)
        {
            switch (category)
            {
                case "attack":
                    result.Cost = CostAttack;
                    result.Damage = DamageAttack;
                    result.SocketCount = 3;
                    result.RoleKinds = extraAura
                        ? new[] { RoleKind.Attack, RoleKind.Aura }
                        : new[] { RoleKind.Attack };
                    break;
                case "spell":
                    result.Cost = CostSpell;
                    result.Damage = DamageSpellTrapMine;
                    result.SocketCount = 3;
                    result.RoleKinds = new[] { RoleKind.Spell };
                    break;
                case "curse":
                    result.Cost = CostCurse;
                    result.Damage = DamageAuraCurse;
                    result.SocketCount = 3;
                    result.RoleKinds = new[] { RoleKind.Curse };
                    break;
                case "aura":
                    result.Cost = CostAura;
                    result.Damage = DamageAuraCurse;
                    result.SocketCount = 1;
                    result.RoleKinds = new[] { RoleKind.Aura };
                    break;
                case "trap":
                    result.Cost = CostTrap;
                    result.Damage = DamageSpellTrapMine;
                    result.SocketCount = 3;
                    result.RoleKinds = new[] { RoleKind.Trap };
                    break;
                case "mine":
                    result.Cost = CostMine;
                    result.Damage = DamageSpellTrapMine;
                    result.SocketCount = 3;
                    result.RoleKinds = new[] { RoleKind.Mine };
                    break;
                default:
                    throw new ArgumentException($"Unknown skill-gem category '{category}'.", nameof(category));
            }

        }

        static RolePayload[] MapRolePayloads(
            JObject levels,
            JObject radiusByLevel,
            GemTag tags,
            JObject header,
            float? radiusValue,
            Result result)
        {
            var payloads = new RolePayload[result.RoleKinds.Length];
            for (var i = 0; i < result.RoleKinds.Length; i++)
            {
                var kind = result.RoleKinds[i];
                payloads[i] = new RolePayload
                {
                    Kind = kind,
                    Modifiers = MapBaseModifiers(
                        kind,
                        tags,
                        header,
                        radiusValue,
                        result.Damage,
                        result.Slug),
                    Effects = Array.Empty<RoleEffectModifier>(),
                    Levels = MapLevelDefinitions(
                        levels,
                        radiusByLevel,
                        radiusValue,
                        kind,
                        result),
                    EffectPayloads = MapEffectPayloads(kind, result.Slug)
                };
            }

            return payloads;
        }

        static RoleStatModifier[] MapBaseModifiers(
            RoleKind kind,
            GemTag tags,
            JObject header,
            float? radiusValue,
            float baseDamage,
            string slug)
        {
            var modifiers = new List<RoleStatModifier>(6);
            switch (kind)
            {
                case RoleKind.Attack:
                    AddSet(
                        modifiers,
                        RoleStat.AttackTime,
                        HeaderNumber(header, "attack_time") ?? DefaultAttackTime);
                    AddSet(
                        modifiers,
                        RoleStat.AttackSpeed,
                        HeaderNumber(header, "attack_speed") ?? DefaultAttackSpeed);
                    AddSet(
                        modifiers,
                        RoleStat.TowerRadius,
                        (tags & GemTag.Melee) != 0
                            ? DefaultRadiusAttackMelee
                            : DefaultRadiusAttackSpell);
                    AddSet(modifiers, RoleStat.Damage, baseDamage);
                    break;

                case RoleKind.Spell:
                    AddSet(
                        modifiers,
                        RoleStat.CastTime,
                        HeaderNumber(header, "cast_time") ?? DefaultCastTimeSpell);
                    AddSet(modifiers, RoleStat.CastSpeed, DefaultCastSpeed);
                    AddSet(modifiers, RoleStat.TowerRadius, DefaultRadiusAttackSpell);
                    AddSet(modifiers, RoleStat.Damage, baseDamage);
                    break;

                case RoleKind.Curse:
                    AddSet(
                        modifiers,
                        RoleStat.TowerRadius,
                        CurseProofNumbers.Radius(TowerInstance.DefaultLevel));
                    break;

                case RoleKind.Aura:
                    AddSet(
                        modifiers,
                        RoleStat.TowerRadius,
                        radiusValue ?? DefaultRadiusAura);
                    AddSet(
                        modifiers,
                        RoleStat.ReservationPercent,
                        ReadReservationPercent(header) ?? DefaultReservationPercent);
                    break;

                case RoleKind.Trap:
                    AddSet(
                        modifiers,
                        RoleStat.CastTime,
                        HeaderNumber(header, "cast_time") ?? DefaultCastTimeTrap);
                    AddSet(modifiers, RoleStat.CastSpeed, DefaultCastSpeed);
                    AddSet(
                        modifiers,
                        RoleStat.TowerRadius,
                        radiusValue ?? DefaultRadiusTrapMine);
                    AddSet(modifiers, RoleStat.Damage, baseDamage);
                    break;

                case RoleKind.Mine:
                    AddSet(
                        modifiers,
                        RoleStat.CastTime,
                        HeaderNumber(header, "cast_time") ?? DefaultCastTimeMine);
                    AddSet(modifiers, RoleStat.CastSpeed, DefaultCastSpeed);
                    AddSet(
                        modifiers,
                        RoleStat.TowerRadius,
                        radiusValue ?? DefaultRadiusTrapMine);
                    AddSet(modifiers, RoleStat.Damage, baseDamage);
                    break;
            }

            ResolveFireBehavior(tags, slug, out _, out var delivery);
            var firesStraightBolt = delivery == DeliveryPattern.Straight
                && ((tags & GemTag.Projectile) != 0 || IsArcSpell(slug));
            if (firesStraightBolt)
            {
                AddSet(modifiers, RoleStat.ProjectileSpeed, DefaultProjectileSpeed);
                if (kind == RoleKind.Attack || kind == RoleKind.Spell)
                    AddSet(modifiers, RoleStat.ProjectileCount, 1);
            }
            else if ((tags & GemTag.Projectile) != 0)
            {
                AddSet(modifiers, RoleStat.ProjectileSpeed, DefaultProjectileSpeed);
            }

            if (kind == RoleKind.Attack && IsLightningArrow(slug))
                AddSet(modifiers, RoleStat.SplashRadius, LightningArrowSplashRadius);

            return modifiers.ToArray();
        }

        static EffectPayloadDefinition[] MapEffectPayloads(RoleKind kind, string slug)
        {
            if (kind == RoleKind.Attack
                && string.Equals(slug, "Molten_Strike", StringComparison.OrdinalIgnoreCase))
            {
                return new[]
                {
                    new EffectPayloadDefinition
                    {
                        Trigger = EffectPayloadTrigger.OnImpact,
                        Anchor = EffectPayloadAnchor.PrimaryTarget,
                        TravelPattern = EffectPayloadTravelPattern.Fountain,
                        ScatterPattern = EffectPayloadScatterPattern.RandomRing,
                        HitPolicy = EffectPayloadHitPolicy.PerImpact,
                        Tags = GemTag.Aoe | GemTag.Projectile,
                        Count = MoltenStrikeMagmaCount,
                        DamageMultiplier = MoltenStrikeMagmaDamageMultiplier,
                        AoeRadius = MoltenStrikeMagmaAoeRadius,
                        MinDistance = MoltenStrikeMagmaMinDistance,
                        MaxDistance = MoltenStrikeMagmaMaxDistance,
                        ArcHeight = 1.5f
                    }
                };
            }

            if (kind == RoleKind.Spell
                && string.Equals(slug, "Firestorm", StringComparison.OrdinalIgnoreCase))
            {
                return new[]
                {
                    new EffectPayloadDefinition
                    {
                        Trigger = EffectPayloadTrigger.AfterDelay,
                        Anchor = EffectPayloadAnchor.GroundTarget,
                        TravelPattern = EffectPayloadTravelPattern.FallFromSky,
                        ScatterPattern = EffectPayloadScatterPattern.None,
                        HitPolicy = EffectPayloadHitPolicy.PerImpact,
                        Tags = GemTag.Aoe,
                        Count = FirestormImpactCount,
                        DamageMultiplier = 1f,
                        AoeRadius = FirestormExplosionRadius,
                        MinDistance = 0f,
                        MaxDistance = FirestormStormRadius,
                        ArcHeight = FirestormDropHeight,
                        DelaySeconds = 0f,
                        IntervalSeconds = FirestormIntervalSeconds
                    }
                };
            }

            return Array.Empty<EffectPayloadDefinition>();
        }

        static RoleLevelDefinition[] MapLevelDefinitions(
            JObject levels,
            JObject radiusByLevel,
            float? defaultRadius,
            RoleKind kind,
            Result result)
        {
            var sourceLevels = ReadSourceLevels(levels);
            if (sourceLevels.Length == 0)
                return Array.Empty<RoleLevelDefinition>();

            var mapped = new RoleLevelDefinition[sourceLevels.Length];
            for (var i = 0; i < sourceLevels.Length; i++)
            {
                var sourceLevel = sourceLevels[i];
                var values = levels[sourceLevel.ToString()] as JObject;
                var modifiers = new List<RoleStatModifier>(3);
                var effects = new List<RoleEffectModifier>(3);

                AddLevelSplash(
                    modifiers,
                    values,
                    radiusByLevel,
                    defaultRadius,
                    kind,
                    result.Slug,
                    sourceLevel);
                AddLevelDamage(modifiers, values, kind);
                AddLevelChain(modifiers, values, result.Slug);
                AddLevelRadiusBonus(modifiers, values, result.Slug);
                AddCurseLevelRadius(modifiers, kind, sourceLevel);
                AddLevelEffects(effects, values, kind, result.Slug, sourceLevel);

                mapped[i] = new RoleLevelDefinition
                {
                    SourceLevel = sourceLevel,
                    Modifiers = modifiers.ToArray(),
                    Effects = effects.ToArray()
                };
            }

            return mapped;
        }

        static void AddLevelSplash(
            List<RoleStatModifier> modifiers,
            JObject values,
            JObject radiusByLevel,
            float? defaultRadius,
            RoleKind kind,
            string slug,
            int sourceLevel)
        {
            if (!IsClassifiedSplashSource(kind, slug))
                return;

            var absolute = FindRadiusValue(values, IsAbsoluteSplashRadiusHeader);
            var bonus = FindRadiusValue(values, IsRadiusBonusHeader);
            float? byLevel = null;
            if (radiusByLevel != null)
                byLevel = ReadRadiusValue(radiusByLevel[sourceLevel.ToString()]);

            float? splash = absolute;
            if (!splash.HasValue
                && byLevel.HasValue
                && !(bonus.HasValue && ApproxEqual(byLevel.Value, bonus.Value)))
            {
                splash = byLevel;
            }

            if (!splash.HasValue)
                splash = defaultRadius;

            if (splash.HasValue)
                AddSet(modifiers, RoleStat.SplashRadius, splash.Value);

            if (bonus.HasValue)
                AddSet(modifiers, RoleStat.SplashRadius, RoleModifierOperation.Add, bonus.Value);
        }

        static bool IsClassifiedSplashSource(RoleKind kind, string slug)
        {
            if (kind != RoleKind.Attack
                && kind != RoleKind.Spell
                && kind != RoleKind.Trap)
                return false;

            return string.Equals(slug, "Cleave", StringComparison.OrdinalIgnoreCase)
                || string.Equals(slug, "Fireball", StringComparison.OrdinalIgnoreCase)
                || string.Equals(slug, "Ice_Nova", StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    slug,
                    "Explosive_Trap",
                    StringComparison.OrdinalIgnoreCase);
        }

        static void AddLevelChain(List<RoleStatModifier> modifiers, JObject values, string slug)
        {
            if (!IsClassifiedChainSource(slug) || values == null)
                return;

            foreach (var effect in values.Properties())
            {
                if (!IsChainTimesHeader(effect.Name))
                    continue;

                var hops = ReadNumber(effect.Value);
                if (hops.HasValue)
                {
                    AddSet(modifiers, RoleStat.ChainCount, hops.Value);
                    return;
                }
            }
        }

        static bool IsClassifiedChainSource(string slug)
        {
            return IsArcSpell(slug);
        }

        static bool IsChainTimesHeader(string header)
        {
            return ContainsIgnoreCase(header, "Chains")
                && ContainsIgnoreCase(header, "Times");
        }

        static bool IsArcSpell(string slug)
        {
            return string.Equals(slug, "Arc", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsLightningArrow(string slug)
        {
            return string.Equals(slug, "Lightning_Arrow", StringComparison.OrdinalIgnoreCase);
        }

        static void AddLevelDamage(
            List<RoleStatModifier> modifiers,
            JObject values,
            RoleKind kind)
        {
            if (kind == RoleKind.Attack)
            {
                var effectiveness = ReadNumber(
                    values?["damage_percent"] ?? values?["base_damage_effectiveness"]);
                if (effectiveness.HasValue)
                {
                    AddSet(
                        modifiers,
                        RoleStat.Damage,
                        RoleModifierOperation.Multiply,
                        effectiveness.Value / 100f);
                    return;
                }

                if (TryReadFlatDamage(values, out var attackMin, out var attackMax))
                {
                    AddRange(modifiers, RoleStat.Damage, attackMin, attackMax);
                    return;
                }

                return;
            }

            if (kind != RoleKind.Spell
                && kind != RoleKind.Trap
                && kind != RoleKind.Mine)
                return;

            if (TryReadFlatDamage(values, out var min, out var max))
            {
                AddRange(modifiers, RoleStat.Damage, min, max);
                return;
            }

            var fallbackEffectiveness = ReadNumber(
                values?["damage_percent"] ?? values?["base_damage_effectiveness"]);
            if (fallbackEffectiveness.HasValue)
            {
                AddSet(
                    modifiers,
                    RoleStat.Damage,
                    RoleModifierOperation.Multiply,
                    fallbackEffectiveness.Value / 100f);
            }
        }

        static void AddLevelRadiusBonus(List<RoleStatModifier> modifiers, JObject values, string slug)
        {
            if (!IsClassifiedRadiusBonusSource(slug) || values == null)
                return;

            foreach (var effect in values.Properties())
            {
                if (!IsRadiusBonusHeader(effect.Name))
                    continue;

                var radius = ReadRadiusValue(effect.Value);
                if (radius.HasValue)
                {
                    AddSet(
                        modifiers,
                        RoleStat.TowerRadius,
                        RoleModifierOperation.Add,
                        radius.Value);
                    return;
                }
            }
        }

        static bool IsClassifiedRadiusBonusSource(string slug)
        {
            return string.Equals(slug, "Anger", StringComparison.OrdinalIgnoreCase);
        }

        static void AddCurseLevelRadius(List<RoleStatModifier> modifiers, RoleKind kind, int sourceLevel)
        {
            if (kind != RoleKind.Curse)
                return;

            AddSet(modifiers, RoleStat.TowerRadius, CurseProofNumbers.Radius(sourceLevel));
        }

        static void AddLevelEffects(
            List<RoleEffectModifier> effects,
            JObject values,
            RoleKind kind,
            string slug,
            int sourceLevel)
        {
            if (kind == RoleKind.Curse)
            {
                AddCurseLevelEffects(effects, values, slug, sourceLevel);
                return;
            }

            if (kind == RoleKind.Aura)
            {
                AddAuraLevelEffects(effects, values);
                return;
            }

            AddSharedNonAuraLevelEffects(effects, values);
        }

        static void AddCurseLevelEffects(
            List<RoleEffectModifier> effects,
            JObject values,
            string slug,
            int sourceLevel)
        {
            if (CurseProofNumbers.IsResistSlug(slug))
            {
                AddEffectSet(
                    effects,
                    CurseProofNumbers.ResistKind(slug),
                    CurseProofNumbers.Resist(sourceLevel));
                return;
            }

            if (CurseProofNumbers.IsElementalWeakness(slug))
            {
                var resist = CurseProofNumbers.Resist(sourceLevel);
                if (values != null)
                {
                    foreach (var effect in values.Properties())
                    {
                        if (!IsElementalResistanceHeader(effect.Name))
                            continue;
                        var poe = ReadNumber(effect.Value) ?? ReadNumber(effect.Value?["value"]);
                        if (!poe.HasValue)
                            continue;
                        resist = CurseProofNumbers.ScalePoE(poe.Value, sourceLevel);
                        break;
                    }
                }

                AddEffectSet(effects, RoleEffectKind.EnemyFireResistance, resist);
                AddEffectSet(effects, RoleEffectKind.EnemyColdResistance, resist);
                AddEffectSet(effects, RoleEffectKind.EnemyLightningResistance, resist);
                return;
            }

            if (CurseProofNumbers.IsVulnerability(slug))
            {
                AddEffectSet(
                    effects,
                    RoleEffectKind.EnemyPhysicalDamageTakenIncreased,
                    CurseProofNumbers.Vulnerability(sourceLevel));
                return;
            }

            if (CurseProofNumbers.IsTemporalChains(slug))
            {
                if (values == null)
                    return;

                foreach (var effect in values.Properties())
                {
                    var amount = ReadNumber(effect.Value) ?? ReadNumber(effect.Value?["value"]);
                    if (!amount.HasValue)
                        continue;

                    if (IsTemporalChainsNormalHeader(effect.Name))
                        AddEffectSet(effects, RoleEffectKind.EnemyActionSpeedLessNormal, amount.Value);
                    else if (IsTemporalChainsRareHeader(effect.Name))
                        AddEffectSet(effects, RoleEffectKind.EnemyActionSpeedLessRare, amount.Value);
                }

                return;
            }

            AddCatalogRetuneCurseEffects(effects, values, sourceLevel);
        }

        static void AddCatalogRetuneCurseEffects(
            List<RoleEffectModifier> effects,
            JObject values,
            int sourceLevel)
        {
            if (values == null)
                return;

            foreach (var effect in values.Properties())
            {
                if (IsSkippedCurseCatalogHeader(effect.Name))
                    continue;

                if (IsAddedPhysicalToHitsHeader(effect.Name)
                    && TryReadRange(effect.Value, out var addedMin, out var addedMax))
                {
                    AddEffectRange(
                        effects,
                        RoleEffectKind.EnemyAddedPhysicalDamage,
                        CurseProofNumbers.ScalePoE(addedMin, sourceLevel),
                        CurseProofNumbers.ScalePoE(addedMax, sourceLevel));
                    continue;
                }

                var amount = ReadNumber(effect.Value) ?? ReadNumber(effect.Value?["value"]);
                if (!amount.HasValue)
                    continue;

                var kind = MapCatalogRetuneEffectKind(effect.Name);
                if (!kind.HasValue)
                    continue;

                AddEffectSet(effects, kind.Value, CurseProofNumbers.ScalePoE(amount.Value, sourceLevel));
            }
        }

        static bool IsSkippedCurseCatalogHeader(string header)
        {
            if (string.IsNullOrEmpty(header))
                return true;

            return string.Equals(header, "damage_percent", StringComparison.OrdinalIgnoreCase)
                || ContainsIgnoreCase(header, "Base duration is")
                || ContainsIgnoreCase(header, "metres to radius")
                || ContainsIgnoreCase(header, "Base radius is")
                || IsFlatDamageHeader(header)
                || ContainsIgnoreCase(header, "Damage per second")
                || ContainsIgnoreCase(header, "more Damage per Curse")
                || ContainsIgnoreCase(header, "Only applies Hexes")
                || ContainsIgnoreCase(header, "can only Support")
                || ContainsIgnoreCase(header, "Burning Ground")
                || ContainsIgnoreCase(header, "Caustic Ground");
        }

        static bool IsAddedPhysicalToHitsHeader(string header)
        {
            return ContainsIgnoreCase(header, "Adds")
                && ContainsIgnoreCase(header, "Physical Damage")
                && ContainsIgnoreCase(header, "Hits against Cursed");
        }

        static RoleEffectKind? MapCatalogRetuneEffectKind(string header)
        {
            if (ContainsIgnoreCase(header, "increased Damage while on Low Life"))
                return RoleEffectKind.EnemyDamageTakenIncreasedLowLife;
            if (ContainsIgnoreCase(header, "Damage from Projectile Hits"))
                return RoleEffectKind.EnemyProjectileDamageTakenIncreased;
            if (ContainsIgnoreCase(header, "Life Leech when Hit by Attacks"))
                return RoleEffectKind.EnemyLifeLeechOnAttackHit;
            if (ContainsIgnoreCase(header, "double Stun Duration"))
                return RoleEffectKind.EnemyDoubleStunDurationChance;
            if (ContainsIgnoreCase(header, "Life when Hit by Attacks"))
                return RoleEffectKind.EnemyLifeWhenHitByAttacks;
            if (ContainsIgnoreCase(header, "Life when Killed"))
                return RoleEffectKind.EnemyLifeWhenKilled;
            if (ContainsIgnoreCase(header, "Critical Strike Multiplier"))
                return RoleEffectKind.EnemyCriticalStrikeMultiplier;
            if (ContainsIgnoreCase(header, "Normal or Magic")
                && ContainsIgnoreCase(header, "less Damage"))
                return RoleEffectKind.EnemyOutgoingDamageLessNormal;
            if (ContainsIgnoreCase(header, "Rare or Unique")
                && ContainsIgnoreCase(header, "less Damage"))
                return RoleEffectKind.EnemyOutgoingDamageLessRare;
            if (ContainsIgnoreCase(header, "reduced Accuracy Rating"))
                return RoleEffectKind.EnemyAccuracyRatingReduced;
            return null;
        }

        static bool IsElementalResistanceHeader(string header)
        {
            return ContainsIgnoreCase(header, "Elemental Resistances");
        }

        static bool IsTemporalChainsNormalHeader(string header)
        {
            return ContainsIgnoreCase(header, "Normal and Magic")
                && ContainsIgnoreCase(header, "Action Speed");
        }

        static bool IsTemporalChainsRareHeader(string header)
        {
            return ContainsIgnoreCase(header, "Rare and Unique")
                && ContainsIgnoreCase(header, "Action Speed");
        }

        static void AddAuraLevelEffects(List<RoleEffectModifier> effects, JObject values)
        {
            if (values == null)
                return;

            foreach (var effect in values.Properties())
            {
                if (IsAddedAttackFireHeader(effect.Name)
                    && TryReadRange(effect.Value, out var attackMin, out var attackMax))
                {
                    AddEffectRange(
                        effects,
                        RoleEffectKind.AllyAddedAttackFireDamage,
                        attackMin,
                        attackMax);
                    continue;
                }

                if (IsAddedSpellFireHeader(effect.Name)
                    && TryReadRange(effect.Value, out var spellMin, out var spellMax))
                {
                    AddEffectRange(
                        effects,
                        RoleEffectKind.AllyAddedSpellFireDamage,
                        spellMin,
                        spellMax);
                }
            }
        }

        static void AddSharedNonAuraLevelEffects(List<RoleEffectModifier> effects, JObject values)
        {
            if (values == null)
                return;

            foreach (var effect in values.Properties())
            {
                if (IsDurationHeader(effect.Name))
                {
                    var duration = ReadNumber(effect.Value)
                        ?? ReadNumber(effect.Value?["value"]);
                    if (duration.HasValue)
                    {
                        AddEffectSet(effects, RoleEffectKind.SkillDuration, duration.Value);
                    }

                    continue;
                }

                if (IsColdResistanceHeader(effect.Name))
                {
                    var resistance = ReadNumber(effect.Value)
                        ?? ReadNumber(effect.Value?["value"]);
                    if (resistance.HasValue)
                    {
                        AddEffectSet(
                            effects,
                            RoleEffectKind.EnemyColdResistance,
                            resistance.Value);
                    }
                }
            }
        }

        static bool IsMappedEffectHeader(string header)
        {
            return IsAddedAttackFireHeader(header)
                || IsAddedSpellFireHeader(header);
        }

        static bool IsAddedAttackFireHeader(string header)
        {
            return ContainsIgnoreCase(header, "additional Fire Damage with Attacks");
        }

        static bool IsAddedSpellFireHeader(string header)
        {
            return ContainsIgnoreCase(header, "additional Fire Damage with Spells");
        }

        static bool IsDurationHeader(string header)
        {
            return ContainsIgnoreCase(header, "Base duration is");
        }

        static bool IsColdResistanceHeader(string header)
        {
            return ContainsIgnoreCase(header, "Cold Resistance");
        }

        static bool ContainsIgnoreCase(string header, string needle)
        {
            return !string.IsNullOrEmpty(header)
                && header.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static void AddEffectSet(
            List<RoleEffectModifier> effects,
            RoleEffectKind kind,
            float value)
        {
            effects.Add(RoleEffectModifier.Single(kind, RoleModifierOperation.Set, value));
        }

        static void AddEffectRange(
            List<RoleEffectModifier> effects,
            RoleEffectKind kind,
            float min,
            float max)
        {
            effects.Add(RoleEffectModifier.Range(kind, RoleModifierOperation.Set, min, max));
        }

        static bool TryReadFlatDamage(JObject values, out float min, out float max)
        {
            min = 0f;
            max = 0f;
            if (values == null)
                return false;

            foreach (var effect in values.Properties())
            {
                if (!IsFlatDamageHeader(effect.Name))
                    continue;
                var kind = effect.Value?["kind"]?.Value<string>();
                if (!string.Equals(kind, "flat", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!TryReadRange(effect.Value, out min, out max))
                    continue;
                return true;
            }

            return false;
        }

        static bool IsFlatDamageHeader(string header)
        {
            if (string.IsNullOrEmpty(header))
                return false;

            return header.StartsWith("Deals ", StringComparison.OrdinalIgnoreCase)
                && header.IndexOf("Damage", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static float? FindRadiusValue(JObject values)
        {
            return FindRadiusValue(values, IsRadiusHeader);
        }

        static float? FindRadiusValue(JObject values, Func<string, bool> headerMatch)
        {
            if (values == null || headerMatch == null)
                return null;

            foreach (var effect in values.Properties())
            {
                if (!headerMatch(effect.Name))
                    continue;
                var value = ReadRadiusValue(effect.Value);
                if (value.HasValue)
                    return value;
            }

            return null;
        }

        static float? ReadRadiusValue(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;

            var value = token;
            if (token.Type == JTokenType.Object)
            {
                var wrapped = token["value"];
                if (wrapped != null)
                    value = wrapped;
            }

            if (value.Type == JTokenType.Array)
            {
                var array = (JArray)value;
                if (array.Count == 0)
                    return null;

                return ReadNumber(array[array.Count - 1]);
            }

            return ReadNumber(value);
        }

        static bool IsRadiusHeader(string header)
        {
            if (string.IsNullOrEmpty(header))
                return false;
            return header.IndexOf("radius", StringComparison.OrdinalIgnoreCase) >= 0
                || header.IndexOf("metre", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool IsAbsoluteSplashRadiusHeader(string header)
        {
            if (string.IsNullOrEmpty(header))
                return false;
            return ContainsIgnoreCase(header, "Base radius")
                || ContainsIgnoreCase(header, "Base explosion radius");
        }

        static bool IsRadiusBonusHeader(string header)
        {
            return ContainsIgnoreCase(header, "metres to radius")
                || ContainsIgnoreCase(header, "metre to radius");
        }

        static bool ApproxEqual(float a, float b)
        {
            return Math.Abs(a - b) <= 0.0001f;
        }

        static bool TryReadRange(JToken token, out float min, out float max)
        {
            min = 0f;
            max = 0f;
            if (token == null || token.Type == JTokenType.Null)
                return false;

            var value = token;
            if (token.Type == JTokenType.Object)
            {
                var wrapped = token["value"];
                if (wrapped != null)
                    value = wrapped;
            }

            if (value.Type == JTokenType.Array)
            {
                var array = (JArray)value;
                if (array.Count == 0)
                    return false;
                var first = ReadNumber(array[0]);
                var last = ReadNumber(array[array.Count - 1]);
                if (!first.HasValue || !last.HasValue)
                    return false;
                min = first.Value;
                max = last.Value;
            }
            else
            {
                var number = ReadNumber(value);
                if (!number.HasValue)
                    return false;
                min = number.Value;
                max = number.Value;
            }

            if (max < min)
            {
                var swap = min;
                min = max;
                max = swap;
            }

            return true;
        }

        static void AddSet(List<RoleStatModifier> modifiers, RoleStat stat, float value)
        {
            AddSet(modifiers, stat, RoleModifierOperation.Set, value);
        }

        static void AddSet(
            List<RoleStatModifier> modifiers,
            RoleStat stat,
            RoleModifierOperation operation,
            float value)
        {
            modifiers.Add(RoleStatModifier.Single(stat, operation, value));
        }

        static void AddRange(
            List<RoleStatModifier> modifiers,
            RoleStat stat,
            float min,
            float max)
        {
            modifiers.Add(RoleStatModifier.Range(stat, RoleModifierOperation.Set, min, max));
        }

        static float? ReadReservationPercent(JObject header)
        {
            if (header == null)
                return null;

            return ReadNumber(header["reservation"]?["value"]?["amount"]);
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

        static GemTag MapTag(string poe) => GemTags.FromPoe(poe);

        static float? HeaderNumber(JObject header, string key)
        {
            if (header == null)
                return null;

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

            sourceLevels.Sort();
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
                    if (effect.Name == "base_damage_effectiveness"
                        || effect.Name == "damage_percent"
                        || IsRadiusHeader(effect.Name)
                        || IsMappedEffectHeader(effect.Name))
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
