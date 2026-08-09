using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class ProjectileRuntimeTests
    {
        const float CellSize = 1f;

        EnemyDefinition _def;

        [SetUp]
        public void SetUp()
        {
            _def = ScriptableObject.CreateInstance<EnemyDefinition>();
            _def.Armor = 0;
            _def.MaxHealth = 100f;
            _def.MoveSpeed = 0.01f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_def);
        }

        [Test]
        public void Chain_HopDamages_FallOffByZeroPointSix()
        {
            // Place e3 nearer to e2 than e1 so hop-2 bounce is unambiguous.
            var e1 = MakeEnemyAt(Vector3.zero, 100f);
            var e2 = MakeEnemyAt(new Vector3(1f, 0f, 0f), 100f);
            var e3 = MakeEnemyAt(new Vector3(1.4f, 0f, 0f), 100f);
            var living = Living(e1, e2, e3);

            var projectile = new ProjectileRuntime();
            projectile.Init(
                origin: Vector3.zero,
                direction: Vector3.right,
                target: e1,
                damage: 10f,
                chainCount: 2,
                speed: 100f,
                chainRange: 5f);

            Assert.IsTrue(projectile.Tick(0.05f, living));
            Assert.AreEqual(90f, e1.Hp, 1e-3f);
            Assert.AreEqual(6f, projectile.Damage, 1e-3f);
            Assert.AreSame(e2, projectile.Target);

            Assert.IsTrue(projectile.Tick(0.05f, living));
            Assert.AreEqual(94f, e2.Hp, 1e-3f);
            Assert.AreEqual(3.6f, projectile.Damage, 1e-3f);
            Assert.AreSame(e3, projectile.Target);

            Assert.IsFalse(projectile.Tick(0.05f, living));
            Assert.AreEqual(96.4f, e3.Hp, 1e-3f);
            Assert.IsFalse(projectile.IsActive);
        }

        [Test]
        public void Shotgun_EachPellet_FullDamage_Regression()
        {
            var towerDef = ScriptableObject.CreateInstance<TowerDefinition>();
            towerDef.Range = 20f;
            towerDef.Damage = 10f;
            towerDef.AttackInterval = 1f;
            towerDef.SocketCount = 2;

            var lmp = ScriptableObject.CreateInstance<GemDefinition>();
            lmp.Id = GemId.Lmp;

            try
            {
                var director = new CombatDirector(CellSize, projectileSpeed: 100f);
                var tower = new TowerRuntime(new Vector2Int(0, 0), towerDef);
                Assert.IsTrue(tower.TrySocket(lmp, 0, allowSocket: true));

                var enemy = MakeEnemyAt(new Vector3(1.5f, 0f, 0.5f), 100f);
                var registry = new EnemyRegistry();
                registry.Register(enemy);
                var pipeline = new GemModifierPipeline();

                director.Tick(0.016f, new List<TowerRuntime> { tower }, registry, pipeline);

                Assert.AreEqual(3, director.Projectiles.Count);
                // LMP post-mod damage is base*0.8; each pellet deals that full amount (not split).
                // Soft-seek keeps fan aim visible while still targeting the primary.
                for (var i = 0; i < director.Projectiles.Count; i++)
                {
                    Assert.AreSame(enemy, director.Projectiles[i].Target);
                    Assert.IsTrue(director.Projectiles[i].Seeking);
                    Assert.IsTrue(director.Projectiles[i].SoftSeek);
                    Assert.AreEqual(8f, director.Projectiles[i].Damage, 1e-4f);
                }

                Assert.Greater(
                    Vector3.Angle(director.Projectiles[0].Direction, director.Projectiles[2].Direction),
                    1f);
            }
            finally
            {
                Object.DestroyImmediate(towerDef);
                Object.DestroyImmediate(lmp);
            }
        }

        [Test]
        public void Fork_OnHit_SpawnsTwoChildrenAtPlusMinus45()
        {
            var enemy = MakeEnemyAt(Vector3.zero, 100f);
            var living = Living(enemy);
            var spawnBuffer = new List<ProjectileRuntime>();

            var inbound = Vector3.forward;
            var projectile = new ProjectileRuntime();
            projectile.Init(
                origin: Vector3.zero,
                direction: inbound,
                target: enemy,
                damage: 10f,
                chainCount: 0,
                speed: 100f,
                chainRange: 0f,
                aoeRadius: 0f,
                pierceRemaining: 0,
                forkRemaining: 1,
                spawnBuffer: spawnBuffer);

            projectile.Tick(0.05f, living);

            Assert.AreEqual(2, spawnBuffer.Count);
            Assert.AreEqual(0, projectile.ForkRemaining);
            Assert.IsFalse(projectile.IsActive);

            var expectedPlus = Quaternion.Euler(0f, 45f, 0f) * inbound;
            var expectedMinus = Quaternion.Euler(0f, -45f, 0f) * inbound;
            Assert.AreEqual(0f, Vector3.Angle(expectedPlus, spawnBuffer[0].Direction), 0.1f);
            Assert.AreEqual(0f, Vector3.Angle(expectedMinus, spawnBuffer[1].Direction), 0.1f);
            Assert.AreEqual(0, spawnBuffer[0].ForkRemaining);
            Assert.AreEqual(0, spawnBuffer[1].ForkRemaining);
            Assert.AreEqual(10f, spawnBuffer[0].Damage, 1e-4f);
            Assert.AreEqual(10f, spawnBuffer[1].Damage, 1e-4f);
        }

        [Test]
        public void Fork_BeforeChain_OnSameCollision_OnlyForks()
        {
            var hit = MakeEnemyAt(Vector3.zero, 100f);
            var other = MakeEnemyAt(new Vector3(1f, 0f, 0f), 100f);
            var living = Living(hit, other);
            var spawnBuffer = new List<ProjectileRuntime>();

            var projectile = new ProjectileRuntime();
            projectile.Init(
                origin: Vector3.zero,
                direction: Vector3.forward,
                target: hit,
                damage: 10f,
                chainCount: 2,
                speed: 100f,
                chainRange: 5f,
                forkRemaining: 1,
                spawnBuffer: spawnBuffer);

            projectile.Tick(0.05f, living);

            Assert.IsFalse(projectile.IsActive);
            Assert.AreEqual(2, spawnBuffer.Count);
            Assert.AreEqual(2, spawnBuffer[0].ChainRemaining);
            Assert.AreEqual(2, spawnBuffer[1].ChainRemaining);
            // Parent did not chain on this collision — still parked on the first target conceptually.
            Assert.AreSame(hit, projectile.Target);
            Assert.AreEqual(100f, other.Hp, 1e-3f); // other untouched until children travel
        }

        [Test]
        public void Pierce_BeforeFork_OnSameCollision_ContinuesWithoutFork()
        {
            var hit = MakeEnemyAt(Vector3.zero, 100f);
            var living = Living(hit);
            var spawnBuffer = new List<ProjectileRuntime>();

            var projectile = new ProjectileRuntime();
            projectile.Init(
                origin: Vector3.zero,
                direction: Vector3.right,
                target: hit,
                damage: 10f,
                chainCount: 0,
                speed: 100f,
                chainRange: 0f,
                pierceRemaining: 2,
                forkRemaining: 1,
                spawnBuffer: spawnBuffer);

            Assert.IsTrue(projectile.Tick(0.05f, living));
            Assert.IsTrue(projectile.IsActive);
            Assert.IsFalse(projectile.Seeking);
            Assert.AreEqual(0, spawnBuffer.Count);
            Assert.AreEqual(1, projectile.PierceRemaining);
            Assert.AreEqual(1, projectile.ForkRemaining);
        }

        [Test]
        public void Pierce_ContinuesAndHitsSecondEnemy()
        {
            var first = MakeEnemyAt(Vector3.zero, 100f);
            var second = MakeEnemyAt(new Vector3(2f, 0f, 0f), 100f);
            var living = Living(first, second);

            var projectile = new ProjectileRuntime();
            projectile.Init(
                origin: new Vector3(-0.5f, 0f, 0f),
                direction: Vector3.right,
                target: first,
                damage: 10f,
                chainCount: 0,
                speed: 50f,
                chainRange: 0f,
                aoeRadius: 0f,
                pierceRemaining: 8);

            Assert.IsTrue(projectile.Tick(0.05f, living));
            Assert.AreEqual(90f, first.Hp, 1e-3f);
            Assert.IsTrue(projectile.IsActive);
            Assert.IsFalse(projectile.Seeking);
            Assert.IsNull(projectile.Target);
            Assert.AreEqual(7, projectile.PierceRemaining);

            for (var i = 0; i < 20 && projectile.IsActive && second.Hp >= 100f; i++)
                projectile.Tick(0.05f, living);

            Assert.AreEqual(90f, second.Hp, 1e-3f);
        }

        EnemyRuntime MakeEnemyAt(Vector3 position, float hp)
        {
            _def.MaxHealth = hp;
            var waypoints = new List<Vector3> { position, position + Vector3.right };
            var enemy = new EnemyRuntime();
            enemy.Init(_def, waypoints);
            return enemy;
        }

        static List<EnemyRuntime> Living(params EnemyRuntime[] enemies)
        {
            var list = new List<EnemyRuntime>(enemies.Length);
            for (var i = 0; i < enemies.Length; i++)
                list.Add(enemies[i]);
            return list;
        }
    }
}
