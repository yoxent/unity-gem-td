using UnityEngine;
using UnityEngine.Serialization;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Gems
{
    [CreateAssetMenu(menuName = "Gem TD/Gem Definition", fileName = "Gem_")]
    public sealed class GemDefinition : ScriptableObject
    {
        public GemId Id = GemId.None;
        public string DisplayName = "Gem";
        [TextArea] public string Description;

        [Header("Draft")]
        [Tooltip("Relative chance this family is offered (vs other families). Does not pick Lesser/Normal/Greater. Values <= 0 are treated as 1.")]
        public float DraftWeight = 1f;

        [Header("Rarity override (optional)")]
        [Header("Leave all 0 = use GemRarityTable (default 60/30/10)")]
        [Tooltip("OVERRIDE for this family only. Leave all three at 0 to use GemRarityTable.")]
        [Min(0f)] public float LesserRarityWeight;
        [Tooltip("OVERRIDE — leave 0 with the other two to use GemRarityTable")]
        [Min(0f)] public float NormalRarityWeight;
        [Tooltip("OVERRIDE — leave 0 with the other two to use GemRarityTable")]
        [Min(0f)] public float GreaterRarityWeight;

        [Header("Combat")]
        [Tooltip("PoE-style tags. Socketing: tower must have every restriction tag (Attack/Projectile/AoE/…). Support and Chaining do not gate.")]
        [SerializeField, FormerlySerializedAs("Tags"), GemTagMask]
        long tags;

        public GemTag Tags
        {
            get => (GemTag)tags;
            set => tags = (long)value;
        }
        [Tooltip("Combat rows applied onto SkillSpec when this gem is socketed. Same Set / Add / Multiply order as tower roles.")]
        public GemStatModifier[] Modifiers;
        [Tooltip("Secondary payloads authored by this support gem. Rarity changes modifier scalars, not payload shape.")]
        public EffectPayloadDefinition[] EffectPayloads;

        public bool HasCustomRarityWeights =>
            LesserRarityWeight > 0f || NormalRarityWeight > 0f || GreaterRarityWeight > 0f;
    }
}
