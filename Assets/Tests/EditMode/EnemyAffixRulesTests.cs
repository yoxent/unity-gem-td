using NUnit.Framework;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;

namespace GemTD.Tests.EditMode
{
    public sealed class EnemyAffixRulesTests
    {
        [Test]
        public void Normal_RejectsAnyAffix()
        {
            var affixes = new[] { EnemyAffix.Swift };
            Assert.IsFalse(EnemyAffixRules.TryValidate(EnemyRank.Normal, affixes, out _));
        }

        [Test]
        public void Elite_AllowsOne()
        {
            var affixes = new[] { EnemyAffix.Swift };
            Assert.IsTrue(EnemyAffixRules.TryValidate(EnemyRank.Elite, affixes, out _));
        }

        [Test]
        public void Elite_RejectsTwo()
        {
            var affixes = new[] { EnemyAffix.Swift, EnemyAffix.Armored };
            Assert.IsFalse(EnemyAffixRules.TryValidate(EnemyRank.Elite, affixes, out _));
        }

        [Test]
        public void Duplicate_IsInvalid()
        {
            var affixes = new[] { EnemyAffix.Swift, EnemyAffix.Swift };
            Assert.IsFalse(EnemyAffixRules.TryValidate(EnemyRank.Commander, affixes, out _));
        }

        [Test]
        public void CurseEffectiveness_Hexproof_IsZero()
        {
            Assert.AreEqual(0f, EnemyAffixRules.CurseEffectiveness(new[] { EnemyAffix.Hexproof }));
        }

        [Test]
        public void CurseEffectiveness_Unhallowed_IsPointSix()
        {
            Assert.AreEqual(
                DamageTypeCombat.UnhallowedCurseEffectiveness,
                EnemyAffixRules.CurseEffectiveness(new[] { EnemyAffix.Unhallowed }));
        }

        [Test]
        public void CurseEffectiveness_HexproofWinsOverUnhallowed()
        {
            Assert.AreEqual(
                0f,
                EnemyAffixRules.CurseEffectiveness(new[] { EnemyAffix.Unhallowed, EnemyAffix.Hexproof }));
        }

        [Test]
        public void CurseEffectiveness_Empty_IsOne()
        {
            Assert.AreEqual(1f, EnemyAffixRules.CurseEffectiveness(null));
        }
    }
}
