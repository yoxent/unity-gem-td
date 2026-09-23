using UnityEngine;

namespace GemTD.Gameplay.Map
{
    /// <summary>
    /// Authoring data for deterministic per-tile grass tint selection.
    /// Keep this component on the inactive GrassRendererTemplate object.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GrassPatchPalette : MonoBehaviour
    {
        [SerializeField] Color[] tints = { Color.white };
        [SerializeField, Min(1)] int patchSize = 1;
        [SerializeField] int seed = 173;

        public bool HasMultipleTints => tints != null && tints.Length > 1;

        public Color GetTint(Vector2Int worldCell)
        {
            if (tints == null || tints.Length == 0)
                return Color.white;

            var size = Mathf.Max(1, patchSize);
            var patchX = Mathf.FloorToInt((float)worldCell.x / size);
            var patchY = Mathf.FloorToInt((float)worldCell.y / size);
            var index = (int)(Hash(patchX, patchY, seed) % (uint)tints.Length);
            return tints[index];
        }

        static uint Hash(int x, int y, int value)
        {
            unchecked
            {
                var hash = (uint)value;
                hash ^= (uint)x * 0x9E3779B9u;
                hash = (hash << 13) | (hash >> 19);
                hash ^= (uint)y * 0x85EBCA6Bu;
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                return hash;
            }
        }
    }
}
