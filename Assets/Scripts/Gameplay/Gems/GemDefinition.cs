using UnityEngine;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Gems
{
    [CreateAssetMenu(menuName = "Gem TD/Gem Definition", fileName = "Gem_")]
    public sealed class GemDefinition : ScriptableObject
    {
        public GemId Id = GemId.None;
        public string DisplayName = "Gem";
        [TextArea] public string Description;
        [Tooltip("Relative draft weight. Values <= 0 are treated as 1.")]
        public float DraftWeight = 1f;
        [Tooltip("PoE-style tags. Socketing: tower must have every restriction tag (Attack/Projectile/AoE/…). Support and Chaining do not gate.")]
        public GemTag Tags = GemTag.None;
        [Tooltip("Combat rows applied onto SkillSpec when this gem is socketed. Same Set / Add / Multiply order as tower roles.")]
        public GemStatModifier[] Modifiers;
        [Tooltip("Secondary payloads authored by this support gem. Rarity changes modifier scalars, not payload shape.")]
        public EffectPayloadDefinition[] EffectPayloads;
    }
}
