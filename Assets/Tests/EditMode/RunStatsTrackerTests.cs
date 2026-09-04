using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class RunStatsTrackerTests
    {
        TowerDefinition _singleTarget;
        TowerDefinition _splash;
        TowerDefinition[] _catalog;
        RunStatsTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _singleTarget = ScriptableObject.CreateInstance<TowerDefinition>();
            _singleTarget.DisplayName = "Single Target";

            _splash = ScriptableObject.CreateInstance<TowerDefinition>();
            _splash.DisplayName = "Splash";

            _catalog = new[] { _singleTarget, _splash };
            _tracker = new RunStatsTracker();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_singleTarget);
            Object.DestroyImmediate(_splash);
        }

        [Test]
        public void RecordTowerPlaced_TracksPerTypeAndTotal()
        {
            _tracker.RecordTowerPlaced(_singleTarget);
            _tracker.RecordTowerPlaced(_singleTarget);
            _tracker.RecordTowerPlaced(_splash);

            var snapshot = _tracker.Snapshot(5, _catalog);
            Assert.AreEqual(3, snapshot.TotalBuilt);
            Assert.AreEqual(2, snapshot.TowersByType[0].Built);
            Assert.AreEqual(0.666f, snapshot.TowersByType[0].BuiltPercent, 0.01f);
            Assert.AreEqual(1, snapshot.TowersByType[1].Built);
            Assert.AreEqual(0.333f, snapshot.TowersByType[1].BuiltPercent, 0.01f);
        }

        [Test]
        public void RecordGemSocketed_CountsUniqueGemIds()
        {
            _tracker.RecordGemSocketed(GemId.MultipleProjectiles);
            _tracker.RecordGemSocketed(GemId.Chain);
            _tracker.RecordGemSocketed(GemId.MultipleProjectiles);

            var snapshot = _tracker.Snapshot(1, _catalog);
            Assert.AreEqual(2, snapshot.SkillsCount);
        }

        [Test]
        public void RecordDamage_TracksTotalAndPercentByTowerType()
        {
            _tracker.RecordDamage(_singleTarget, 30f);
            _tracker.RecordDamage(_singleTarget, 20f);
            _tracker.RecordDamage(_splash, 50f);

            var snapshot = _tracker.Snapshot(15, _catalog);
            Assert.AreEqual(100f, snapshot.TotalDamage, 0.001f);
            Assert.AreEqual(50f, snapshot.TowersByType[0].Damage, 0.001f);
            Assert.AreEqual(0.5f, snapshot.TowersByType[0].DamagePercent, 0.001f);
            Assert.AreEqual(50f, snapshot.TowersByType[1].Damage, 0.001f);
            Assert.AreEqual(0.5f, snapshot.TowersByType[1].DamagePercent, 0.001f);
        }

        [Test]
        public void RecordKill_TracksTotalAndPercentByTowerType()
        {
            _tracker.RecordKill(_singleTarget);
            _tracker.RecordKill(_singleTarget);
            _tracker.RecordKill(_singleTarget);
            _tracker.RecordKill(_splash);

            var snapshot = _tracker.Snapshot(8, _catalog);
            Assert.AreEqual(4, snapshot.TotalKills);
            Assert.AreEqual(3, snapshot.TowersByType[0].Kills);
            Assert.AreEqual(0.75f, snapshot.TowersByType[0].KillPercent, 0.001f);
            Assert.AreEqual(1, snapshot.TowersByType[1].Kills);
            Assert.AreEqual(0.25f, snapshot.TowersByType[1].KillPercent, 0.001f);
        }

        [Test]
        public void RecordGoldEarned_TracksTotal()
        {
            _tracker.RecordGoldEarned(5);
            _tracker.RecordGoldEarned(25);
            _tracker.RecordGoldEarned(0);
            _tracker.RecordGoldEarned(-3);

            var snapshot = _tracker.Snapshot(3, _catalog);
            Assert.AreEqual(30, snapshot.TotalGoldEarned);
        }

        [Test]
        public void Reset_ClearsAllStats()
        {
            _tracker.RecordTowerPlaced(_singleTarget);
            _tracker.RecordGemSocketed(GemId.Fork);
            _tracker.RecordDamage(_singleTarget, 10f);
            _tracker.RecordKill(_singleTarget);
            _tracker.RecordGoldEarned(40);

            _tracker.Reset();

            var snapshot = _tracker.Snapshot(0, _catalog);
            Assert.AreEqual(0, snapshot.TotalBuilt);
            Assert.AreEqual(0, snapshot.TotalDamage);
            Assert.AreEqual(0, snapshot.TotalKills);
            Assert.AreEqual(0, snapshot.TotalGoldEarned);
            Assert.AreEqual(0, snapshot.SkillsCount);
            Assert.AreEqual(0, snapshot.TowersByType[0].Damage);
            Assert.AreEqual(0, snapshot.TowersByType[0].Kills);
            Assert.AreEqual(0, snapshot.TowersByType[0].Built);
        }
    }
}
