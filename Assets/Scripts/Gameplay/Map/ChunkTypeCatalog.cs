using System.Collections.Generic;
using UnityEngine;

namespace GemTD.Gameplay.Map
{
    [CreateAssetMenu(menuName = "Gem TD/Chunk Type Catalog")]
    public sealed class ChunkTypeCatalog : ScriptableObject
    {
        static readonly List<MapChunkStamp> NoStamps = new List<MapChunkStamp>();

        [SerializeField] ChunkType type = ChunkType.Corner;
        [SerializeField] List<MapChunkStamp> stamps = new List<MapChunkStamp>();

        public ChunkType Type => type;
        public IReadOnlyList<MapChunkStamp> Stamps => stamps ?? NoStamps;

        void OnEnable()
        {
            if (stamps == null) stamps = new List<MapChunkStamp>();
        }

        public void Configure(ChunkType chunkType, MapChunkStamp[] list)
        {
            type = chunkType;
            stamps = new List<MapChunkStamp>();
            if (list == null) return;
            for (var i = 0; i < list.Length; i++)
                stamps.Add(list[i]);
        }

        public bool TryAddStamp(MapChunkStamp stamp)
        {
            if (stamp == null) return false;
            if (stamps == null) stamps = new List<MapChunkStamp>();
            for (var i = 0; i < stamps.Count; i++)
                if (stamps[i] == stamp) return false;
            stamps.Add(stamp);
            return true;
        }

        public void CopyInto(List<MapChunkStamp> into)
        {
            if (into == null || stamps == null) return;
            for (var i = 0; i < stamps.Count; i++)
                if (stamps[i] != null) into.Add(stamps[i]);
        }
    }
}
