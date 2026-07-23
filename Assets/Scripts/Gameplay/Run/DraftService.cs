using System;
using System.Collections.Generic;
using GemTD.Gameplay.Gems;

namespace GemTD.Gameplay.Run
{
    public enum DraftReplacePhase
    {
        None = 0,
        AwaitingConfirm = 1,
        AwaitingInventoryPick = 2
    }

    /// <summary>
    /// Samples draft offers and drives pick / skip / full-bag replace flow.
    /// </summary>
    public sealed class DraftService
    {
        readonly System.Random _rng;
        readonly List<GemDefinition> _offer = new List<GemDefinition>(3);
        readonly List<GemDefinition> _scratch = new List<GemDefinition>(16);

        public IReadOnlyList<GemDefinition> CurrentOffer => _offer;
        public bool AllowSkip { get; private set; }
        public bool IsActive { get; private set; }
        public DraftReplacePhase ReplacePhase { get; private set; }
        public GemDefinition PendingReplaceGem { get; private set; }

        public DraftService(System.Random rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        public void BeginOffer(IReadOnlyList<GemDefinition> pool, bool allowSkip)
        {
            _offer.Clear();
            PendingReplaceGem = null;
            ReplacePhase = DraftReplacePhase.None;
            AllowSkip = allowSkip;
            IsActive = false;

            if (pool == null || pool.Count < 3)
                throw new ArgumentException("Draft pool must contain at least 3 gems.", nameof(pool));

            _scratch.Clear();
            for (var i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null)
                    _scratch.Add(pool[i]);
            }

            if (_scratch.Count < 3)
                throw new ArgumentException("Draft pool must contain at least 3 non-null gems.", nameof(pool));

            // Fisher–Yates partial shuffle for 3 unique picks.
            for (var i = 0; i < 3; i++)
            {
                var j = i + _rng.Next(_scratch.Count - i);
                var tmp = _scratch[i];
                _scratch[i] = _scratch[j];
                _scratch[j] = tmp;
                _offer.Add(_scratch[i]);
            }

            IsActive = true;
        }

        public bool TryPick(int offerIndex, GemInventory inventory, out bool resolved)
        {
            resolved = false;
            if (!IsActive || inventory == null || ReplacePhase != DraftReplacePhase.None)
                return false;

            if (offerIndex < 0 || offerIndex >= _offer.Count)
                return false;

            var gem = _offer[offerIndex];
            if (gem == null)
                return false;

            if (inventory.FreeSlotCount > 0)
            {
                if (!inventory.TryAdd(gem))
                    return false;

                ClearOffer();
                resolved = true;
                return true;
            }

            PendingReplaceGem = gem;
            ReplacePhase = DraftReplacePhase.AwaitingConfirm;
            return true;
        }

        public bool TrySkip(RunEconomy economy, int skipGold, out bool resolved)
        {
            resolved = false;
            if (!IsActive || !AllowSkip || ReplacePhase != DraftReplacePhase.None)
                return false;

            if (economy == null)
                return false;

            if (skipGold > 0)
                economy.AddGold(skipGold);

            ClearOffer();
            resolved = true;
            return true;
        }

        public void ConfirmReplaceYes()
        {
            if (!IsActive || ReplacePhase != DraftReplacePhase.AwaitingConfirm || PendingReplaceGem == null)
                return;

            ReplacePhase = DraftReplacePhase.AwaitingInventoryPick;
        }

        public void ConfirmReplaceNo()
        {
            if (!IsActive)
                return;

            PendingReplaceGem = null;
            ReplacePhase = DraftReplacePhase.None;
        }

        public void CancelReplace() => ConfirmReplaceNo();

        public bool TryCompleteReplace(int inventoryIndex, GemInventory inventory, out bool resolved)
        {
            resolved = false;
            if (!IsActive || inventory == null || ReplacePhase != DraftReplacePhase.AwaitingInventoryPick)
                return false;

            if (PendingReplaceGem == null)
                return false;

            if (!inventory.TryDiscardAt(inventoryIndex, out _))
                return false;

            if (!inventory.TryAdd(PendingReplaceGem))
                return false;

            ClearOffer();
            resolved = true;
            return true;
        }

        void ClearOffer()
        {
            _offer.Clear();
            PendingReplaceGem = null;
            ReplacePhase = DraftReplacePhase.None;
            IsActive = false;
            AllowSkip = false;
        }
    }
}
