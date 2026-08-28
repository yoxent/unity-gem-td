using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;

namespace GemTD.Tests.EditMode
{
    public sealed class GemRarityTests
    {
        GemDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<GemDefinition>();
            _definition.DisplayName = "Chain";
            _definition.Id = GemId.Chain;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_definition);
        }

        [Test]
        public void Instance_DisplayNamePrefixesOnlyNonNormalRarities()
        {
            Assert.AreEqual("Lesser Chain", new GemInstance(_definition, GemRarity.Lesser).DisplayName);
            Assert.AreEqual("Chain", new GemInstance(_definition, GemRarity.Normal).DisplayName);
            Assert.AreEqual("Greater Chain", new GemInstance(_definition, GemRarity.Greater).DisplayName);
        }

        [Test]
        public void FromDefinition_UsesNormalAndNullDefinitionIsEmpty()
        {
            var normal = GemInstance.FromDefinition(_definition);
            Assert.IsFalse(normal.IsEmpty);
            Assert.AreEqual(GemRarity.Normal, normal.Rarity);
            Assert.AreEqual(GemId.Chain, normal.Id);
            Assert.IsTrue(GemInstance.FromDefinition(null).IsEmpty);
        }

        [Test]
        public void InvalidRarity_NormalizesToNormal()
        {
            var instance = new GemInstance(_definition, (GemRarity)99);
            Assert.AreEqual(GemRarity.Normal, instance.Rarity);
            Assert.AreEqual("Chain", instance.DisplayName);
        }

        [Test]
        public void RarityTable_ZeroAndOneWeightsCanForceAGivenTier()
        {
            var table = ScriptableObject.CreateInstance<GemRarityTable>();
            table.LesserWeight = 0f;
            table.NormalWeight = 0f;
            table.GreaterWeight = 1f;

            Assert.AreEqual(GemRarity.Greater, table.Roll(new System.Random(123)));
            UnityEngine.Object.DestroyImmediate(table);
        }

        [Test]
        public void RarityTable_AllZeroWeightsFallBackToNormal()
        {
            var table = ScriptableObject.CreateInstance<GemRarityTable>();
            table.LesserWeight = 0f;
            table.NormalWeight = 0f;
            table.GreaterWeight = 0f;

            Assert.AreEqual(GemRarity.Normal, table.Roll(new System.Random(123)));
            UnityEngine.Object.DestroyImmediate(table);
        }
    }
}
