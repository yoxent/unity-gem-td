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
        public void KnockbackAlongPath_RewindsTowardStart_ClampsAtSpawn()
        {
            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(1, 0));
            var enemy = new EnemyRuntime();
            enemy.Init(_def, waypoints);

            Assert.IsFalse(enemy.TickMove(0.25f));
            var mid = enemy.Progress;
            Assert.Greater(mid, 0.4f);
            Assert.Less(mid, 0.6f);

            enemy.KnockbackAlongPath(0.25f);
            Assert.AreEqual(mid - 0.25f, enemy.Progress, 1e-3f);

            enemy.KnockbackAlongPath(10f);
            Assert.AreEqual(0f, enemy.Progress, 1e-4f);
            Assert.AreEqual(waypoints[0], enemy.WorldPosition);
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
        public void Definition_DefaultsToSlideHopParams()
        {
            Assert.AreEqual(LocomotionStyle.Slide, _def.Locomotion);
            Assert.AreEqual(0.35f, _def.HopHeight, 1e-4f);
            Assert.AreEqual(0.4f, _def.HopPeriod, 1e-4f);
            Assert.AreEqual(0.45f, _def.FlyHeight, 1e-4f);
            Assert.AreEqual(1.25f, _def.FlyPeriod, 1e-4f);
        }

        [Test]
        public void TickMove_WhenLocomotionHop_StillReachesExit()
        {
            _def.Locomotion = LocomotionStyle.Hop;
            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(1, 0));
            var enemy = new EnemyRuntime();
            enemy.Init(_def, waypoints);

            Assert.IsFalse(enemy.TickMove(0.25f));
            Assert.IsTrue(enemy.TickMove(0.25f));
            Assert.AreEqual(waypoints[1], enemy.WorldPosition);
        }

        [Test]
        public void TickMove_WhenLocomotionFly_StillReachesExit()
        {
            _def.Locomotion = LocomotionStyle.Fly;
            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(1, 0));
            var enemy = new EnemyRuntime();
            enemy.Init(_def, waypoints);

            Assert.IsFalse(enemy.TickMove(0.25f));
            Assert.IsTrue(enemy.TickMove(0.25f));
            Assert.AreEqual(waypoints[1], enemy.WorldPosition);
        }

        [Test]
        public void Runtime_SnapshotsLocomotionOnInit()
        {
            _def.Locomotion = LocomotionStyle.Hop;
            _def.HopHeight = 0.11f;
            _def.HopPeriod = 0.22f;
            _def.FlyHeight = 0.33f;
            _def.FlyPeriod = 0.44f;

            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(1, 0));
            var enemy = new EnemyRuntime();
            enemy.Init(_def, waypoints);

            // Mutate the ScriptableObject after spawn; the runtime locomotion must not change.
            _def.Locomotion = LocomotionStyle.Slide;
            _def.HopHeight = 1.7f;
            _def.HopPeriod = 3.3f;
            _def.FlyHeight = 2.5f;
            _def.FlyPeriod = 4.5f;

            Assert.AreEqual(LocomotionStyle.Hop, enemy.LocomotionStyle);
            Assert.AreEqual(0.11f, enemy.HopHeight, 1e-4f);
            Assert.AreEqual(0.22f, enemy.HopPeriod, 1e-4f);
            Assert.AreEqual(0.33f, enemy.FlyHeight, 1e-4f);
            Assert.AreEqual(0.44f, enemy.FlyPeriod, 1e-4f);
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

        [Test]
        public void TryGetPositionAfter_DoesNotMutateProgress()
        {
            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(1, 0));
            var enemy = new EnemyRuntime();
            enemy.Init(_def, waypoints);
            Assert.IsFalse(enemy.TickMove(0.25f));
            var progress = enemy.Progress;
            var pos = enemy.WorldPosition;

            Assert.IsTrue(enemy.TryGetPositionAfter(0.25f, out var future));
            Assert.AreEqual(waypoints[1], future);
            Assert.AreEqual(progress, enemy.Progress, 1e-4f);
            Assert.AreEqual(pos, enemy.WorldPosition);
        }

        [Test]
        public void TryGetPositionAfter_NoPath_ReturnsFalseAndCurrentPosition()
        {
            var enemy = new EnemyRuntime();
            enemy.Init(_def, System.Array.Empty<Vector3>());
            Assert.IsFalse(enemy.TryGetPositionAfter(1f, out var point));
            Assert.AreEqual(enemy.WorldPosition, point);
        }

        [Test]
        public void TickMove_Blink_WaitsThenJumpsAlongPath()
        {
            _def.Tags = EnemyTag.Runner | EnemyTag.Blink;
            _def.BlinkDistance = 2f;
            _def.BlinkInterval = 4f;
            _def.MoveSpeed = 2f;
            var waypoints = StraightPath(40);
            var enemy = new EnemyRuntime();
            enemy.Init(_def, waypoints);

            Assert.IsFalse(enemy.TickMove(3.9f));
            Assert.AreEqual(7.8f, enemy.Progress, 1e-3f);

            Assert.IsFalse(enemy.TickMove(0.1f));
            Assert.AreEqual(10f, enemy.Progress, 1e-3f);
        }

        [Test]
        public void TickMove_Blink_RepeatsEachInterval()
        {
            _def.Tags = EnemyTag.Runner | EnemyTag.Blink;
            _def.BlinkDistance = 2f;
            _def.BlinkInterval = 4f;
            _def.MoveSpeed = 2f;
            var enemy = new EnemyRuntime();
            enemy.Init(_def, StraightPath(40));

            Assert.IsFalse(enemy.TickMove(8f));
            Assert.AreEqual(20f, enemy.Progress, 1e-3f);
        }

        [Test]
        public void TickMove_Blink_ReachesExitWhenTheJumpPassesTheEnd()
        {
            _def.Tags = EnemyTag.Runner | EnemyTag.Blink;
            _def.BlinkDistance = 2f;
            _def.BlinkInterval = 4f;
            _def.MoveSpeed = 2f;
            var enemy = new EnemyRuntime();
            enemy.Init(_def, StraightPath(9));

            Assert.IsTrue(enemy.TickMove(4f));
            Assert.AreEqual(9f, enemy.Progress, 1e-3f);
        }

        [Test]
        public void TickMove_BlinkTag_NonPositiveDistanceUsesDefault()
        {
            _def.Tags = EnemyTag.Blink;
            _def.BlinkDistance = -1f;
            _def.BlinkInterval = 0f;
            _def.MoveSpeed = 2f;
            var enemy = new EnemyRuntime();
            enemy.Init(_def, StraightPath(40));

            Assert.IsFalse(enemy.TickMove(EnemyDefinition.DefaultBlinkInterval));
            var walked = _def.MoveSpeed * EnemyDefinition.DefaultBlinkInterval;
            Assert.AreEqual(walked + EnemyDefinition.DefaultBlinkDistance, enemy.Progress, 1e-3f);
        }

        [Test]
        public void TickMove_BlinkOff_IgnoresDistanceAndInterval()
        {
            _def.Tags = EnemyTag.Runner;
            _def.BlinkDistance = 2f;
            _def.BlinkInterval = 4f;
            _def.MoveSpeed = 2f;
            var enemy = new EnemyRuntime();
            enemy.Init(_def, StraightPath(40));

            Assert.IsFalse(enemy.TickMove(4f));
            Assert.AreEqual(8f, enemy.Progress, 1e-3f);
        }

        [Test]
        public void TryGetPositionAfter_Blink_MatchesSteppedMove()
        {
            _def.Tags = EnemyTag.Runner | EnemyTag.Blink;
            _def.BlinkDistance = 2f;
            _def.BlinkInterval = 4f;
            _def.MoveSpeed = 2f;
            var waypoints = StraightPath(40);
            var stepped = new EnemyRuntime();
            stepped.Init(_def, waypoints);
            var predicted = new EnemyRuntime();
            predicted.Init(_def, waypoints);

            const float total = 4.25f;
            const float step = 0.05f;
            var t = 0f;
            while (t < total - 1e-4f)
            {
                var dt = total - t < step ? total - t : step;
                stepped.TickMove(dt);
                t += dt;
            }

            Assert.IsTrue(predicted.TryGetPositionAfter(total, out var point));
            Assert.AreEqual(stepped.WorldPosition.x, point.x, 1e-2f);
            Assert.AreEqual(stepped.WorldPosition.z, point.z, 1e-2f);
            Assert.AreEqual(0f, predicted.Progress, 1e-4f);
        }

        [Test]
        public void TryGetPathTangent_Eastbound_FacesPlusX()
        {
            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(1, 0));
            var enemy = new EnemyRuntime();
            enemy.Init(_def, waypoints);

            Assert.IsTrue(enemy.TryGetPathTangent(out var tangent));
            Assert.AreEqual(1f, tangent.x, 1e-4f);
            Assert.AreEqual(0f, tangent.y, 1e-4f);
            Assert.AreEqual(0f, tangent.z, 1e-4f);
        }

        [Test]
        public void TryGetPathTangent_AfterCorner_FacesNewSegment()
        {
            var waypoints = BuildWorldWaypoints(
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(1, 1));
            var enemy = new EnemyRuntime();
            enemy.Init(_def, waypoints);

            Assert.IsFalse(enemy.TickMove(0.5f));
            Assert.IsTrue(enemy.TryGetPathTangent(out var tangent));
            Assert.AreEqual(0f, tangent.x, 1e-4f);
            Assert.AreEqual(1f, tangent.z, 1e-4f);
        }

        EnemyRuntime CreateEnemy()
        {
            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(1, 0));
            var enemy = new EnemyRuntime();
            enemy.Init(_def, waypoints);
            return enemy;
        }

        static List<Vector3> StraightPath(int cells)
        {
            var list = new List<Vector3>(cells + 1);
            var half = CellSize * 0.5f;
            for (var i = 0; i <= cells; i++)
                list.Add(new Vector3(i * CellSize + half, 0f, half));
            return list;
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
