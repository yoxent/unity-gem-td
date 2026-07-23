using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;

namespace GemTD.Tests.EditMode
{
    public sealed class TargetSelectorTests
    {
        const float CellSize = 1f;

        EnemyDefinition _def;
        TargetSelector _selector;

        [SetUp]
        public void SetUp()
        {
            _def = ScriptableObject.CreateInstance<EnemyDefinition>();
            _def.MaxHealth = 50f;
            _def.MoveSpeed = 2f;
            _selector = new TargetSelector();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_def);
        }

        [Test]
        public void TrySelectFirst_PicksHighestProgressAmongLivingInRange()
        {
            var waypoints = BuildWorldWaypoints(
                new Vector2Int(0, 0),
                new Vector2Int(10, 0));

            var near = new EnemyRuntime();
            near.Init(_def, waypoints);
            near.TickMove(0.5f); // progress ~0.1

            var far = new EnemyRuntime();
            far.Init(_def, waypoints);
            far.TickMove(2.5f); // progress ~0.5

            var towerPos = CellCenter(0, 0);
            var candidates = new List<EnemyRuntime> { near, far };

            Assert.IsTrue(_selector.TrySelectFirst(towerPos, range: 20f, candidates, out var target));
            Assert.AreSame(far, target);
            Assert.Greater(far.Progress, near.Progress);
        }

        [Test]
        public void TrySelectFirst_IgnoresDeadAndOutOfRange()
        {
            var waypoints = BuildWorldWaypoints(
                new Vector2Int(0, 0),
                new Vector2Int(10, 0));

            var deadLeader = new EnemyRuntime();
            deadLeader.Init(_def, waypoints);
            deadLeader.TickMove(4f);
            deadLeader.ApplyDamage(100f);

            var outOfRange = new EnemyRuntime();
            outOfRange.Init(_def, waypoints);
            outOfRange.TickMove(8f);

            var inRange = new EnemyRuntime();
            inRange.Init(_def, waypoints);
            inRange.TickMove(1f);

            var towerPos = CellCenter(0, 0);
            var candidates = new List<EnemyRuntime> { deadLeader, outOfRange, inRange };

            Assert.IsTrue(_selector.TrySelectFirst(towerPos, range: 3f, candidates, out var target));
            Assert.AreSame(inRange, target);
        }

        [Test]
        public void TrySelectFirst_ReturnsFalseWhenNoValidTarget()
        {
            var towerPos = CellCenter(0, 0);
            Assert.IsFalse(_selector.TrySelectFirst(towerPos, range: 5f, new List<EnemyRuntime>(), out var target));
            Assert.IsNull(target);
        }

        [Test]
        public void TrySelect_Last_PicksLowestProgressAmongLivingInRange()
        {
            var waypoints = BuildWorldWaypoints(
                new Vector2Int(0, 0),
                new Vector2Int(10, 0));

            var near = new EnemyRuntime();
            near.Init(_def, waypoints);
            near.TickMove(0.5f);

            var far = new EnemyRuntime();
            far.Init(_def, waypoints);
            far.TickMove(2.5f);

            var towerPos = CellCenter(0, 0);
            var candidates = new List<EnemyRuntime> { near, far };

            Assert.IsTrue(_selector.TrySelect(TargetingMode.Last, towerPos, range: 20f, candidates, out var target));
            Assert.AreSame(near, target);
            Assert.Less(near.Progress, far.Progress);
        }

        [Test]
        public void TrySelect_Closest_PicksNearestEnemyInRange()
        {
            var waypointsNear = BuildWorldWaypoints(
                new Vector2Int(1, 0),
                new Vector2Int(10, 0));
            var waypointsFar = BuildWorldWaypoints(
                new Vector2Int(5, 0),
                new Vector2Int(10, 0));

            var closest = new EnemyRuntime();
            closest.Init(_def, waypointsNear);

            var distant = new EnemyRuntime();
            distant.Init(_def, waypointsFar);

            var towerPos = CellCenter(0, 0);
            var candidates = new List<EnemyRuntime> { distant, closest };

            Assert.IsTrue(_selector.TrySelect(TargetingMode.Closest, towerPos, range: 20f, candidates, out var target));
            Assert.AreSame(closest, target);
        }

        [Test]
        public void TrySelect_Strongest_PicksHighestHp()
        {
            var waypoints = BuildWorldWaypoints(
                new Vector2Int(0, 0),
                new Vector2Int(10, 0));

            var weak = new EnemyRuntime();
            weak.Init(_def, waypoints);
            weak.ApplyDamage(25f);

            var strong = new EnemyRuntime();
            strong.Init(_def, waypoints);

            var towerPos = CellCenter(0, 0);
            var candidates = new List<EnemyRuntime> { weak, strong };

            Assert.IsTrue(_selector.TrySelect(TargetingMode.Strongest, towerPos, range: 20f, candidates, out var target));
            Assert.AreSame(strong, target);
            Assert.Greater(strong.Hp, weak.Hp);
        }

        [Test]
        public void TrySelect_Strongest_TieBreaksOnHigherProgress()
        {
            var waypoints = BuildWorldWaypoints(
                new Vector2Int(0, 0),
                new Vector2Int(10, 0));

            var lowProgress = new EnemyRuntime();
            lowProgress.Init(_def, waypoints);
            lowProgress.TickMove(0.5f);

            var highProgress = new EnemyRuntime();
            highProgress.Init(_def, waypoints);
            highProgress.TickMove(2.5f);

            var towerPos = CellCenter(0, 0);
            var candidates = new List<EnemyRuntime> { lowProgress, highProgress };

            Assert.IsTrue(_selector.TrySelect(TargetingMode.Strongest, towerPos, range: 20f, candidates, out var target));
            Assert.AreSame(highProgress, target);
            Assert.AreEqual(lowProgress.Hp, highProgress.Hp);
            Assert.Greater(highProgress.Progress, lowProgress.Progress);
        }

        static Vector3 CellCenter(int x, int y)
        {
            var half = CellSize * 0.5f;
            return new Vector3(x * CellSize + half, 0f, y * CellSize + half);
        }

        static List<Vector3> BuildWorldWaypoints(params Vector2Int[] cells)
        {
            var list = new List<Vector3>(cells.Length);
            for (var i = 0; i < cells.Length; i++)
                list.Add(CellCenter(cells[i].x, cells[i].y));
            return list;
        }
    }
}
