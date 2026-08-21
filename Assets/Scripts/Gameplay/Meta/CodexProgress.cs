using System;
using System.Collections.Generic;

namespace GemTD.Gameplay.Meta
{
    /// <summary>
    /// Tracks which <see cref="CodexEntry"/> ids the player has discovered.
    /// Owns NO display text (that lives on the SOs). Persisted via <see cref="ICodexStore"/>.
    /// </summary>
    public sealed class CodexProgress
    {
        readonly ICodexStore _store;
        readonly HashSet<string> _unlocked;

        public CodexProgress(ICodexStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _unlocked = new HashSet<string>(StringComparer.Ordinal);
            var dto = _store.Load();
            if (dto?.UnlockedIds != null)
            {
                for (var i = 0; i < dto.UnlockedIds.Length; i++)
                {
                    if (!string.IsNullOrEmpty(dto.UnlockedIds[i]))
                        _unlocked.Add(dto.UnlockedIds[i]);
                }
            }
        }

        public bool IsUnlocked(CodexEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Id))
                return false;
            return _unlocked.Contains(entry.Id);
        }

        public bool IsUnlocked(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            return _unlocked.Contains(id);
        }

        public void Unlock(CodexEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Id))
                return;
            if (_unlocked.Add(entry.Id))
                Save();
        }

        void Save()
        {
            var arr = new string[_unlocked.Count];
            _unlocked.CopyTo(arr);
            _store.Save(new CodexSaveDto { UnlockedIds = arr });
        }
    }
}