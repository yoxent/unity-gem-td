using UnityEngine;

namespace GemTD.Gameplay.Meta
{
    /// <summary>Ordered catalog of <see cref="CodexEntry"/>. Static authoring asset.</summary>
    [CreateAssetMenu(menuName = "Gem TD/Codex Catalog", fileName = "CodexCatalog")]
    public sealed class CodexCatalog : ScriptableObject
    {
        public CodexEntry[] Entries;

        public CodexEntry GetById(string id)
        {
            if (Entries == null)
                return null;

            for (var i = 0; i < Entries.Length; i++)
            {
                var e = Entries[i];
                if (e != null && e.Id == id)
                    return e;
            }
            return null;
        }
    }
}