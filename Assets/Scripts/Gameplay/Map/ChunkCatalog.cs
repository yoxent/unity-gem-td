using System.Collections.Generic;
using UnityEngine;

namespace GemTD.Gameplay.Map
{
    public interface IChunkCatalog
    {
        void CopyAll(List<MapChunkStamp> into);
        void CopyType(ChunkType type, List<MapChunkStamp> into);
    }

    [CreateAssetMenu(menuName = "Gem TD/Chunk Catalog")]
    public sealed class ChunkCatalog : ScriptableObject, IChunkCatalog
    {
        [SerializeField] ChunkTypeCatalog deadend;
        [SerializeField] ChunkTypeCatalog straight;
        [SerializeField] ChunkTypeCatalog corner;
        [SerializeField] ChunkTypeCatalog tjunction;
        [SerializeField] ChunkTypeCatalog cross;
        [SerializeField] ChunkTypeCatalog homebase;

        public ChunkTypeCatalog CatalogFor(ChunkType type)
        {
            switch (type)
            {
                case ChunkType.DeadEnd: return deadend;
                case ChunkType.Straight: return straight;
                case ChunkType.Corner: return corner;
                case ChunkType.TJunction: return tjunction;
                case ChunkType.Cross: return cross;
                case ChunkType.Homebase: return homebase;
                default: return null;
            }
        }

        public void SetTypeCatalog(ChunkType type, ChunkTypeCatalog catalog)
        {
            switch (type)
            {
                case ChunkType.DeadEnd: deadend = catalog; break;
                case ChunkType.Straight: straight = catalog; break;
                case ChunkType.Corner: corner = catalog; break;
                case ChunkType.TJunction: tjunction = catalog; break;
                case ChunkType.Cross: cross = catalog; break;
                case ChunkType.Homebase: homebase = catalog; break;
            }
        }

        public void CopyAll(List<MapChunkStamp> into)
        {
            into.Clear();
            // Expand picks only path chunks. Homebase is start-layout-only.
            if (deadend != null) deadend.CopyInto(into);
            if (straight != null) straight.CopyInto(into);
            if (corner != null) corner.CopyInto(into);
            if (tjunction != null) tjunction.CopyInto(into);
            if (cross != null) cross.CopyInto(into);
        }

        public void CopyType(ChunkType type, List<MapChunkStamp> into)
        {
            into.Clear();
            var cat = CatalogFor(type);
            if (cat != null) cat.CopyInto(into);
        }
    }
}
