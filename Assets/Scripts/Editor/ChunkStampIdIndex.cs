using System.Collections.Generic;
using GemTD.Gameplay.Map;

namespace GemTD.Editor
{
    static class ChunkStampIdIndex
    {
        public static void Load(ChunkTypeCatalog catalog, Dictionary<string, string> canonicalToName)
        {
            canonicalToName.Clear();
            if (catalog == null) return;
            var stamps = catalog.Stamps;
            for (var i = 0; i < stamps.Count; i++)
            {
                var stamp = stamps[i];
                if (stamp == null) continue;
                var id = ChunkMaskId.Canonical(stamp.GetMask());
                if (!canonicalToName.ContainsKey(id))
                    canonicalToName[id] = stamp.gameObject.name;
            }
        }
    }
}
