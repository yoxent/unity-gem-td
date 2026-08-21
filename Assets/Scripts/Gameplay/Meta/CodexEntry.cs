using UnityEngine;
using GemTD.Gameplay.Gems;

namespace GemTD.Gameplay.Meta
{
    /// <summary>Authoring data for one Alchemical Codex entry. Static asset; unlock state lives in <see cref="CodexProgress"/>.</summary>
    [CreateAssetMenu(menuName = "Gem TD/Codex Entry", fileName = "Codex_")]
    public sealed class CodexEntry : ScriptableObject
    {
        /// <summary>Stable save key (e.g. "hydra-ballista"). Must be unique within a catalog.</summary>
        [Tooltip("Stable save key. Must be unique within a catalog.")]
        public string Id;

        /// <summary>Shown only when unlocked. Keep empty/??? while designing a mystery entry.</summary>
        public string DisplayName;

        /// <summary>Optional icon shown when unlocked. Null -> ??? chip.</summary>
        public Sprite Icon;

        [TextArea] public string LockedHint;

        [TextArea] public string UnlockedText;

        /// <summary>Recipe gems. Declared now, read later (Approach B generalizes detection).</summary>
        public GemDefinition[] Recipe;
    }
}