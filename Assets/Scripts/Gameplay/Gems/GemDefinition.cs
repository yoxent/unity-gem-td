using UnityEngine;

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
        [Tooltip("PoE-style tags from poe1_inspired_tower_support_gems.json. Socketing uses RestrictionMask (Attack/Projectile/AoE/…).")]
        public GemTag Tags = GemTag.None;
        [Tooltip("Optional override. None = gem Tags ∩ RestrictionMask.")]
        public GemTag RequiredTags = GemTag.None;
    }
}
