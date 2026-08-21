using UnityEngine;

namespace GemTD.Gameplay.Run
{
    /// <summary>Ordered campaign waves for a run. Static authoring asset.</summary>
    [CreateAssetMenu(menuName = "Gem TD/Wave Catalog", fileName = "WaveCatalog")]
    public sealed class WaveCatalog : ScriptableObject
    {
        public WaveDefinition[] Waves;

        public int Count => Waves != null ? Waves.Length : 0;

        public WaveDefinition[] GetWavesOrEmpty() =>
            Waves != null && Waves.Length > 0 ? Waves : System.Array.Empty<WaveDefinition>();
    }
}
