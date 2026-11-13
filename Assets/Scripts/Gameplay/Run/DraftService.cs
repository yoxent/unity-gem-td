using System;
using System.Collections.Generic;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Run
{
    public enum DraftReplacePhase
    {
        None = 0,
        AwaitingConfirm = 1,
        AwaitingInventoryPick = 2
    }

    /// <summary>
    /// Holds a 4-card draft offer and drives pick / skip / reroll / ban / full-bag gem replace.
    /// </summary>
    public sealed class DraftService
    {
        public const int RerollBaseCost = 50;
        public const int BanBaseCost = 250;

        readonly System.Random _rng;
        readonly List<DraftOfferCard> _offer = new List<DraftOfferCard>(4);
        readonly List<GemDefinition> _bannedGems = new List<GemDefinition>(8);
        readonly List<TowerDefinition> _bannedTowers = new List<TowerDefinition>(8);
        readonly List<GemDefinition> _excludeGems = new List<GemDefinition>(16);
        readonly List<TowerDefinition> _excludeTowers = new List<TowerDefinition>(16);

        DraftCatalog _catalog;
        int _rerollsThisOffer;
        int _bansThisRun;
        int _selectedIndex = -1;

        public IReadOnlyList<DraftOfferCard> CurrentOffer => _offer;
        public bool AllowSkip { get; private set; }
        public bool IsActive { get; private set; }
        public DraftReplacePhase ReplacePhase { get; private set; }
        public GemInstance PendingReplaceGem { get; private set; }
        public int SelectedIndex => _selectedIndex;
        public int NextRerollCost => ScaleRerollCost(_rerollsThisOffer);
        public int NextBanCost => BanBaseCost * (1 + _bansThisRun);

        public TowerRoster Roster { get; }

        public DraftService(System.Random rng) : this(rng, TowerRosterCaps.Default)
        {
        }

        public DraftService(System.Random rng, TowerRosterCaps caps)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            Roster = new TowerRoster(caps);
        }

        public int FilledCardCount
        {
            get
            {
                var n = 0;
                for (var i = 0; i < _offer.Count; i++)
                {
                    if (_offer[i].IsFilled)
                        n++;
                }

                return n;
            }
        }

        public void BeginOffer(DraftCatalog catalog, bool allowSkip)
        {
            _offer.Clear();
            PendingReplaceGem = default;
            ReplacePhase = DraftReplacePhase.None;
            AllowSkip = allowSkip;
            IsActive = false;
            _catalog = catalog;
            _rerollsThisOffer = 0;
            _selectedIndex = -1;

            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            FillOffer(excludeCurrent: false);
            if (FilledCardCount == 0)
                throw new ArgumentException("Draft catalog produced no cards.", nameof(catalog));

            IsActive = true;
        }

        public bool TrySelect(int offerIndex)
        {
            if (!IsActive || ReplacePhase != DraftReplacePhase.None)
                return false;
            if (!HasFilledCard(offerIndex))
                return false;

            _selectedIndex = offerIndex;
            return true;
        }

        public bool TryPick(int offerIndex, GemInventory inventory, out bool resolved)
        {
            resolved = false;
            if (!IsActive || ReplacePhase != DraftReplacePhase.None)
                return false;

            if (!HasFilledCard(offerIndex))
                return false;

            var card = _offer[offerIndex];
            if (card.IsTower)
            {
                Roster.ApplyPick(card.Tower);
                ClearOffer();
                resolved = true;
                return true;
            }

            if (!card.IsGem || inventory == null)
                return false;

            var gem = card.Gem;
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
                economy.GrantDraftSkipGold(skipGold);

            ClearOffer();
            resolved = true;
            return true;
        }

        public bool CanReroll(RunEconomy economy)
        {
            return IsActive
                && ReplacePhase == DraftReplacePhase.None
                && economy != null
                && economy.Gold >= NextRerollCost;
        }

        public bool CanBan(RunEconomy economy)
        {
            if (!IsActive || ReplacePhase != DraftReplacePhase.None || economy == null)
                return false;
            if (!HasFilledCard(_selectedIndex))
                return false;
            if (economy.Gold < NextBanCost)
                return false;
            if (!AllowSkip && FilledCardCount <= 1 && economy.Gold < NextRerollCost)
                return false;
            return true;
        }

        public bool TryReroll(RunEconomy economy)
        {
            if (!CanReroll(economy))
                return false;
            if (!economy.TrySpend(NextRerollCost))
                return false;

            _rerollsThisOffer++;
            _selectedIndex = -1;
            FillOffer(excludeCurrent: true);
            return true;
        }

        public bool TryBan(RunEconomy economy)
        {
            if (!CanBan(economy))
                return false;
            if (!economy.TrySpend(NextBanCost))
                return false;

            var card = _offer[_selectedIndex];
            if (card.IsGem)
                _bannedGems.Add(card.Gem.Def);
            else if (card.IsTower)
                _bannedTowers.Add(card.Tower);

            _offer[_selectedIndex] = default;
            _bansThisRun++;
            _selectedIndex = -1;
            return true;
        }

        public void ConfirmReplaceYes()
        {
            if (!IsActive || ReplacePhase != DraftReplacePhase.AwaitingConfirm || PendingReplaceGem.IsEmpty)
                return;

            ReplacePhase = DraftReplacePhase.AwaitingInventoryPick;
        }

        public void ConfirmReplaceNo()
        {
            if (!IsActive)
                return;

            PendingReplaceGem = default;
            ReplacePhase = DraftReplacePhase.None;
        }

        public void CancelReplace() => ConfirmReplaceNo();

        public bool TryCompleteReplace(int inventoryIndex, GemInventory inventory, out bool resolved)
        {
            resolved = false;
            if (!IsActive || inventory == null || ReplacePhase != DraftReplacePhase.AwaitingInventoryPick)
                return false;

            if (PendingReplaceGem.IsEmpty)
                return false;

            if (!inventory.TryDiscardAt(inventoryIndex, out _))
                return false;

            if (!inventory.TryAdd(PendingReplaceGem))
                return false;

            ClearOffer();
            resolved = true;
            return true;
        }

        void FillOffer(bool excludeCurrent)
        {
            _excludeGems.Clear();
            _excludeTowers.Clear();
            AppendExcludes(_bannedGems, _bannedTowers);
            if (excludeCurrent)
                SnapshotCurrentIntoExcludes();

            DraftOfferSampler.Fill(_offer, _catalog, _rng, shuffle: true, _excludeGems, _excludeTowers, Roster);
        }

        void SnapshotCurrentIntoExcludes()
        {
            for (var i = 0; i < _offer.Count; i++)
            {
                var card = _offer[i];
                if (card.IsGem)
                    _excludeGems.Add(card.Gem.Def);
                else if (card.IsTower)
                    _excludeTowers.Add(card.Tower);
            }
        }

        void AppendExcludes(List<GemDefinition> gems, List<TowerDefinition> towers)
        {
            for (var i = 0; i < gems.Count; i++)
            {
                if (gems[i] != null)
                    _excludeGems.Add(gems[i]);
            }

            for (var i = 0; i < towers.Count; i++)
            {
                if (towers[i] != null)
                    _excludeTowers.Add(towers[i]);
            }
        }

        bool HasFilledCard(int index)
        {
            return index >= 0 && index < _offer.Count && _offer[index].IsFilled;
        }

        static int ScaleRerollCost(int rerollsUsed)
        {
            var cost = RerollBaseCost;
            for (var i = 0; i < rerollsUsed; i++)
            {
                if (cost > int.MaxValue / 2)
                    return int.MaxValue;
                cost *= 2;
            }

            return cost;
        }

        void ClearOffer()
        {
            _offer.Clear();
            PendingReplaceGem = default;
            ReplacePhase = DraftReplacePhase.None;
            IsActive = false;
            AllowSkip = false;
            _selectedIndex = -1;
        }
    }
}
