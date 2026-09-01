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
        readonly TowerRosterCaps _caps;
        readonly List<TowerDefinition> _unlocked;
        readonly List<int> _levelIndex;

        public TowerRoster() : this(TowerRosterCaps.Default)
        {
        }

        public TowerRoster(TowerRosterCaps caps)
        {
            _caps = caps;
            var capacity = caps.MaxSlots > 0 ? caps.MaxSlots : 1;
            _unlocked = new List<TowerDefinition>(capacity);
            _levelIndex = new List<int>(capacity);
        }

        public int MaxSlots => _caps.MaxSlots;

        public int Count => _unlocked.Count;

        public bool IsFull =>
            Remaining(TowerRosterCategory.Damaging) == 0
            && Remaining(TowerRosterCategory.Curse) == 0
            && Remaining(TowerRosterCategory.Aura) == 0;

        public int CountIn(TowerRosterCategory category)
        {
            var n = 0;
            for (var i = 0; i < _unlocked.Count; i++)
            {
                if (TowerRosterCategoryRules.Of(_unlocked[i]) == category)
                    n++;
            }

            return n;
        }

        public int Remaining(TowerRosterCategory category)
        {
            var left = _caps.Cap(category) - CountIn(category);
            return left < 0 ? 0 : left;
        }

        public bool CanUnlock(TowerDefinition def)
        {
            if (def == null || Contains(def))
                return false;
            return Remaining(TowerRosterCategoryRules.Of(def)) > 0;
        }

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
                if (!CanUnlock(def))
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
            return card.DisplayName ?? "";
        }

        public static string FormatOfferStatus(DraftOfferCard card, TowerRoster roster)
        {
            if (!card.IsTower)
                return "";
            if (roster != null && roster.Contains(card.Tower))
                return "Upgrade";
            return "New";
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
