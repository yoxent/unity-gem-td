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
