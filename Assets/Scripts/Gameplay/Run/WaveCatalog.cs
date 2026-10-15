using UnityEngine;

namespace GemTD.Gameplay.Run
{
    /// <summary>Ordered campaign waves for a run. Static authoring asset.</summary>
    [CreateAssetMenu(menuName = "Gem TD/Wave Catalog", fileName = "WaveCatalog")]
    public sealed class WaveCatalog : ScriptableObject
    {
        public WaveDefinition[] Waves;

        /// <summary>Rush, Bulwark, Ward, Full mix. Used from wave 16 on.</summary>
        public WaveDefinition[] Combos;

        public int Count => Waves != null ? Waves.Length : 0;

        public WaveDefinition[] GetWavesOrEmpty() =>
            Waves != null && Waves.Length > 0 ? Waves : System.Array.Empty<WaveDefinition>();
    }
}
