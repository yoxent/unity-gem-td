using System.Collections.Generic;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Run
{
    /// <summary>Builds a 4-card offer from a <see cref="DraftCatalog"/>. No LINQ.</summary>
    public static class DraftOfferSampler
    {
        public static void Fill(
            List<DraftOfferCard> dest,
            DraftCatalog catalog,
            System.Random rng,
            bool shuffle)
        {
            Fill(dest, catalog, rng, shuffle, null, null, null);
        }

        public static void Fill(
            List<DraftOfferCard> dest,
            DraftCatalog catalog,
            System.Random rng,
            bool shuffle,
            List<GemDefinition> excludeGems,
            List<TowerDefinition> excludeTowers)
        {
            Fill(dest, catalog, rng, shuffle, excludeGems, excludeTowers, null);
        }

        public static void Fill(
            List<DraftOfferCard> dest,
            DraftCatalog catalog,
            System.Random rng,
            bool shuffle,
            List<GemDefinition> excludeGems,
            List<TowerDefinition> excludeTowers,
            TowerRoster roster)
        {
            if (dest == null)
                throw new System.ArgumentNullException(nameof(dest));
            dest.Clear();
            if (catalog == null || rng == null)
                return;

            if (catalog.Mix == DraftMixKind.FourTowers)
                FillFourTowers(dest, catalog, rng, excludeTowers, roster);
            else
                FillCampaign(dest, catalog, rng, excludeGems, excludeTowers, roster);

            if (shuffle)
                Shuffle(dest, rng);
        }

        static void FillFourTowers(
            List<DraftOfferCard> dest,
            DraftCatalog catalog,
            System.Random rng,
            List<TowerDefinition> excludeTowers,
            TowerRoster roster)
        {
            var towers = CopyTowers(catalog, excludeTowers);
            for (var n = 0; n < 4; n++)
            {
                var t = TakeTower(towers, rng, roster, damagingOnly: true);
                if (t == null)
                    break;
                dest.Add(DraftOfferCard.FromTower(t));
            }
        }

        static void FillCampaign(
            List<DraftOfferCard> dest,
            DraftCatalog catalog,
            System.Random rng,
            List<GemDefinition> excludeGems,
            List<TowerDefinition> excludeTowers,
            TowerRoster roster)
        {
            var gems = CopyGems(catalog, excludeGems);
            var towers = CopyTowers(catalog, excludeTowers);

            for (var n = 0; n < 2; n++)
            {
                var gem = RollGem(TakeGemFamily(gems, rng), catalog.RarityTable, rng);
                if (gem.IsEmpty)
                    break;
                dest.Add(DraftOfferCard.FromGem(gem));
            }

            var guaranteedTower = TakeTower(towers, rng, roster, damagingOnly: false);
            if (guaranteedTower != null)
                dest.Add(DraftOfferCard.FromTower(guaranteedTower));

            var extraGemFamily = TakeGemFamily(gems, rng);
            var extraTower = TakeTower(towers, rng, roster, damagingOnly: false);
            if (extraGemFamily != null && extraTower != null)
            {
                if (rng.Next(0, 2) == 0)
                    dest.Add(DraftOfferCard.FromGem(RollGem(extraGemFamily, catalog.RarityTable, rng)));
                else
                    dest.Add(DraftOfferCard.FromTower(extraTower));
            }
            else if (extraGemFamily != null)
                dest.Add(DraftOfferCard.FromGem(RollGem(extraGemFamily, catalog.RarityTable, rng)));
            else if (extraTower != null)
                dest.Add(DraftOfferCard.FromTower(extraTower));
        }

        static List<GemDefinition> CopyGems(DraftCatalog catalog, List<GemDefinition> exclude)
        {
            var src = catalog.GemPool != null ? catalog.GemPool.GetGemsOrEmpty() : System.Array.Empty<GemDefinition>();
            var list = new List<GemDefinition>(src.Length);
            for (var i = 0; i < src.Length; i++)
            {
                if (src[i] != null && !ContainsGem(exclude, src[i]))
                    list.Add(src[i]);
            }

            return list;
        }

        static List<TowerDefinition> CopyTowers(
            DraftCatalog catalog,
            List<TowerDefinition> exclude)
        {
            var src = catalog.TowerPool != null ? catalog.TowerPool.GetTowersOrEmpty() : System.Array.Empty<TowerDefinition>();
            var list = new List<TowerDefinition>(src.Length);
            for (var i = 0; i < src.Length; i++)
            {
                if (src[i] == null || ContainsTower(exclude, src[i]))
                    continue;
                list.Add(src[i]);
            }

            return list;
        }

        static bool ContainsGem(List<GemDefinition> exclude, GemDefinition gem)
        {
            if (exclude == null)
                return false;
            for (var i = 0; i < exclude.Count; i++)
            {
                if (ReferenceEquals(exclude[i], gem))
                    return true;
            }

            return false;
        }

        static bool ContainsTower(List<TowerDefinition> exclude, TowerDefinition tower)
        {
            if (exclude == null)
                return false;
            for (var i = 0; i < exclude.Count; i++)
            {
                if (ReferenceEquals(exclude[i], tower))
                    return true;
            }

            return false;
        }

        static GemDefinition TakeGemFamily(List<GemDefinition> pool, System.Random rng)
        {
            if (pool.Count == 0)
                return null;

            var index = PickIndex(pool.Count, rng, j => Weight(pool[j].DraftWeight));
            var definition = pool[index];
            pool.RemoveAt(index);
            return definition;
        }

        static GemInstance RollGem(
            GemDefinition definition,
            GemRarityTable rarityTable,
            System.Random rng)
        {
            if (definition == null)
                return default;

            return new GemInstance(definition, RollRarity(definition, rarityTable, rng));
        }

        static GemRarity RollRarity(
            GemDefinition definition,
            GemRarityTable rarityTable,
            System.Random rng)
        {
            if (definition.HasCustomRarityWeights)
            {
                return GemRarityTable.Roll(
                    rng,
                    definition.LesserRarityWeight,
                    definition.NormalRarityWeight,
                    definition.GreaterRarityWeight);
            }

            return rarityTable != null
                ? rarityTable.Roll(rng)
                : GemRarity.Normal;
        }

        static TowerDefinition TakeTower(
            List<TowerDefinition> pool,
            System.Random rng,
            TowerRoster roster,
            bool damagingOnly)
        {
            var damaging = new List<TowerDefinition>(pool.Count);
            var curses = new List<TowerDefinition>(pool.Count);
            var auras = new List<TowerDefinition>(pool.Count);

            for (var i = 0; i < pool.Count; i++)
            {
                var tower = pool[i];
                var category = TowerRosterCategoryRules.Of(tower);
                if (damagingOnly && category != TowerRosterCategory.Damaging)
                    continue;
                if (!IsEligible(tower, roster))
                    continue;
                if (category == TowerRosterCategory.Curse)
                    curses.Add(tower);
                else if (category == TowerRosterCategory.Aura)
                    auras.Add(tower);
                else
                    damaging.Add(tower);
            }

            var chosenList = PickBucket(damaging, curses, auras, rng, roster);
            if (chosenList == null || chosenList.Count == 0)
                return null;

            var pick = chosenList[rng.Next(0, chosenList.Count)];
            pool.Remove(pick);
            return pick;
        }

        static bool IsEligible(TowerDefinition tower, TowerRoster roster)
        {
            if (roster == null)
                return true;
            if (roster.Contains(tower))
                return true;
            return roster.Remaining(TowerRosterCategoryRules.Of(tower)) > 0;
        }

        static List<TowerDefinition> PickBucket(
            List<TowerDefinition> damaging,
            List<TowerDefinition> curses,
            List<TowerDefinition> auras,
            System.Random rng,
            TowerRoster roster)
        {
            var wD = BucketWeight(damaging, TowerRosterCategory.Damaging, roster);
            var wC = BucketWeight(curses, TowerRosterCategory.Curse, roster);
            var wA = BucketWeight(auras, TowerRosterCategory.Aura, roster);
            var total = wD + wC + wA;
            if (total <= 0f)
                return null;

            var roll = (float)rng.NextDouble() * total;
            if (roll < wD)
                return damaging;
            if (roll < wD + wC)
                return curses;
            return auras;
        }

        static float BucketWeight(
            List<TowerDefinition> eligible,
            TowerRosterCategory category,
            TowerRoster roster)
        {
            if (eligible.Count == 0)
                return 0f;

            var hasNew = false;
            var hasUpgrade = false;
            for (var i = 0; i < eligible.Count; i++)
            {
                if (roster != null && roster.Contains(eligible[i]))
                    hasUpgrade = true;
                else
                    hasNew = true;
            }

            if (hasNew)
            {
                if (roster == null)
                    return TowerRosterCaps.Default.Cap(category);
                return roster.Remaining(category);
            }

            return hasUpgrade ? 1f : 0f;
        }

        static int PickIndex(int count, System.Random rng, System.Func<int, float> weightAt)
        {
            var total = 0f;
            for (var i = 0; i < count; i++)
                total += weightAt(i);
            if (total <= 0f)
                return rng.Next(0, count);

            var roll = (float)rng.NextDouble() * total;
            var acc = 0f;
            for (var i = 0; i < count; i++)
            {
                acc += weightAt(i);
                if (roll < acc)
                    return i;
            }

            return count - 1;
        }

        static float Weight(float authored)
        {
            if (authored <= 0f)
                return 1f;
            return authored;
        }

        static void Shuffle(List<DraftOfferCard> dest, System.Random rng)
        {
            for (var i = dest.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                var tmp = dest[i];
                dest[i] = dest[j];
                dest[j] = tmp;
            }
        }
    }
}
