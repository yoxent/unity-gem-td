using System.Collections.Generic;

namespace GemTD.Gameplay.Gems
{
    public sealed class GemInventory
    {
        readonly GemDefinition[] _slots;

        public IReadOnlyList<GemDefinition> Slots => _slots;

        public int Capacity => _slots.Length;

        public int OccupiedCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i] != null)
                        count++;
                }

                return count;
            }
        }

        public int FreeSlotCount => Capacity - OccupiedCount;

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

        public bool TryDiscardAt(int index, out GemDefinition discarded)
        {
            if (index < 0 || index >= _slots.Length || _slots[index] == null)
            {
                discarded = null;
                return false;
            }

            discarded = _slots[index];
            _slots[index] = null;
            return true;
        }

        public bool TryTakeAt(int index, out GemDefinition gem)
        {
            if (index < 0 || index >= _slots.Length || _slots[index] == null)
            {
                gem = null;
                return false;
            }

            gem = _slots[index];
            _slots[index] = null;
            return true;
        }
    }
}
