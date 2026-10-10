using NUnit.Framework;
using GemTD.Gameplay.Enemies;

namespace GemTD.Tests.EditMode
{
    public sealed class EnemySplitTests
    {
        [Test]
        public void EliteSplitting_ToNormal_IsEmpty()
        {
            var parent = new[] { EnemyAffix.Splitting };
            var child = EnemySplit.BuildChildAffixes(parent, EnemyRank.Normal, _ => 0);
            Assert.AreEqual(0, child.Length);
        }

        [Test]
        public void CommanderSplittingPlusSwift_ToElite_KeepsSwift()
        {
            var parent = new[] { EnemyAffix.Splitting, EnemyAffix.Swift };
            var child = EnemySplit.BuildChildAffixes(parent, EnemyRank.Elite, _ => 0);
            Assert.AreEqual(1, child.Length);
            Assert.AreEqual(EnemyAffix.Swift, child[0]);
        }

        [Test]
        public void BossFourAffixes_ToCommander_DropsOneRandomAfterSplitting()
        {
            var parent = new[]
            {
                EnemyAffix.Splitting,
                EnemyAffix.Armored,
                EnemyAffix.Swift,
                EnemyAffix.Hulking
            };
            var child = EnemySplit.BuildChildAffixes(parent, EnemyRank.Commander, remaining => 0);
            Assert.AreEqual(2, child.Length);
            Assert.IsFalse(EnemyAffixRules.Contains(child, EnemyAffix.Splitting));
        }
    }
}
