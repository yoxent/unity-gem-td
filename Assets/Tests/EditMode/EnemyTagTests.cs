using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Enemies;

namespace GemTD.Tests.EditMode
{
    public sealed class EnemyTagTests
    {
        EnemyDefinition _def;

        [SetUp]
        public void SetUp()
        {
            _def = ScriptableObject.CreateInstance<EnemyDefinition>();
            _def.DisplayName = "Runner";
            _def.Tags = EnemyTag.Runner;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_def);
        }

        [Test]
        public void Elite_KeepsTheEarliestAuraAndNamesIt()
        {
            _def.Rank = EnemyRank.Elite;
            _def.Tags |= EnemyTag.Hunting | EnemyTag.Shaded;
            _def.EnforceAuraTags();

            Assert.AreEqual("Hunting Runner", _def.PresentedName);
            Assert.IsTrue(EnemyAffixRules.Contains(_def.Affixes, EnemyAffix.Hunting));
            Assert.IsFalse(EnemyAffixRules.Contains(_def.Affixes, EnemyAffix.Shaded));
        }

        [Test]
        public void Commander_KeepsTwoAurasInFixedOrder()
        {
            _def.Rank = EnemyRank.Commander;
            _def.Tags |= EnemyTag.Shaded | EnemyTag.Hunting;
            _def.EnforceAuraTags();

            Assert.AreEqual("Hunting Shaded Runner", _def.PresentedName);
            Assert.IsTrue(EnemyAffixRules.Contains(_def.Affixes, EnemyAffix.Hunting));
            Assert.IsTrue(EnemyAffixRules.Contains(_def.Affixes, EnemyAffix.Shaded));
        }

        [Test]
        public void Normal_DropsAuraTags()
        {
            _def.Rank = EnemyRank.Normal;
            _def.Tags |= EnemyTag.Bloody | EnemyTag.Hunting;
            _def.EnforceAuraTags();

            Assert.IsFalse(EnemyTags.Has(_def.Tags, EnemyTag.Bloody));
            Assert.IsFalse(EnemyTags.Has(_def.Tags, EnemyTag.Hunting));
            Assert.AreEqual("Runner", _def.PresentedName);
        }

        [Test]
        public void SwiftAffix_UsesTheOnlyEliteSlot()
        {
            _def.Rank = EnemyRank.Elite;
            _def.DisplayName = "Swift Runner";
            _def.Affixes = new[] { EnemyAffix.Swift };
            _def.Tags |= EnemyTag.Hunting;
            _def.EnforceAuraTags();

            Assert.AreEqual("Swift Runner", _def.PresentedName);
            Assert.IsFalse(EnemyTags.Has(_def.Tags, EnemyTag.Hunting));
            Assert.IsFalse(EnemyAffixRules.Contains(_def.Affixes, EnemyAffix.Hunting));
        }

        [Test]
        public void EnforceAuraTags_DropsTheAuraPastTheCap()
        {
            _def.Rank = EnemyRank.Elite;
            _def.Tags |= EnemyTag.Bloody | EnemyTag.Hunting;
            _def.EnforceAuraTags();

            Assert.IsTrue(EnemyTags.Has(_def.Tags, EnemyTag.Bloody));
            Assert.IsFalse(EnemyTags.Has(_def.Tags, EnemyTag.Hunting));
            Assert.IsTrue(EnemyAffixRules.Contains(_def.Affixes, EnemyAffix.Bloody));
            Assert.IsFalse(EnemyAffixRules.Contains(_def.Affixes, EnemyAffix.Hunting));
        }
    }
}
