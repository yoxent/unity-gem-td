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
            var towers = CopyTowers(catalog, excludeTowers, roster);
            for (var n = 0; n < 4; n++)
            {
                var t = TakeTower(towers, rng);
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
            var towers = CopyTowers(catalog, excludeTowers, roster);

            for (var n = 0; n < 2; n++)
            {
                var gem = RollGem(TakeGemFamily(gems, rng), catalog.RarityTable, rng);
                if (gem.IsEmpty)
                    break;
                dest.Add(DraftOfferCard.FromGem(gem));
            }

            var guaranteedTower = TakeTower(towers, rng);
            if (guaranteedTower != null)
                dest.Add(DraftOfferCard.FromTower(guaranteedTower));

            var extraGemFamily = TakeGemFamily(gems, rng);
            var extraTower = TakeTower(towers, rng);
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
            List<TowerDefinition> exclude,
            TowerRoster roster)
        {
            var src = catalog.TowerPool != null ? catalog.TowerPool.GetTowersOrEmpty() : System.Array.Empty<TowerDefinition>();
            var list = new List<TowerDefinition>(src.Length);
            var rosterFull = roster != null && roster.IsFull;
            for (var i = 0; i < src.Length; i++)
            {
                if (src[i] == null || ContainsTower(exclude, src[i]))
                    continue;
                if (rosterFull && !roster.Contains(src[i]))
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

            var rarity = rarityTable != null
                ? rarityTable.Roll(rng)
                : GemRarity.Normal;
            return new GemInstance(definition, rarity);
        }

        static TowerDefinition TakeTower(List<TowerDefinition> pool, System.Random rng)
        {
            if (pool.Count == 0)
                return null;
            var i = PickIndex(pool.Count, rng, _ => 1f);
            var chosen = pool[i];
            pool.RemoveAt(i);
            return chosen;
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
