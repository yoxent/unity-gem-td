using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class SocketLockdownTests
    {
        TowerDefinition _def;
        TowerInstance _towerA;
        TowerInstance _towerB;

        [SetUp]
        public void SetUp()
        {
            _def = ScriptableObject.CreateInstance<TowerDefinition>();
            _def.DisplayName = "Test Tower";
            _def.SocketCount = 3;
            _towerA = new TowerInstance(new Vector2Int(0, 0), _def);
            _towerB = new TowerInstance(new Vector2Int(1, 0), _def);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_def);
        }

        [Test]
        public void Plan_AlwaysAllows()
        {
            var lockout = new SocketLockdown(duration: 3f);
            lockout.NotifyChanged(_towerA, RunStateId.Plan);
            Assert.IsTrue(lockout.CanSocket(_towerA, RunStateId.Plan));
            Assert.AreEqual(0f, lockout.Remaining(_towerA), 1e-4f);
        }

        [Test]
        public void Combat_LocksOnlyThatTower_ForThreeSeconds()
        {
            var lockout = new SocketLockdown(3f);
            lockout.NotifyChanged(_towerA, RunStateId.Combat);
            Assert.IsFalse(lockout.CanSocket(_towerA, RunStateId.Combat));
            Assert.IsTrue(lockout.CanSocket(_towerB, RunStateId.Combat));
            Assert.AreEqual(3f, lockout.Remaining(_towerA), 1e-4f);
            lockout.Tick(3f);
            Assert.IsTrue(lockout.CanSocket(_towerA, RunStateId.Combat));
            Assert.AreEqual(0f, lockout.Remaining(_towerA), 1e-4f);
        }

        [Test]
        public void DurationZero_NeverLocks()
        {
            var lockout = new SocketLockdown(0f);
            lockout.NotifyChanged(_towerA, RunStateId.Combat);
            Assert.IsTrue(lockout.CanSocket(_towerA, RunStateId.Combat));
            Assert.AreEqual(0f, lockout.Remaining(_towerA), 1e-4f);
        }
    }
}
