using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;

namespace GemTD.Tests.EditMode
{
    public sealed class DraftPoolCatalogTests
    {
        [Test]
        public void Count_NullGems_ReturnsZero()
        {
            var catalog = ScriptableObject.CreateInstance<DraftPoolCatalog>();
            Assert.AreEqual(0, catalog.Count);
        }

        [Test]
        public void Count_ReturnsArrayLength()
        {
            var catalog = ScriptableObject.CreateInstance<DraftPoolCatalog>();
            catalog.Gems = new[]
            {
                ScriptableObject.CreateInstance<GemDefinition>(),
                ScriptableObject.CreateInstance<GemDefinition>(),
                ScriptableObject.CreateInstance<GemDefinition>(),
            };
            Assert.AreEqual(3, catalog.Count);
        }

        [Test]
        public void GetGemsOrEmpty_NullOrEmpty_ReturnsEmpty()
        {
            var catalog = ScriptableObject.CreateInstance<DraftPoolCatalog>();
            Assert.AreEqual(0, catalog.GetGemsOrEmpty().Length);

            catalog.Gems = System.Array.Empty<GemDefinition>();
            Assert.AreEqual(0, catalog.GetGemsOrEmpty().Length);
        }

        [Test]
        public void GetGemsOrEmpty_Assigned_ReturnsSameArray()
        {
            var catalog = ScriptableObject.CreateInstance<DraftPoolCatalog>();
            var gems = new[]
            {
                ScriptableObject.CreateInstance<GemDefinition>(),
                ScriptableObject.CreateInstance<GemDefinition>(),
                ScriptableObject.CreateInstance<GemDefinition>(),
            };
            catalog.Gems = gems;
            Assert.AreSame(gems, catalog.GetGemsOrEmpty());
        }

        [Test]
        public void DraftCatalog_RarityTable_IsOptionalReference()
        {
            var catalog = ScriptableObject.CreateInstance<DraftCatalog>();
            var rarityTable = ScriptableObject.CreateInstance<GemRarityTable>();

            Assert.IsNull(catalog.RarityTable);
            catalog.RarityTable = rarityTable;
            Assert.AreSame(rarityTable, catalog.RarityTable);

            Object.DestroyImmediate(rarityTable);
            Object.DestroyImmediate(catalog);
        }
    }
}
