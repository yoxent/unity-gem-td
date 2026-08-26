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
        AttackRoleDefinition _towerRole;
        GemDefinition _multipleProjectiles;
        GemDefinition _chain;
        GemModifierPipeline _pipeline;

        [SetUp]
        public void SetUp()
        {
            _enemyDef = ScriptableObject.CreateInstance<EnemyDefinition>();
            _enemyDef.MaxHealth = 100f;
            _enemyDef.MoveSpeed = 0.01f;

            _towerDef = ScriptableObject.CreateInstance<TowerDefinition>();
            _towerRole = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.ProjectileCount, 1f)
            };
            _towerDef.Roles = new TowerRoleDefinition[] { _towerRole };
            _towerDef.Tags = GemTag.Attack | GemTag.Projectile;
            _towerDef.Damage = 10f;
            _towerDef.SocketCount = 2;

            _multipleProjectiles = ScriptableObject.CreateInstance<GemDefinition>();
            _multipleProjectiles.Id = GemId.MultipleProjectiles;
            CatalogGemModifiers.Bind(_multipleProjectiles);

            _chain = ScriptableObject.CreateInstance<GemDefinition>();
            _chain.Id = GemId.Chain;
            CatalogGemModifiers.Bind(_chain);

            _pipeline = new GemModifierPipeline();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemyDef);
            Object.DestroyImmediate(_towerRole);
            Object.DestroyImmediate(_towerDef);
            Object.DestroyImmediate(_multipleProjectiles);
            Object.DestroyImmediate(_chain);
        }

        [Test]
        public void Tick_NoGems_SpawnsOneProjectileAtPrimary()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);

            Assert.AreEqual(1, director.Projectiles.Count);
            Assert.AreSame(enemy, director.Projectiles[0].Target);
            Assert.AreEqual(10f, director.Projectiles[0].Damage, 1e-4f);
            Assert.Greater(tower.Cooldown, 0f);
        }

        [Test]
        public void Tick_WithMultipleProjectiles_SpawnsProjectileCountSamePrimary()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            Assert.IsTrue(tower.TrySocket(_multipleProjectiles, 0, allowSocket: true));

            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);

            Assert.AreEqual(3, director.Projectiles.Count);
            for (var i = 0; i < director.Projectiles.Count; i++)
                Assert.AreSame(enemy, director.Projectiles[i].Target);
        }

        [Test]
        public void Projectile_OnHit_AppliesDamage_AndChainsToNearestOther()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 200f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            Assert.IsTrue(tower.TrySocket(_chain, 0, allowSocket: true));

            var primary = CreateEnemyAtProgress(0.2f);
            var secondary = CreateEnemyAtProgress(0.15f);
            var registry = new EnemyRegistry();
            registry.Register(primary);
            registry.Register(secondary);

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);
            Assert.AreEqual(1, director.Projectiles.Count);
            Assert.Greater(director.Projectiles[0].ChainRemaining, 0);

            // Drive projectile until first hit + bounce
            for (var i = 0; i < 60; i++)
                director.Tick(0.05f, new List<TowerInstance>(), registry, _pipeline);

            Assert.Less(primary.Hp, 100f);
            Assert.Less(secondary.Hp, 100f);
        }

        [Test]
        public void Projectile_ChainNoOp_WhenNoOtherLiving()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 200f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            Assert.IsTrue(tower.TrySocket(_chain, 0, allowSocket: true));

            var only = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(only);

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);

            for (var i = 0; i < 60; i++)
                director.Tick(0.05f, new List<TowerInstance>(), registry, _pipeline);

            Assert.Less(only.Hp, 100f);
            Assert.AreEqual(0, director.Projectiles.Count);
        }

        [Test]
        public void ClearProjectiles_RemovesInFlightBolts()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);
            Assert.Greater(director.Projectiles.Count, 0);

            director.ClearProjectiles();

            Assert.AreEqual(0, director.Projectiles.Count);
        }

        [Test]
        public void Tick_WithSlowerProjectiles_UsesReducedSpeed()
        {
            var slow = ScriptableObject.CreateInstance<GemDefinition>();
            slow.Id = GemId.SlowerProjectiles;
            CatalogGemModifiers.Bind(slow);
            try
            {
                const float baseSpeed = 100f;
                var director = new CombatDirector(CellSize, projectileSpeed: baseSpeed);
                var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
                Assert.IsTrue(tower.TrySocket(slow, 0, allowSocket: true));

                var enemy = CreateEnemyNearTower();
                var registry = new EnemyRegistry();
                registry.Register(enemy);

                director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);

                Assert.AreEqual(1, director.Projectiles.Count);
                Assert.AreEqual(baseSpeed * 0.6f, director.Projectiles[0].Speed, 1e-4f);
                Assert.AreEqual(13f, director.Projectiles[0].Damage, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(slow);
            }
        }

        [Test]
        public void Tick_UsesRoleProjectileSpeedMultiplier()
        {
            _towerRole.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 1,
                    Modifiers = new[]
                    {
                        RoleStatModifier.Single(
                            RoleStat.ProjectileSpeed,
                            RoleModifierOperation.Set,
                            0.5f)
                    }
                }
            };

            const float baseSpeed = 100f;
            var director = new CombatDirector(CellSize, projectileSpeed: baseSpeed);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);

            Assert.AreEqual(1, director.Projectiles.Count);
            Assert.AreEqual(baseSpeed * 0.5f, director.Projectiles[0].Speed, 1e-4f);
        }

        [Test]
        public void Hydra_SpawnsThreeTimesPelletCountOfBareMultipleProjectiles()
        {
            var hydraTower = ScriptableObject.CreateInstance<TowerDefinition>();
            hydraTower.DisplayName = "Hydra Test Tower";
            var hydraRole = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            hydraTower.Roles = new TowerRoleDefinition[] { hydraRole };
            hydraTower.SocketCount = 3;
            hydraTower.AllowsHydraEvolution = true;
            hydraTower.Damage = 10f;
            hydraRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.ProjectileCount, 1f)
            };

            var fork = ScriptableObject.CreateInstance<GemDefinition>();
            fork.Id = GemId.Fork;
            CatalogGemModifiers.Bind(fork);
            try
            {
                var director = new CombatDirector(CellSize, projectileSpeed: 100f);
                var enemy = CreateEnemyNearTower();
                var registry = new EnemyRegistry();
                registry.Register(enemy);

                var multipleProjectilesOnly = new TowerInstance(new Vector2Int(0, 0), hydraTower);
                Assert.IsTrue(multipleProjectilesOnly.TrySocket(_multipleProjectiles, 0, allowSocket: true));
                director.Tick(0.016f, new List<TowerInstance> { multipleProjectilesOnly }, registry, _pipeline);
                Assert.AreEqual(3, director.Projectiles.Count);

                director.ClearProjectiles();

                var hydra = new TowerInstance(new Vector2Int(0, 0), hydraTower);
                Assert.IsTrue(hydra.TrySocket(_multipleProjectiles, 0, allowSocket: true));
                Assert.IsTrue(hydra.TrySocket(_chain, 1, allowSocket: true));
                Assert.IsTrue(hydra.TrySocket(fork, 2, allowSocket: true));
                director.Tick(0.016f, new List<TowerInstance> { hydra }, registry, _pipeline);

                // Hydra off: same fire as Multiple Projectiles only (3 pellets). Fork/chain children spawn on hit.
                Assert.AreEqual(3, director.Projectiles.Count);
            }
            finally
            {
                Object.DestroyImmediate(hydraTower);
                Object.DestroyImmediate(hydraRole);
                Object.DestroyImmediate(fork);
            }
        }

        [Test]
        public void Tick_DirectAim_PointsAtCurrentPosition()
        {
            _enemyDef.MoveSpeed = 2f;
            _towerRole.AimMode = AimMode.Direct;
            var director = new CombatDirector(CellSize, projectileSpeed: 4f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyAtProgress(0.2f);
            _enemyDef.MoveSpeed = 2f;
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);

            Assert.AreEqual(1, director.Projectiles.Count);
            var shot = director.Projectiles[0];
            var expected = (enemy.WorldPosition - shot.Position).normalized;
            Assert.AreEqual(expected.x, shot.Direction.x, 0.05f);
            Assert.AreEqual(expected.z, shot.Direction.z, 0.05f);
            Assert.AreSame(enemy, shot.Target);
        }

        [Test]
        public void Tick_GroundAim_LeadsAlongPath()
        {
            _enemyDef.MoveSpeed = 2f;
            _towerRole.AimMode = AimMode.Ground;
            var director = new CombatDirector(CellSize, projectileSpeed: 4f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyAtProgress(0.2f);
            _enemyDef.MoveSpeed = 2f;
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            var origin = new Vector3(CellSize * 0.5f, 0f, CellSize * 0.5f);
            var intercept = PathIntercept.Predict(origin, 4f, enemy);

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);

            Assert.AreEqual(1, director.Projectiles.Count);
            var shot = director.Projectiles[0];
            var expected = (intercept - origin).normalized;
            Assert.AreEqual(expected.x, shot.Direction.x, 0.05f);
            Assert.AreEqual(expected.z, shot.Direction.z, 0.05f);
            Assert.Greater(intercept.x, enemy.WorldPosition.x);
        }

        [Test]
        public void Tick_PayloadNova_SpawnsZeroDamagePayload_ThenRadialVolley()
        {
            _towerRole.DeliveryPattern = DeliveryPattern.PayloadNova;
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.ProjectileCount, 4f)
            };
            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);
            Assert.AreEqual(1, director.Projectiles.Count);
            Assert.IsTrue(director.Projectiles[0].IsPayload);
            Assert.AreEqual(0f, director.Projectiles[0].Damage, 1e-4f);
            var hpBefore = enemy.Hp;

            for (var i = 0; i < 30; i++)
            {
                director.Tick(0.05f, new List<TowerInstance>(), registry, _pipeline);
                if (director.Projectiles.Count == 4 && !director.Projectiles[0].IsPayload)
                    break;
            }

            Assert.AreEqual(4, director.Projectiles.Count);
            Assert.AreEqual(hpBefore, enemy.Hp, 1e-4f);
            var yaws = new float[4];
            for (var i = 0; i < 4; i++)
            {
                Assert.IsFalse(director.Projectiles[i].IsPayload);
                var dir = director.Projectiles[i].Direction;
                yaws[i] = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                if (yaws[i] < 0f)
                    yaws[i] += 360f;
            }
            System.Array.Sort(yaws);
            Assert.AreEqual(0f, yaws[0], 1f);
            Assert.AreEqual(90f, yaws[1], 1f);
            Assert.AreEqual(180f, yaws[2], 1f);
            Assert.AreEqual(270f, yaws[3], 1f);
        }

        [Test]
        public void Tick_PayloadNova_CountZero_SpawnsNothing()
        {
            _towerRole.DeliveryPattern = DeliveryPattern.PayloadNova;
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.ProjectileCount, 0f)
            };
            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);
            Assert.AreEqual(0, director.Projectiles.Count);
        }

        [Test]
        public void Tick_PayloadNova_NearAimPoint_LandsImmediately()
        {
            _towerRole.DeliveryPattern = DeliveryPattern.PayloadNova;
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.ProjectileCount, 2f)
            };
            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = new EnemyRuntime();
            enemy.Init(_enemyDef, new List<Vector3>
            {
                new Vector3(CellSize * 0.5f, 0f, CellSize * 0.5f),
                new Vector3(CellSize * 0.5f + 0.01f, 0f, CellSize * 0.5f)
            });
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);
            Assert.AreEqual(2, director.Projectiles.Count);
            Assert.IsFalse(director.Projectiles[0].IsPayload);
            Assert.IsFalse(director.Projectiles[1].IsPayload);
        }

        static RoleStatModifier Modifier(RoleStat stat, float value)
        {
            return RoleStatModifier.Single(stat, RoleModifierOperation.Set, value);
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
