using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class CombatDirectorTests
    {
        const float CellSize = 1f;

        EnemyDefinition _enemyDef;
        TowerDefinition _towerDef;
        GemDefinition _lmp;
        GemDefinition _chain;
        GemModifierPipeline _pipeline;

        [SetUp]
        public void SetUp()
        {
            _enemyDef = ScriptableObject.CreateInstance<EnemyDefinition>();
            _enemyDef.MaxHealth = 100f;
            _enemyDef.MoveSpeed = 0.01f;

            _towerDef = ScriptableObject.CreateInstance<TowerDefinition>();
            _towerDef.Range = 20f;
            _towerDef.Damage = 10f;
            _towerDef.AttackInterval = 1f;
            _towerDef.SocketCount = 2;

            _lmp = ScriptableObject.CreateInstance<GemDefinition>();
            _lmp.Id = GemId.Lmp;

            _chain = ScriptableObject.CreateInstance<GemDefinition>();
            _chain.Id = GemId.Chain;

            _pipeline = new GemModifierPipeline();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemyDef);
            Object.DestroyImmediate(_towerDef);
            Object.DestroyImmediate(_lmp);
            Object.DestroyImmediate(_chain);
        }

        [Test]
        public void Tick_NoGems_SpawnsOneProjectileAtPrimary()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerRuntime(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            director.Tick(0.016f, new List<TowerRuntime> { tower }, registry, _pipeline);

            Assert.AreEqual(1, director.Projectiles.Count);
            Assert.AreSame(enemy, director.Projectiles[0].Target);
            Assert.AreEqual(10f, director.Projectiles[0].Damage, 1e-4f);
            Assert.Greater(tower.Cooldown, 0f);
        }

        [Test]
        public void Tick_WithLmp_SpawnsProjectileCountSamePrimary()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerRuntime(new Vector2Int(0, 0), _towerDef);
            Assert.IsTrue(tower.TrySocket(_lmp, 0, allowSocket: true));

            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            director.Tick(0.016f, new List<TowerRuntime> { tower }, registry, _pipeline);

            Assert.AreEqual(3, director.Projectiles.Count);
            for (var i = 0; i < director.Projectiles.Count; i++)
                Assert.AreSame(enemy, director.Projectiles[i].Target);
        }

        [Test]
        public void Projectile_OnHit_AppliesDamage_AndChainsToNearestOther()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 200f);
            var tower = new TowerRuntime(new Vector2Int(0, 0), _towerDef);
            Assert.IsTrue(tower.TrySocket(_chain, 0, allowSocket: true));

            var primary = CreateEnemyAtProgress(0.2f);
            var secondary = CreateEnemyAtProgress(0.15f);
            var registry = new EnemyRegistry();
            registry.Register(primary);
            registry.Register(secondary);

            director.Tick(0.016f, new List<TowerRuntime> { tower }, registry, _pipeline);
            Assert.AreEqual(1, director.Projectiles.Count);
            Assert.Greater(director.Projectiles[0].ChainRemaining, 0);

            // Drive projectile until first hit + bounce
            for (var i = 0; i < 60; i++)
                director.Tick(0.05f, new List<TowerRuntime>(), registry, _pipeline);

            Assert.Less(primary.Hp, 100f);
            Assert.Less(secondary.Hp, 100f);
        }

        [Test]
        public void Projectile_ChainNoOp_WhenNoOtherLiving()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 200f);
            var tower = new TowerRuntime(new Vector2Int(0, 0), _towerDef);
            Assert.IsTrue(tower.TrySocket(_chain, 0, allowSocket: true));

            var only = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(only);

            director.Tick(0.016f, new List<TowerRuntime> { tower }, registry, _pipeline);

            for (var i = 0; i < 60; i++)
                director.Tick(0.05f, new List<TowerRuntime>(), registry, _pipeline);

            Assert.Less(only.Hp, 100f);
            Assert.AreEqual(0, director.Projectiles.Count);
        }

        EnemyRuntime CreateEnemyNearTower()
        {
            return CreateEnemyAtProgress(0.1f);
        }

        EnemyRuntime CreateEnemyAtProgress(float approximateProgress)
        {
            var waypoints = BuildWorldWaypoints(new Vector2Int(0, 0), new Vector2Int(10, 0));
            var enemy = new EnemyRuntime();
            enemy.Init(_enemyDef, waypoints);
            // Path length 10; MoveSpeed ~0 so nudge via many tiny ticks of higher temp speed
            var steps = Mathf.Max(1, Mathf.RoundToInt(approximateProgress * 10f / 0.05f));
            _enemyDef.MoveSpeed = 1f;
            for (var i = 0; i < steps; i++)
                enemy.TickMove(0.05f);
            _enemyDef.MoveSpeed = 0.01f;
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
