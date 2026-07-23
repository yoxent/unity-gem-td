using UnityEngine;

namespace GemTD.Gameplay.Gems
{
    [CreateAssetMenu(menuName = "Gem TD/Gem Definition", fileName = "Gem_")]
    public sealed class GemDefinition : ScriptableObject
    {
        public GemId Id = GemId.None;
        public string DisplayName = "Gem";
        [TextArea] public string Description;
    }
}
