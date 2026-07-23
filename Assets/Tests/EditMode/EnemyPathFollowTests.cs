using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Grid;

namespace GemTD.Tests.EditMode
{
    public sealed class EnemyPathFollowTests
    {
        const float CellSize = 1f;

        EnemyDefinition _def;

        [SetUp]
        public void SetUp()
        {
            _def = ScriptableObject.CreateInstance<EnemyDefinition>();
            _def.MaxHealth = 20f;
            _def.MoveSpeed = 2f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_def);
        }

        [Test]
        public void TickMove_TwoPointPolyline_ReachesExit()
        {
            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(1, 0));
            var enemy = new EnemyRuntime();
            enemy.Init(_def, waypoints);

            Assert.AreEqual(0f, enemy.Progress, 1e-4f);
            Assert.IsTrue(enemy.IsAlive);
            Assert.AreEqual(waypoints[0], enemy.WorldPosition);

            Assert.IsFalse(enemy.TickMove(0.25f));
            Assert.Less(enemy.Progress, 1f);

            Assert.IsTrue(enemy.TickMove(0.25f));
            Assert.AreEqual(1f, enemy.Progress, 1e-4f);
            Assert.AreEqual(waypoints[1], enemy.WorldPosition);
        }

        [Test]
        public void ApplyDamage_ReducesHpAndKills()
        {
            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(1, 0));
            var enemy = new EnemyRuntime();
            enemy.Init(_def, waypoints);

            enemy.ApplyDamage(8f);
            Assert.AreEqual(12f, enemy.Hp, 1e-4f);
            Assert.IsTrue(enemy.IsAlive);

            enemy.ApplyDamage(12f);
            Assert.AreEqual(0f, enemy.Hp, 1e-4f);
            Assert.IsFalse(enemy.IsAlive);
            Assert.IsFalse(enemy.TickMove(1f));
        }

        [Test]
        public void Registry_RegisterUnregisterAndCopyAlive()
        {
            var registry = new EnemyRegistry();
            var a = CreateEnemy();
            var b = CreateEnemy();
            var c = CreateEnemy();

            registry.Register(a);
            registry.Register(b);
            registry.Register(c);
            Assert.AreEqual(3, registry.Count);

            b.ApplyDamage(100f);
            registry.Unregister(a);

            var alive = new List<EnemyRuntime>();
            registry.CopyAlive(alive);
            Assert.AreEqual(1, alive.Count);
            Assert.AreSame(c, alive[0]);

            Assert.AreSame(b, registry.GetAt(0));
            registry.Unregister(b);
            Assert.AreEqual(1, registry.Count);
        }

        EnemyRuntime CreateEnemy()
        {
            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(1, 0));
            var enemy = new EnemyRuntime();
            enemy.Init(_def, waypoints);
            return enemy;
        }

        static List<Vector3> BuildWorldWaypoints(params Vector2Int[] cells)
        {
            var half = CellSize * 0.5f;
            var list = new List<Vector3>(cells.Length);
            for (var i = 0; i < cells.Length; i++)
            {
                var c = cells[i];
                list.Add(new Vector3(c.x * CellSize + half, 0f, c.y * CellSize + half));
            }
            return list;
        }
    }
}
