using System;
using System.Collections.Generic;
using System.Text;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    /// <summary>Fixed-position tooltip for gems socketed in Tower Details.</summary>
    public sealed class TowerGemTooltip : MonoBehaviour
    {
        static readonly Color PositiveColor = new Color32(107, 203, 119, 255);
        static readonly Color NegativeColor = new Color32(224, 90, 90, 255);

        [SerializeField] GameObject infoPanel;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text gemNameText;
        [SerializeField] TMP_Text rarityText;
        [SerializeField] Transform tagsParent;
        [SerializeField] Transform tagSubParentTemplate;
        [SerializeField] TagLabel tagLabelPrefab;
        [SerializeField] List<TagLabel> tagLabels = new List<TagLabel>();
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] TMP_Text effectsText;

        readonly List<string> _tagNames = new List<string>(4);
        void Awake()
        {
            if (tagSubParentTemplate != null)
                tagSubParentTemplate.gameObject.SetActive(false);
            Hide();
        }

        void OnDisable()
        {
            Hide();
        }

        public void Show(GemInstance gem)
        {
            if (gem.IsEmpty)
            {
                Hide();
                return;
            }

            var definition = gem.Def;
            if (icon != null)
            {
                icon.sprite = definition.Icon;
                icon.gameObject.SetActive(definition.Icon != null);
                icon.preserveAspect = true;
            }

            SetText(gemNameText, definition.DisplayName);
            SetRarity(gem.Rarity);
            SetText(descriptionText, definition.Description);
            SetEffects(gem);
            SetTags(GemTags.EffectiveGemTags(definition));

            if (infoPanel != null)
                infoPanel.SetActive(true);
        }

        public void Hide()
        {
            if (infoPanel != null)
                infoPanel.SetActive(false);
        }

        void SetRarity(GemRarity rarity)
        {
            if (rarityText == null)
                return;

            var normalized = GemRarityUtility.Normalize(rarity);
            rarityText.text = FormatRarity(normalized);
            rarityText.color = RarityColor(normalized);
        }

        void SetTags(GemTag tags)
        {
            if (tagsParent == null || tagSubParentTemplate == null)
                return;

            tagSubParentTemplate.gameObject.SetActive(false);
            GemTags.CollectNames(tags, _tagNames);

            for (var i = 0; i < _tagNames.Count; i++)
            {
                var label = GetOrCreateTag(i);
                if (label == null)
                    continue;

                if (label.transform.parent != tagsParent)
                    label.transform.SetParent(tagsParent, false);
                label.transform.SetSiblingIndex(i);
                label.gameObject.SetActive(true);
                label.Bind(_tagNames[i]);
            }

            for (var i = _tagNames.Count; i < tagLabels.Count; i++)
            {
                if (tagLabels[i] != null)
                    tagLabels[i].gameObject.SetActive(false);
            }

            tagsParent.gameObject.SetActive(_tagNames.Count > 0);
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)tagsParent);
        }

        TagLabel GetOrCreateTag(int index)
        {
            while (tagLabels.Count <= index)
            {
                if (tagLabelPrefab == null)
                {
                    Debug.LogError("TowerGemTooltip: assign tagLabelPrefab on the prefab.", this);
                    return null;
                }

                var label = Instantiate(tagLabelPrefab, tagsParent);
                label.gameObject.SetActive(false);
                tagLabels.Add(label);
            }

            return tagLabels[index];
        }

        void SetEffects(GemInstance gem)
        {
            if (effectsText == null)
                return;

            effectsText.text = FormatEffects(gem);
            effectsText.gameObject.SetActive(!string.IsNullOrEmpty(effectsText.text));
        }

        public static string FormatEffects(GemInstance gem)
        {
            var effects = new StringBuilder(256);
            var modifiers = gem.Def != null ? gem.Def.Modifiers : null;
            if (modifiers != null)
            {
                for (var i = 0; i < modifiers.Length; i++)
                {
                    if (!TryFormatModifier(modifiers[i].Resolve(gem.Rarity), out var text, out var polarity))
                        continue;

                    if (effects.Length > 0)
                        effects.Append('\n');
                    effects.Append("• ");
                    if (polarity == EffectPolarity.Positive)
                        effects.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(PositiveColor)).Append('>');
                    else if (polarity == EffectPolarity.Negative)
                        effects.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(NegativeColor)).Append('>');
                    effects.Append(text);
                    if (polarity != EffectPolarity.Neutral)
                        effects.Append("</color>");
                }
            }

            return effects.ToString();
        }

        static bool TryFormatModifier(
            GemStatModifier modifier,
            out string text,
            out EffectPolarity polarity)
        {
            text = string.Empty;
            polarity = EffectPolarity.Neutral;
            var scalar = modifier.ValueKind == RoleStatValueKind.Range
                ? (modifier.OperandMin + modifier.OperandMax) * 0.5f
                : modifier.OperandMin;

            switch (modifier.Stat)
            {
                case GemStat.Damage:
                    text = FormatNumeric("Hit damage", modifier);
                    break;
                case GemStat.ProjectileCount:
                    text = FormatNumeric("Projectiles", modifier);
                    break;
                case GemStat.SpreadDegrees:
                    text = FormatSuffix("Spread", modifier, "°");
                    break;
                case GemStat.ChainCount:
                    text = FormatNumeric("Chain count", modifier);
                    break;
                case GemStat.ForkCount:
                    text = FormatNumeric("Fork count", modifier);
                    break;
                case GemStat.AoeRadius:
                    text = FormatNumeric("Area radius", modifier);
                    break;
                case GemStat.FireRateMultiplier:
                    text = FormatMultiplier("Action speed", modifier);
                    break;
                case GemStat.AttackSpeedMultiplier:
                    text = FormatMultiplier("Attack speed", modifier);
                    break;
                case GemStat.CastSpeedMultiplier:
                    text = FormatMultiplier("Cast speed", modifier);
                    break;
                case GemStat.RangeMultiplier:
                    text = FormatMultiplier("Range", modifier);
                    break;
                case GemStat.ProjectileSpeedMultiplier:
                    text = FormatMultiplier("Projectile speed", modifier);
                    break;
                case GemStat.EchoVolleyCount:
                    text = FormatNumeric("Extra volleys", modifier);
                    break;
                case GemStat.EchoDamageFactor:
                    text = FormatMultiplier("Echo damage", modifier);
                    break;
                case GemStat.PierceCount:
                    text = FormatNumeric("Pierce count", modifier);
                    break;
                case GemStat.Ignite:
                    text = scalar != 0f ? "Applies Burn" : string.Empty;
                    break;
                case GemStat.Chill:
                    text = scalar != 0f ? "Applies Chill" : string.Empty;
                    break;
                case GemStat.Shock:
                    text = scalar != 0f ? "Applies Shock" : string.Empty;
                    break;
                case GemStat.Proliferate:
                    text = scalar != 0f ? "Ailments spread" : string.Empty;
                    break;
                case GemStat.KnockbackChance:
                    text = FormatChance("Knockback chance", modifier);
                    break;
                case GemStat.KnockbackDistance:
                    text = FormatSuffix("Knockback distance", modifier, "m");
                    break;
                case GemStat.BleedChance:
                    text = FormatChance("Bleed chance", modifier);
                    break;
                case GemStat.BleedDamage:
                    text = FormatMultiplier("Bleed damage", modifier);
                    break;
                case GemStat.BleedDuration:
                    text = FormatDuration("Bleed duration", modifier);
                    break;
                case GemStat.IgniteChance:
                    text = FormatChance("Burn chance", modifier);
                    break;
                case GemStat.IgniteDuration:
                    text = FormatDuration("Burn duration", modifier);
                    break;
                case GemStat.ChillEffect:
                    text = FormatPercentValue("Chill effect", modifier);
                    break;
                case GemStat.ChillDuration:
                    text = FormatDuration("Chill duration", modifier);
                    break;
                case GemStat.ShockChance:
                    text = FormatChance("Shock chance", modifier);
                    break;
                case GemStat.ShockEffect:
                    text = FormatPercentValue("Shock effect", modifier);
                    break;
                case GemStat.ShockDuration:
                    text = FormatDuration("Shock duration", modifier);
                    break;
                case GemStat.FreezeChance:
                    text = FormatChance("Freeze chance", modifier);
                    break;
                case GemStat.FreezeDuration:
                    text = FormatDuration("Freeze duration", modifier);
                    break;
                case GemStat.PoisonChance:
                    text = FormatChance("Poison chance", modifier);
                    break;
                case GemStat.PoisonDuration:
                    text = FormatDuration("Poison duration", modifier);
                    break;
                case GemStat.StunChance:
                    text = FormatChance("Stun chance", modifier);
                    break;
                case GemStat.StunDuration:
                    text = FormatDuration("Stun duration", modifier);
                    break;
                case GemStat.BurningDamage:
                    text = FormatMultiplier("Burn damage", modifier);
                    break;
                case GemStat.PhysAsExtraFire:
                    text = FormatPercentValue("Physical as extra Fire", modifier);
                    break;
                case GemStat.PhysAsExtraCold:
                    text = FormatPercentValue("Physical as extra Cold", modifier);
                    break;
                case GemStat.PhysAsExtraLightning:
                    text = FormatPercentValue("Physical as extra Lightning", modifier);
                    break;
                case GemStat.PhysAsExtraChaos:
                    text = FormatPercentValue("Physical as extra Chaos", modifier);
                    break;
                case GemStat.ConvertFireToCold:
                    text = FormatPercentValue("Fire converted to Cold", modifier);
                    break;
                case GemStat.ConvertColdToLightning:
                    text = FormatPercentValue("Cold converted to Lightning", modifier);
                    break;
                case GemStat.ConvertLightningToPhysical:
                    text = FormatPercentValue("Lightning converted to Physical", modifier);
                    break;
                case GemStat.ConvertFireToChaos:
                    text = FormatPercentValue("Fire converted to Chaos", modifier);
                    break;
                case GemStat.ConvertLightningToChaos:
                    text = FormatPercentValue("Lightning converted to Chaos", modifier);
                    break;
                case GemStat.ConvertColdToChaos:
                    text = FormatPercentValue("Cold converted to Chaos", modifier);
                    break;
                case GemStat.HallowingFlame:
                    text = scalar != 0f ? "Applies Hallowing Flame" : string.Empty;
                    break;
                case GemStat.AilmentDamage:
                    text = FormatMultiplier("Ailment damage", modifier);
                    break;
                case GemStat.AilmentDuration:
                    text = FormatMultiplier("Ailment duration", modifier);
                    break;
                case GemStat.AimMode:
                    text = $"Aim: {FormatEnum<AimMode>(scalar)}";
                    break;
                case GemStat.DeliveryPattern:
                    text = $"Delivery: {FormatEnum<DeliveryPattern>(scalar)}";
                    break;
            }

            if (string.IsNullOrEmpty(text))
                return false;

            polarity = ResolvePolarity(modifier, scalar);
            return true;
        }

        static EffectPolarity ResolvePolarity(GemStatModifier modifier, float scalar)
        {
            if (modifier.Stat == GemStat.AimMode || modifier.Stat == GemStat.DeliveryPattern)
                return EffectPolarity.Neutral;
            if (modifier.Stat == GemStat.Ignite
                || modifier.Stat == GemStat.Chill
                || modifier.Stat == GemStat.Shock
                || modifier.Stat == GemStat.Proliferate
                || modifier.Stat == GemStat.HallowingFlame)
                return scalar != 0f ? EffectPolarity.Positive : EffectPolarity.Neutral;
            if (modifier.Operation == RoleModifierOperation.Multiply)
                return scalar > 1.001f
                    ? EffectPolarity.Positive
                    : scalar < 0.999f ? EffectPolarity.Negative : EffectPolarity.Neutral;
            if (scalar > 0.001f)
                return EffectPolarity.Positive;
            if (scalar < -0.001f)
                return EffectPolarity.Negative;
            return EffectPolarity.Neutral;
        }

        static string FormatNumeric(string label, GemStatModifier modifier)
        {
            return $"{label} {FormatValue(modifier, false, false)}";
        }

        static string FormatMultiplier(string label, GemStatModifier modifier)
        {
            var value = modifier.Operation == RoleModifierOperation.Multiply
                ? modifier.ValueKind == RoleStatValueKind.Range
                    ? FormatFactorRange(modifier)
                    : FormatFactor(modifier.OperandMin)
                : FormatValue(modifier, true, false);
            return $"{label} {value}";
        }

        static string FormatChance(string label, GemStatModifier modifier)
        {
            return $"{label} {FormatMultiplierValue(modifier)}";
        }

        static string FormatDuration(string label, GemStatModifier modifier)
        {
            return $"{label} {FormatDurationValue(modifier)}";
        }

        static string FormatPercentValue(string label, GemStatModifier modifier)
        {
            return $"{label} {FormatMultiplierValue(modifier)}";
        }

        static string FormatSuffix(string label, GemStatModifier modifier, string suffix)
        {
            return $"{label} {FormatValue(modifier, false, false)}{suffix}";
        }

        static string FormatValue(GemStatModifier modifier, bool percent, bool seconds)
        {
            if (modifier.ValueKind == RoleStatValueKind.Range)
                return FormatRange(modifier, percent);

            var value = modifier.OperandMin;
            var text = percent ? $"{value * 100f:0.#}%" : $"{value:0.##}";
            if (seconds)
                text += "s";
            if (modifier.Operation == RoleModifierOperation.Add && value > 0f)
                text = "+" + text;
            return text;
        }

        static string FormatRange(GemStatModifier modifier, bool percent)
        {
            var min = percent ? modifier.OperandMin * 100f : modifier.OperandMin;
            var max = percent ? modifier.OperandMax * 100f : modifier.OperandMax;
            var text = $"{min:0.#}–{max:0.#}";
            if (percent)
                text += "%";
            if (modifier.Operation == RoleModifierOperation.Add && min > 0f)
                text = "+" + text;
            return text;
        }

        static string FormatFactor(float value)
        {
            return $"×{value:0.##}";
        }

        static string FormatFactorRange(GemStatModifier modifier)
        {
            return $"×{modifier.OperandMin:0.##}–{modifier.OperandMax:0.##}";
        }

        static string FormatMultiplierValue(GemStatModifier modifier)
        {
            return modifier.Operation == RoleModifierOperation.Multiply
                ? modifier.ValueKind == RoleStatValueKind.Range
                    ? FormatFactorRange(modifier)
                    : FormatFactor(modifier.OperandMin)
                : FormatValue(modifier, true, false);
        }

        static string FormatDurationValue(GemStatModifier modifier)
        {
            if (modifier.Operation == RoleModifierOperation.Multiply)
            {
                return modifier.ValueKind == RoleStatValueKind.Range
                    ? FormatFactorRange(modifier)
                    : FormatFactor(modifier.OperandMin);
            }

            return FormatValue(modifier, false, true);
        }

        static string FormatEnum<T>(float value) where T : Enum
        {
            var ordinal = Mathf.RoundToInt(value);
            return Enum.IsDefined(typeof(T), ordinal)
                ? Enum.GetName(typeof(T), ordinal)
                : ordinal.ToString();
        }

        static string FormatRarity(GemRarity rarity)
        {
            switch (GemRarityUtility.Normalize(rarity))
            {
                case GemRarity.Lesser:
                    return "Lesser";
                case GemRarity.Greater:
                    return "Greater";
                default:
                    return "Normal";
            }
        }

        static Color RarityColor(GemRarity rarity)
        {
            switch (GemRarityUtility.Normalize(rarity))
            {
                case GemRarity.Lesser:
                    return new Color32(154, 165, 177, 255);
                case GemRarity.Greater:
                    return new Color32(199, 125, 255, 255);
                default:
                    return new Color32(91, 141, 239, 255);
            }
        }

        static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }

        enum EffectPolarity
        {
            Neutral,
            Positive,
            Negative
        }
    }
}
