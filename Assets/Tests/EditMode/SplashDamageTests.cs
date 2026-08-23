using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class SplashDamageTests
    {
        const float CellSize = 1f;

        EnemyDefinition _enemyDef;
        TowerDefinition _splashTower;
        AttackRoleDefinition _splashRole;
        GemModifierPipeline _pipeline;

        [SetUp]
        public void SetUp()
        {
            _enemyDef = ScriptableObject.CreateInstance<EnemyDefinition>();
            _enemyDef.MaxHealth = 100f;
            _enemyDef.MoveSpeed = 0.01f;

            _splashTower = ScriptableObject.CreateInstance<TowerDefinition>();
            _splashRole = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            _splashRole.TowerRadius = 20f;
            _splashRole.Modifiers = new[]
            {
                new RoleStatModifier
                {
                    Stat = RoleStat.SplashRadius,
                    Operation = RoleModifierOperation.Set,
                    Value = 1.5f
                }
            };
            _splashTower.Roles = new TowerRoleDefinition[] { _splashRole };
            _splashTower.Tags = GemTag.Attack | GemTag.Projectile | GemTag.Aoe;
            _splashTower.Damage = 8f;
            _splashRole.AttackTime = 1.2f;
            _splashTower.SocketCount = 2;

            _pipeline = new GemModifierPipeline();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemyDef);
            Object.DestroyImmediate(_splashRole);
            Object.DestroyImmediate(_splashTower);
        }

        [Test]
        public void SplashTower_DamagesPrimaryAndNearbyEnemy()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 200f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _splashTower);

            // Same progress; nearby is off the flight line so ballistic hits primary first.
            // Lateral 1.0 is inside SplashRadius 1.5.
            var primary = CreateEnemyAtProgress(0.2f);
            var nearby = CreateEnemyAtProgress(0.2f, laneOffsetZ: 1f);
            var registry = new EnemyRegistry();
            registry.Register(primary);
            registry.Register(nearby);

            var primaryHpBefore = primary.Hp;
            var nearbyHpBefore = nearby.Hp;

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);
            Assert.AreEqual(1, director.Projectiles.Count);

            for (var i = 0; i < 60; i++)
                director.Tick(0.05f, new List<TowerInstance>(), registry, _pipeline);

            Assert.Less(primary.Hp, primaryHpBefore);
            Assert.Less(nearby.Hp, nearbyHpBefore);
            Assert.AreEqual(8f, primaryHpBefore - primary.Hp, 1e-4f);
            Assert.AreEqual(8f, nearbyHpBefore - nearby.Hp, 1e-4f);
        }

        [Test]
        public void NoSplash_WhenSplashRadiusZero_OnlyPrimaryDamaged()
        {
            _splashTower.Tags = GemTag.Attack | GemTag.Projectile;
            _splashRole.Modifiers = null;

            var director = new CombatDirector(CellSize, projectileSpeed: 200f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _splashTower);

            var primary = CreateEnemyAtProgress(0.2f);
            var nearby = CreateEnemyAtProgress(0.2f, laneOffsetZ: 1f);
            var registry = new EnemyRegistry();
            registry.Register(primary);
            registry.Register(nearby);

            var nearbyHpBefore = nearby.Hp;

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);

            for (var i = 0; i < 60; i++)
                director.Tick(0.05f, new List<TowerInstance>(), registry, _pipeline);

            Assert.Less(primary.Hp, 100f);
            Assert.AreEqual(nearbyHpBefore, nearby.Hp, 1e-4f);
        }

        EnemyRuntime CreateEnemyAtProgress(float approximateProgress, float laneOffsetZ = 0f)
        {
            var waypoints = BuildWorldWaypoints(laneOffsetZ, new Vector2Int(0, 0), new Vector2Int(10, 0));
            var enemy = new EnemyRuntime();
            enemy.Init(_enemyDef, waypoints);
            _enemyDef.MoveSpeed = 1f;
            var steps = Mathf.Max(1, Mathf.RoundToInt(approximateProgress * 10f / 0.05f));
            for (var i = 0; i < steps; i++)
                enemy.TickMove(0.05f);
            _enemyDef.MoveSpeed = 0.01f;
            return enemy;
        }

        static List<Vector3> BuildWorldWaypoints(float laneOffsetZ, params Vector2Int[] cells)
        {
            var half = CellSize * 0.5f;
            var list = new List<Vector3>(cells.Length);
            for (var i = 0; i < cells.Length; i++)
            {
                var c = cells[i];
                list.Add(new Vector3(c.x * CellSize + half, 0f, c.y * CellSize + half + laneOffsetZ));
            }
            return list;
        }
    }
}
