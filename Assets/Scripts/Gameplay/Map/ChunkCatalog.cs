using System.Collections.Generic;
using UnityEngine;

namespace GemTD.Gameplay.Map
{
    public interface IChunkCatalog
    {
        void CopyAll(List<MapChunkStamp> into);
    }

    [CreateAssetMenu(menuName = "Gem TD/Chunk Catalog")]
    public sealed class ChunkCatalog : ScriptableObject, IChunkCatalog
    {
        [SerializeField] MapChunkStamp[] straight = System.Array.Empty<MapChunkStamp>();
        [SerializeField] MapChunkStamp[] corner   = System.Array.Empty<MapChunkStamp>();
        [SerializeField] MapChunkStamp[] tjunction = System.Array.Empty<MapChunkStamp>();
        [SerializeField] MapChunkStamp[] cross    = System.Array.Empty<MapChunkStamp>();

        public IReadOnlyList<MapChunkStamp> Straight => straight;
        public IReadOnlyList<MapChunkStamp> Corner   => corner;
        public IReadOnlyList<MapChunkStamp> TJunction => tjunction;
        public IReadOnlyList<MapChunkStamp> Cross    => cross;

        public void CopyAll(List<MapChunkStamp> into)
        {
            into.Clear();
            Append(into, straight);
            Append(into, corner);
            Append(into, tjunction);
            Append(into, cross);
        }

        static void Append(List<MapChunkStamp> into, MapChunkStamp[] src)
        {
            if (src == null) return;
            for (var i = 0; i < src.Length; i++)
                if (src[i] != null) into.Add(src[i]);
        }
    }
}
