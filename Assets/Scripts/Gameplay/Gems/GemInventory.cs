using System.Collections.Generic;

namespace GemTD.Gameplay.Gems
{
    public sealed class GemInventory
    {
        readonly GemDefinition[] _slots;

        public IReadOnlyList<GemDefinition> Slots => _slots;

        public GemInventory(int capacity)
        {
            var size = capacity > 0 ? capacity : 1;
            _slots = new GemDefinition[size];
        }

        public void Seed(IReadOnlyList<GemDefinition> gems)
        {
            for (var i = 0; i < _slots.Length; i++)
                _slots[i] = null;

            if (gems == null)
                return;

            var count = gems.Count < _slots.Length ? gems.Count : _slots.Length;
            for (var i = 0; i < count; i++)
                _slots[i] = gems[i];
        }

        public bool TryAdd(GemDefinition gem)
        {
            if (gem == null)
                return false;

            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                    continue;

                _slots[i] = gem;
                return true;
            }

            return false;
        }

        public bool TryTake(GemId id, out GemDefinition gem)
        {
            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null || _slots[i].Id != id)
                    continue;

                gem = _slots[i];
                _slots[i] = null;
                return true;
            }

            gem = null;
            return false;
        }
    }
}
