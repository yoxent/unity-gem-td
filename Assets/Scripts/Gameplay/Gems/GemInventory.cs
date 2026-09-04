using System.Collections.Generic;

namespace GemTD.Gameplay.Gems
{
    public sealed class GemInventory
    {
        readonly GemInstance[] _slots;

        public IReadOnlyList<GemInstance> Slots => _slots;

        public int Capacity => _slots.Length;

        public int OccupiedCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _slots.Length; i++)
                {
                    if (!_slots[i].IsEmpty)
                        count++;
                }

                return count;
            }
        }

        public int FreeSlotCount => Capacity - OccupiedCount;

        public GemInventory(int capacity)
        {
            var size = capacity > 0 ? capacity : 1;
            _slots = new GemInstance[size];
        }

        public void Seed(IReadOnlyList<GemDefinition> gems)
        {
            for (var i = 0; i < _slots.Length; i++)
                _slots[i] = default;

            if (gems == null)
                return;

            var count = gems.Count < _slots.Length ? gems.Count : _slots.Length;
            for (var i = 0; i < count; i++)
                _slots[i] = GemInstance.FromDefinition(gems[i]);

            CombineTriples();
        }

        public bool TryAdd(GemInstance gem)
        {
            if (gem.IsEmpty)
                return false;

            for (var i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].IsEmpty)
                    continue;

                _slots[i] = gem;
                CombineTriples();
                return true;
            }

            return false;
        }

        public bool TryAdd(GemDefinition gem)
        {
            return TryAdd(GemInstance.FromDefinition(gem));
        }

        public bool TryAddAt(int index, GemInstance gem)
        {
            if (gem.IsEmpty || index < 0 || index >= _slots.Length || !_slots[index].IsEmpty)
                return false;

            _slots[index] = gem;
            CombineTriples();
            return true;
        }

        public bool TryAddAt(int index, GemDefinition gem)
        {
            return TryAddAt(index, GemInstance.FromDefinition(gem));
        }

        public bool TryTake(GemId id, out GemInstance gem)
        {
            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsEmpty || _slots[i].Id != id)
                    continue;

                gem = _slots[i];
                _slots[i] = default;
                return true;
            }

            gem = default;
            return false;
        }

        public bool TryDiscardAt(int index, out GemInstance discarded)
        {
            if (index < 0 || index >= _slots.Length || _slots[index].IsEmpty)
            {
                discarded = default;
                return false;
            }

            discarded = _slots[index];
            _slots[index] = default;
            return true;
        }

        public bool TryTakeAt(int index, out GemInstance gem)
        {
            if (index < 0 || index >= _slots.Length || _slots[index].IsEmpty)
            {
                gem = default;
                return false;
            }

            gem = _slots[index];
            _slots[index] = default;
            return true;
        }

        /// <summary>
        /// Moves a gem from <paramref name="fromIndex"/> to <paramref name="toIndex"/>.
        /// If <paramref name="toIndex"/> is occupied, swaps the two gems.
        /// </summary>
        public bool TryMoveOrSwapAt(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex)
                return false;

            if (fromIndex < 0 || fromIndex >= _slots.Length
                || toIndex < 0 || toIndex >= _slots.Length)
                return false;

            var fromGem = _slots[fromIndex];
            if (fromGem.IsEmpty)
                return false;

            var toGem = _slots[toIndex]; // may be empty
            _slots[fromIndex] = toGem;
            _slots[toIndex] = fromGem;
            return true;
        }

        /// <summary>
        /// Fuse every bag triple of the same family and rarity into the next rarity.
        /// Greater does not fuse. Sockets are not this inventory. Cascades until no triple remains.
        /// </summary>
        public int CombineTriples()
        {
            var fuses = 0;
            while (TryCombineOnce())
                fuses++;
            return fuses;
        }

        bool TryCombineOnce()
        {
            for (var i = 0; i < _slots.Length; i++)
            {
                var first = _slots[i];
                if (first.IsEmpty)
                    continue;
                if (!GemRarityUtility.TryNext(first.Rarity, out var next))
                    continue;

                var i0 = -1;
                var i1 = -1;
                var i2 = -1;
                for (var j = 0; j < _slots.Length; j++)
                {
                    var slot = _slots[j];
                    if (slot.IsEmpty || slot.Id != first.Id || slot.Rarity != first.Rarity)
                        continue;
                    if (i0 < 0)
                        i0 = j;
                    else if (i1 < 0)
                        i1 = j;
                    else
                    {
                        i2 = j;
                        break;
                    }
                }

                if (i2 < 0)
                    continue;

                _slots[i0] = new GemInstance(first.Def, next);
                _slots[i1] = default;
                _slots[i2] = default;
                return true;
            }

            return false;
        }
    }
}
