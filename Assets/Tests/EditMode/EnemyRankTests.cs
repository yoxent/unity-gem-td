using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Enemies;

namespace GemTD.Tests.EditMode
{
    public sealed class EnemyRankTests
    {
        [Test]
        public void MaxAffixes_MatchSpec()
        {
            Assert.AreEqual(0, EnemyRankRules.MaxAffixes(EnemyRank.Normal));
            Assert.AreEqual(1, EnemyRankRules.MaxAffixes(EnemyRank.Elite));
            Assert.AreEqual(2, EnemyRankRules.MaxAffixes(EnemyRank.Commander));
            Assert.AreEqual(4, EnemyRankRules.MaxAffixes(EnemyRank.Boss));
        }

        [Test]
        public void IsBoss_TrueOnlyForBossRank()
        {
            var def = ScriptableObject.CreateInstance<EnemyDefinition>();
            def.Rank = EnemyRank.Normal;
            Assert.IsFalse(def.IsBoss);
            def.Rank = EnemyRank.Elite;
            Assert.IsFalse(def.IsBoss);
            def.Rank = EnemyRank.Commander;
            Assert.IsFalse(def.IsBoss);
            def.Rank = EnemyRank.Boss;
            Assert.IsTrue(def.IsBoss);
            Object.DestroyImmediate(def);
        }
    }
}
