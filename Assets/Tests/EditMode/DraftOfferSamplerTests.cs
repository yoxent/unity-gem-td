using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class DraftOfferSamplerTests
    {
        readonly List<Object> _destroy = new List<Object>(32);

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _destroy.Count; i++)
            {
                if (_destroy[i] != null)
                    Object.DestroyImmediate(_destroy[i]);
            }
            _destroy.Clear();
        }

        [Test]
        public void FourTowers_TakesFourUnique()
        {
            var catalog = MakeCatalog(DraftMixKind.FourTowers, gems: 0, towers: 6);
            var dest = new List<DraftOfferCard>(4);
            DraftOfferSampler.Fill(dest, catalog, new System.Random(1), shuffle: false);

            Assert.AreEqual(4, dest.Count);
            for (var i = 0; i < dest.Count; i++)
            {
                Assert.IsTrue(dest[i].IsTower);
                for (var j = i + 1; j < dest.Count; j++)
                    Assert.AreNotSame(dest[i].Tower, dest[j].Tower);
            }
        }

        [Test]
        public void FourTowers_ShortPool_StopsEarly()
        {
            var catalog = MakeCatalog(DraftMixKind.FourTowers, gems: 0, towers: 2);
            var dest = new List<DraftOfferCard>(4);
            DraftOfferSampler.Fill(dest, catalog, new System.Random(1), shuffle: false);
            Assert.AreEqual(2, dest.Count);
        }

        [Test]
        public void Campaign_TwoGemsThenTowerThenContested()
        {
            var catalog = MakeCatalog(DraftMixKind.TwoGemsOneTowerContested, gems: 5, towers: 4);
            var dest = new List<DraftOfferCard>(4);
            DraftOfferSampler.Fill(dest, catalog, new System.Random(7), shuffle: false);

            Assert.AreEqual(4, dest.Count);
            Assert.IsTrue(dest[0].IsGem);
            Assert.IsTrue(dest[1].IsGem);
            Assert.AreNotSame(dest[0].Gem.Def, dest[1].Gem.Def);
            Assert.IsTrue(dest[2].IsTower);
            Assert.IsTrue(dest[3].IsGem || dest[3].IsTower);
            if (dest[3].IsGem)
            {
                Assert.AreNotSame(dest[3].Gem.Def, dest[0].Gem.Def);
                Assert.AreNotSame(dest[3].Gem.Def, dest[1].Gem.Def);
            }
            else
                Assert.AreNotSame(dest[3].Tower, dest[2].Tower);
        }

        [Test]
        public void CampaignSampling_RollsRarityAfterUniqueFamilySelection()
        {
            var catalog = MakeCatalog(DraftMixKind.TwoGemsOneTowerContested, gems: 5, towers: 4);
            catalog.RarityTable = MakeRarityTable(lesser: 0f, normal: 0f, greater: 1f);
            var offer = new List<DraftOfferCard>();
            DraftOfferSampler.Fill(offer, catalog, new System.Random(42), shuffle: false);

            var seen = new List<GemDefinition>();
            for (var i = 0; i < offer.Count; i++)
            {
                if (!offer[i].IsGem)
                    continue;
                Assert.AreEqual(GemRarity.Greater, offer[i].Gem.Rarity);
                Assert.IsFalse(ContainsDefinition(seen, offer[i].Gem.Def));
                seen.Add(offer[i].Gem.Def);
            }
        }

        [Test]
        public void CampaignSampling_NullRarityTable_UsesNormal()
        {
            var catalog = MakeCatalog(DraftMixKind.TwoGemsOneTowerContested, gems: 5, towers: 4);
            var offer = new List<DraftOfferCard>();
            DraftOfferSampler.Fill(offer, catalog, new System.Random(42), shuffle: false);

            for (var i = 0; i < offer.Count; i++)
            {
                if (offer[i].IsGem)
                    Assert.AreEqual(GemRarity.Normal, offer[i].Gem.Rarity);
            }
        }

        [Test]
        public void CampaignSampling_RarityTable_CanProduceMixedRarities()
        {
            var catalog = MakeCatalog(DraftMixKind.TwoGemsOneTowerContested, gems: 5, towers: 4);
            catalog.RarityTable = MakeRarityTable(lesser: 1f, normal: 1f, greater: 1f);
            var offer = new List<DraftOfferCard>();
            var sawLesser = false;
            var sawNormal = false;
            var sawGreater = false;

            for (var seed = 1; seed <= 80; seed++)
            {
                DraftOfferSampler.Fill(offer, catalog, new System.Random(seed), shuffle: false);
                for (var i = 0; i < offer.Count; i++)
                {
                    if (!offer[i].IsGem)
                        continue;
                    sawLesser |= offer[i].Gem.Rarity == GemRarity.Lesser;
                    sawNormal |= offer[i].Gem.Rarity == GemRarity.Normal;
                    sawGreater |= offer[i].Gem.Rarity == GemRarity.Greater;
                }
            }

            Assert.IsTrue(sawLesser);
            Assert.IsTrue(sawNormal);
            Assert.IsTrue(sawGreater);
        }

        [Test]
        public void CampaignSampling_FamilyRarityOverride_BeatsCatalogTable()
        {
            var catalog = MakeCatalog(DraftMixKind.TwoGemsOneTowerContested, gems: 3, towers: 2);
            catalog.RarityTable = MakeRarityTable(lesser: 1f, normal: 0f, greater: 0f);
            catalog.GemPool.Gems[0].LesserRarityWeight = 0f;
            catalog.GemPool.Gems[0].NormalRarityWeight = 0f;
            catalog.GemPool.Gems[0].GreaterRarityWeight = 1f;
            catalog.GemPool.Gems[1].LesserRarityWeight = 0f;
            catalog.GemPool.Gems[1].NormalRarityWeight = 0f;
            catalog.GemPool.Gems[1].GreaterRarityWeight = 1f;
            catalog.GemPool.Gems[2].LesserRarityWeight = 0f;
            catalog.GemPool.Gems[2].NormalRarityWeight = 0f;
            catalog.GemPool.Gems[2].GreaterRarityWeight = 1f;

            var offer = new List<DraftOfferCard>();
            DraftOfferSampler.Fill(offer, catalog, new System.Random(11), shuffle: false);

            for (var i = 0; i < offer.Count; i++)
            {
                if (offer[i].IsGem)
                    Assert.AreEqual(GemRarity.Greater, offer[i].Gem.Rarity);
            }
        }

        [Test]
        public void Campaign_ContestedSlot_BothKindsAppear()
        {
            var catalog = MakeCatalog(DraftMixKind.TwoGemsOneTowerContested, gems: 5, towers: 4);
            var dest = new List<DraftOfferCard>(4);
            var gemFourths = 0;
            var towerFourths = 0;
            for (var seed = 1; seed <= 80; seed++)
            {
                DraftOfferSampler.Fill(dest, catalog, new System.Random(seed), shuffle: false);
                if (dest.Count < 4)
                    continue;
                if (dest[3].IsGem)
                    gemFourths++;
                else
                    towerFourths++;
            }

            Assert.Greater(gemFourths, 5);
            Assert.Greater(towerFourths, 5);
        }

        [Test]
        public void Campaign_ShortGemPool_OmitsMissingCards()
        {
            var catalog = MakeCatalog(DraftMixKind.TwoGemsOneTowerContested, gems: 1, towers: 4);
            var dest = new List<DraftOfferCard>(4);
            DraftOfferSampler.Fill(dest, catalog, new System.Random(1), shuffle: false);

            Assert.AreEqual(3, dest.Count);
            Assert.IsTrue(dest[0].IsGem);
            Assert.IsTrue(dest[1].IsTower);
            Assert.IsTrue(dest[2].IsTower);
            Assert.AreNotSame(dest[1].Tower, dest[2].Tower);
        }

        [Test]
        public void FourTowers_Exclude_SkipsThoseTowers()
        {
            var catalog = MakeCatalog(DraftMixKind.FourTowers, gems: 0, towers: 4);
            var exclude = new List<TowerDefinition>(2);
            exclude.Add(catalog.TowerPool.Towers[0]);
            exclude.Add(catalog.TowerPool.Towers[1]);
            var dest = new List<DraftOfferCard>(4);
            DraftOfferSampler.Fill(dest, catalog, new System.Random(1), shuffle: false, null, exclude);

            Assert.AreEqual(2, dest.Count);
            for (var i = 0; i < dest.Count; i++)
            {
                Assert.AreNotSame(dest[i].Tower, catalog.TowerPool.Towers[0]);
                Assert.AreNotSame(dest[i].Tower, catalog.TowerPool.Towers[1]);
            }
        }

        [Test]
        public void Campaign_RosterFull_OnlyOwnedTowersAppear()
        {
            var catalog = MakeCatalog(DraftMixKind.TwoGemsOneTowerContested, gems: 6, towers: 12);
            var roster = new TowerRoster();
            for (var i = 0; i < TowerRoster.MaxSlots; i++)
                roster.ApplyPick(catalog.TowerPool.Towers[i]);

            var dest = new List<DraftOfferCard>(4);
            for (var seed = 1; seed <= 40; seed++)
            {
                DraftOfferSampler.Fill(
                    dest, catalog, new System.Random(seed), shuffle: false, null, null, roster);
                Assert.AreEqual(4, dest.Count);
                for (var i = 0; i < dest.Count; i++)
                {
                    if (!dest[i].IsTower)
                        continue;
                    Assert.IsTrue(roster.Contains(dest[i].Tower));
                    Assert.AreNotSame(dest[i].Tower, catalog.TowerPool.Towers[10]);
                    Assert.AreNotSame(dest[i].Tower, catalog.TowerPool.Towers[11]);
                }
            }
        }

        [Test]
        public void Campaign_RosterNotFull_NewTowersCanAppear()
        {
            var catalog = MakeCatalog(DraftMixKind.TwoGemsOneTowerContested, gems: 4, towers: 6);
            var roster = new TowerRoster();
            roster.ApplyPick(catalog.TowerPool.Towers[0]);

            var dest = new List<DraftOfferCard>(4);
            var sawNew = false;
            for (var seed = 1; seed <= 40; seed++)
            {
                DraftOfferSampler.Fill(
                    dest, catalog, new System.Random(seed), shuffle: false, null, null, roster);
                for (var i = 0; i < dest.Count; i++)
                {
                    if (dest[i].IsTower && !roster.Contains(dest[i].Tower))
                        sawNew = true;
                }
            }

            Assert.IsTrue(sawNew);
        }

        DraftCatalog MakeCatalog(DraftMixKind mix, int gems, int towers)
        {
            var catalog = ScriptableObject.CreateInstance<DraftCatalog>();
            _destroy.Add(catalog);
            catalog.Mix = mix;

            if (gems > 0)
            {
                var gemPool = ScriptableObject.CreateInstance<DraftPoolCatalog>();
                _destroy.Add(gemPool);
                gemPool.Gems = new GemDefinition[gems];
                for (var i = 0; i < gems; i++)
                {
                    var gem = ScriptableObject.CreateInstance<GemDefinition>();
                    gem.Id = (GemId)(i + 1);
                    gem.DisplayName = "G" + i;
                    gem.DraftWeight = 1f;
                    _destroy.Add(gem);
                    gemPool.Gems[i] = gem;
                }

                catalog.GemPool = gemPool;
            }

            if (towers > 0)
            {
                var towerPool = ScriptableObject.CreateInstance<TowerCatalog>();
                _destroy.Add(towerPool);
                towerPool.Towers = new TowerDefinition[towers];
                for (var i = 0; i < towers; i++)
                {
                    var tower = ScriptableObject.CreateInstance<TowerDefinition>();
                    tower.DisplayName = "T" + i;
                    _destroy.Add(tower);
                    towerPool.Towers[i] = tower;
                }

                catalog.TowerPool = towerPool;
            }

            return catalog;
        }

        GemRarityTable MakeRarityTable(float lesser, float normal, float greater)
        {
            var table = ScriptableObject.CreateInstance<GemRarityTable>();
            table.LesserWeight = lesser;
            table.NormalWeight = normal;
            table.GreaterWeight = greater;
            _destroy.Add(table);
            return table;
        }

        static bool ContainsDefinition(List<GemDefinition> definitions, GemDefinition definition)
        {
            for (var i = 0; i < definitions.Count; i++)
            {
                if (ReferenceEquals(definitions[i], definition))
                    return true;
            }

            return false;
        }
    }
}
