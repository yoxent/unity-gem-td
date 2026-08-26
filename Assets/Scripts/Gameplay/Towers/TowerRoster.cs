using System.Collections.Generic;
using GemTD.Gameplay.Run;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Run-owned unlocked tower types and their draft level index (0 = in-game level 1).
    /// Writing that index onto a <see cref="TowerInstance"/> selects role <c>Levels[]</c> modifiers and effects (combat Level 1–10).
    /// </summary>
    public sealed class TowerRoster
    {
        public const int MaxSlots = 10;

        readonly List<TowerDefinition> _unlocked = new List<TowerDefinition>(MaxSlots);
        readonly List<int> _levelIndex = new List<int>(MaxSlots);

        public int Count => _unlocked.Count;

        public bool IsFull => _unlocked.Count >= MaxSlots;

        public bool Contains(TowerDefinition def)
        {
            return IndexOf(def) >= 0;
        }

        public bool TryGetAt(int index, out TowerDefinition def)
        {
            def = null;
            if (index < 0 || index >= _unlocked.Count)
                return false;
            def = _unlocked[index];
            return def != null;
        }

        public int GetLevelIndex(TowerDefinition def)
        {
            var i = IndexOf(def);
            return i < 0 ? 0 : _levelIndex[i];
        }

        public int GetDisplayLevel(TowerDefinition def) => GetLevelIndex(def) + 1;

        public void ApplyPick(TowerDefinition def)
        {
            if (def == null)
                return;

            var i = IndexOf(def);
            if (i < 0)
            {
                if (_unlocked.Count >= MaxSlots)
                    return;
                _unlocked.Add(def);
                _levelIndex.Add(0);
                return;
            }

            _levelIndex[i]++;
        }

        public void ApplyLevels(IList<TowerInstance> placed)
        {
            if (placed == null)
                return;

            for (var i = 0; i < placed.Count; i++)
            {
                var tower = placed[i];
                if (tower == null || tower.Def == null)
                    continue;
                if (!Contains(tower.Def))
                    continue;
                tower.LevelIndex = GetLevelIndex(tower.Def);
            }
        }

        public TowerDefinition[] CopyTypes()
        {
            return _unlocked.ToArray();
        }

        public static string FormatOfferLabel(DraftOfferCard card, TowerRoster roster)
        {
            if (card.IsGem)
                return card.DisplayName;
            if (!card.IsTower)
                return "";

            var name = card.DisplayName;
            if (roster != null && roster.Contains(card.Tower))
                return name + "\nUpgrade to level " + (roster.GetDisplayLevel(card.Tower) + 1);
            return name + "\nUnlock";
        }

        public static string FormatBarLabel(TowerDefinition def, TowerRoster roster)
        {
            if (def == null)
                return "?";
            var name = !string.IsNullOrEmpty(def.DisplayName) ? def.DisplayName : def.name;
            if (roster == null || !roster.Contains(def))
                return name;
            return name + "  Lv " + roster.GetDisplayLevel(def);
        }

        int IndexOf(TowerDefinition def)
        {
            if (def == null)
                return -1;
            for (var i = 0; i < _unlocked.Count; i++)
            {
                if (_unlocked[i] == def)
                    return i;
            }

            return -1;
        }
    }
}
