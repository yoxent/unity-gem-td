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
        const float CastCompleteDt = 2f;

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

            TickThroughCast(director, tower, registry);

            Assert.AreEqual(1, director.Projectiles.Count);
            Assert.AreSame(enemy, director.Projectiles[0].Target);
            Assert.AreEqual(10f, director.Projectiles[0].Damage, 1e-4f);
        }

        [Test]
        public void Tick_Fires_IncrementsFireGenerationOnce()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            Assert.AreEqual(0, tower.FireGeneration);
            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);
            Assert.AreEqual(1, tower.FireGeneration);
            Assert.AreEqual(enemy.WorldPosition, tower.LastAimPoint);

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);
            Assert.AreEqual(1, tower.FireGeneration);
        }

        [Test]
        public void Tick_NoTargetInRange_DoesNotIncrementFireGeneration()
        {
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 0.5f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.ProjectileCount, 1f)
            };
            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = new EnemyRuntime();
            enemy.Init(_enemyDef, new[] { new Vector3(50f, 0f, 0f) });
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            TickThroughCast(director, tower, registry);

            Assert.AreEqual(0, director.Projectiles.Count);
            Assert.AreEqual(0, tower.FireGeneration);
        }

        [Test]
        public void Tick_MultipleProjectiles_IncrementsFireGenerationOnce()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            Assert.IsTrue(tower.TrySocket(_multipleProjectiles, 0, allowSocket: true));
            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            TickThroughCast(director, tower, registry);

            Assert.AreEqual(3, director.Projectiles.Count);
            Assert.AreEqual(1, tower.FireGeneration);
        }

        [Test]
        public void TryFireOnce_IncrementsFireGeneration_AndStoresAim()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var living = new List<EnemyRuntime> { enemy };
            var muzzle = new Vector3(0.5f, 0f, 0.5f);

            Assert.IsTrue(director.TryFireOnce(tower, muzzle, living, _pipeline));
            Assert.AreEqual(1, tower.FireGeneration);
            Assert.AreEqual(enemy.WorldPosition, tower.LastAimPoint);
        }

        [Test]
        public void Tick_StartsCast_IncrementsFireGenerationWithoutProjectile()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);

            Assert.AreEqual(1, tower.FireGeneration);
            Assert.AreEqual(0, director.Projectiles.Count);
            Assert.IsTrue(director.HasActiveVolley);
            Assert.AreEqual(1f, tower.CurrentFireInterval, 1e-4f);
        }

        [Test]
        public void Tick_SpawnsProjectileAfterFireInterval()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 1f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var living = new List<EnemyRuntime> { enemy };
            var muzzle = new Vector3(0.5f, 0f, 0.5f);

            Assert.IsTrue(director.TryFireOnce(tower, muzzle, living, _pipeline));
            Assert.AreEqual(0, director.Projectiles.Count);
            Assert.IsTrue(director.HasActiveVolley);

            director.TickInFlight(0.5f, living);
            Assert.AreEqual(0, director.Projectiles.Count);

            director.TickInFlight(0.6f, living);
            Assert.AreEqual(1, director.Projectiles.Count);
            Assert.AreEqual(1, tower.FireGeneration);
        }

        [Test]
        public void Tick_SpawnsProjectileAtStrikeNormalized()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 1f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef)
            {
                StrikeNormalized = 0.5f
            };
            var enemy = CreateEnemyNearTower();
            var living = new List<EnemyRuntime> { enemy };

            Assert.IsTrue(director.TryFireOnce(tower, Vector3.zero, living, _pipeline));
            Assert.AreEqual(0, director.Projectiles.Count);

            director.TickInFlight(0.4f, living);
            Assert.AreEqual(0, director.Projectiles.Count);

            director.TickInFlight(0.2f, living);
            Assert.AreEqual(1, director.Projectiles.Count);
            Assert.AreEqual(1, tower.FireGeneration);
        }

        [Test]
        public void Tick_DoesNotRecastDuringRecoveryAfterStrike()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef)
            {
                StrikeNormalized = 0.5f
            };
            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);
            var towers = new List<TowerInstance> { tower };

            director.Tick(0.016f, towers, registry, _pipeline);
            director.Tick(0.6f, towers, registry, _pipeline);

            Assert.AreEqual(1, tower.FireGeneration);
            Assert.AreEqual(1, director.Projectiles.Count);

            director.Tick(0.3f, towers, registry, _pipeline);
            Assert.AreEqual(1, tower.FireGeneration);
        }

        [Test]
        public void Tick_Miss_ExpiresAtTowerRange()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 20f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var living = new List<EnemyRuntime> { enemy };

            Assert.IsTrue(director.TryFireOnce(tower, Vector3.zero, living, _pipeline));
            director.TickInFlight(CastCompleteDt, living);
            Assert.AreEqual(1, director.Projectiles.Count);

            var empty = new List<EnemyRuntime>();
            director.TickInFlight(0.9f, empty);
            Assert.AreEqual(1, director.Projectiles.Count);

            director.TickInFlight(0.2f, empty);
            Assert.AreEqual(0, director.Projectiles.Count);
        }

        [Test]
        public void Tick_DoesNotRecastOnTheTickThatResolves()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);
            var towers = new List<TowerInstance> { tower };

            director.Tick(0.016f, towers, registry, _pipeline);
            director.Tick(1.1f, towers, registry, _pipeline);

            Assert.AreEqual(1, tower.FireGeneration);
            Assert.AreEqual(1, director.Projectiles.Count);
        }

        [Test]
        public void Tick_FasterAttacks_ShortensCastWait()
        {
            var faster = ScriptableObject.CreateInstance<GemDefinition>();
            faster.Id = GemId.FasterAttacks;
            CatalogGemModifiers.Bind(faster);
            try
            {
                var director = new CombatDirector(CellSize, projectileSpeed: 1f);
                var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
                Assert.IsTrue(tower.TrySocket(faster, 0, allowSocket: true));
                var enemy = CreateEnemyNearTower();
                var living = new List<EnemyRuntime> { enemy };

                Assert.IsTrue(director.TryFireOnce(tower, Vector3.zero, living, _pipeline));
                Assert.AreEqual(0.8f, tower.CurrentFireInterval, 1e-4f);
                Assert.AreEqual(0, director.Projectiles.Count);

                director.TickInFlight(0.5f, living);
                Assert.AreEqual(0, director.Projectiles.Count);

                director.TickInFlight(0.4f, living);
                Assert.AreEqual(1, director.Projectiles.Count);
            }
            finally
            {
                Object.DestroyImmediate(faster);
            }
        }

        [Test]
        public void Tick_GroundPulse_DelaysDamageUntilFireInterval()
        {
            _towerRole.AimMode = AimMode.Ground;
            _towerRole.DeliveryPattern = DeliveryPattern.GroundPulse;
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f)
            };
            _towerDef.Tags = GemTag.Attack | GemTag.Melee | GemTag.Slam | GemTag.Aoe;

            var director = new CombatDirector(CellSize, projectileSpeed: 20f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);
            var towers = new List<TowerInstance> { tower };
            var hpBefore = enemy.Hp;

            director.Tick(0.016f, towers, registry, _pipeline);
            Assert.AreEqual(hpBefore, enemy.Hp, 1e-4f);
            Assert.AreEqual(1, tower.FireGeneration);

            director.Tick(0.5f, towers, registry, _pipeline);
            Assert.AreEqual(hpBefore, enemy.Hp, 1e-4f);

            director.Tick(0.6f, towers, registry, _pipeline);
            Assert.Less(enemy.Hp, hpBefore);
            Assert.AreEqual(1, tower.FireGeneration);
        }

        [Test]
        public void TryFireOnce_DoesNotStartSecondPendingStrike()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 1f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var living = new List<EnemyRuntime> { enemy };

            Assert.IsTrue(director.TryFireOnce(tower, Vector3.zero, living, _pipeline));
            Assert.IsTrue(director.TryFireOnce(tower, Vector3.zero, living, _pipeline));
            Assert.AreEqual(1, tower.FireGeneration);

            director.TickInFlight(1.1f, living);
            Assert.AreEqual(1, director.Projectiles.Count);
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

            TickThroughCast(director, tower, registry);

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

            TickThroughCast(director, tower, registry);
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

            TickThroughCast(director, tower, registry);

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

            TickThroughCast(director, tower, registry);
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

                TickThroughCast(director, tower, registry);

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

            TickThroughCast(director, tower, registry);

            Assert.AreEqual(1, director.Projectiles.Count);
            Assert.AreEqual(baseSpeed * 0.5f, director.Projectiles[0].Speed, 1e-4f);
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

            TickThroughCast(director, tower, registry);

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

            TickThroughCast(director, tower, registry);

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

            TickThroughCast(director, tower, registry);
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

            TickThroughCast(director, tower, registry);
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

            TickThroughCast(director, tower, registry);
            Assert.AreEqual(2, director.Projectiles.Count);
            Assert.IsFalse(director.Projectiles[0].IsPayload);
            Assert.IsFalse(director.Projectiles[1].IsPayload);
        }

        [Test]
        public void Tick_WarpStrike_RiseDoesNotHit_LandDealsMelee()
        {
            _towerRole.AimMode = AimMode.Direct;
            _towerRole.DeliveryPattern = DeliveryPattern.WarpStrike;
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.Damage, 10f)
            };
            _towerRole.EffectPayloads = new[]
            {
                new EffectPayloadDefinition
                {
                    Trigger = EffectPayloadTrigger.OnImpact,
                    Anchor = EffectPayloadAnchor.PrimaryTarget,
                    TravelPattern = EffectPayloadTravelPattern.Fountain,
                    ScatterPattern = EffectPayloadScatterPattern.RandomRing,
                    HitPolicy = EffectPayloadHitPolicy.PerImpact,
                    Count = 4,
                    DamageMultiplier = 0.4f,
                    AoeRadius = 1f,
                    MinDistance = 1f,
                    MaxDistance = 4f
                }
            };
            _towerDef.Tags = GemTag.Attack | GemTag.Melee | GemTag.Strike | GemTag.Projectile;

            var director = new CombatDirector(CellSize, projectileSpeed: 20f, payloadRng: new System.Random(42));
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            TickThroughCast(director, tower, registry);

            Assert.AreEqual(1, director.Projectiles.Count);
            Assert.IsTrue(director.Projectiles[0].IsWarpStrike);
            Assert.AreEqual(100f, enemy.Hp, 1e-4f);

            for (var i = 0; i < 40; i++)
                director.Tick(0.05f, new List<TowerInstance>(), registry, _pipeline);

            Assert.Less(enemy.Hp, 100f);
        }

        [Test]
        public void Tick_WarpStrike_DirectionTurnsDownAfterApex()
        {
            _towerRole.AimMode = AimMode.Direct;
            _towerRole.DeliveryPattern = DeliveryPattern.WarpStrike;
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.Damage, 10f)
            };
            _towerDef.Tags = GemTag.Attack | GemTag.Melee | GemTag.Strike;

            var director = new CombatDirector(CellSize, projectileSpeed: 1f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var living = new List<EnemyRuntime> { enemy };

            Assert.IsTrue(director.TryFireOnce(tower, Vector3.zero, living, _pipeline));
            CompleteCast(director, living);
            Assert.AreEqual(1, director.Projectiles.Count);

            director.TickInFlight(0.75f, living);
            Assert.Greater(director.Projectiles[0].Direction.y, 0f);

            director.TickInFlight(0.75f, living);
            Assert.Less(director.Projectiles[0].Direction.y, -0.9f);
        }

        [Test]
        public void Tick_WarpStrike_SpawnsPayloadsThatCanAoE()
        {
            _towerRole.AimMode = AimMode.Direct;
            _towerRole.DeliveryPattern = DeliveryPattern.WarpStrike;
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.Damage, 10f)
            };
            _towerRole.EffectPayloads = new[]
            {
                new EffectPayloadDefinition
                {
                    Trigger = EffectPayloadTrigger.OnImpact,
                    TravelPattern = EffectPayloadTravelPattern.Fountain,
                    ScatterPattern = EffectPayloadScatterPattern.FixedRadial,
                    HitPolicy = EffectPayloadHitPolicy.PerImpact,
                    Count = 1,
                    DamageMultiplier = 1f,
                    AoeRadius = 2f,
                    MinDistance = 2f,
                    MaxDistance = 2f
                }
            };
            _towerDef.Tags = GemTag.Attack | GemTag.Melee | GemTag.Strike;

            var gem = ScriptableObject.CreateInstance<GemDefinition>();
            gem.Id = GemId.Knockback;
            gem.Tags = GemTag.Support;
            gem.EffectPayloads = new[]
            {
                new EffectPayloadDefinition
                {
                    Trigger = EffectPayloadTrigger.OnImpact,
                    Anchor = EffectPayloadAnchor.PrimaryTarget,
                    TravelPattern = EffectPayloadTravelPattern.Fountain,
                    ScatterPattern = EffectPayloadScatterPattern.FixedRadial,
                    HitPolicy = EffectPayloadHitPolicy.PerImpact,
                    Count = 1,
                    DamageMultiplier = 1f,
                    AoeRadius = 1f,
                    MinDistance = 1f,
                    MaxDistance = 1f
                }
            };

            try
            {
                const int rolePayloadCount = 1;
                var director = new CombatDirector(CellSize, projectileSpeed: 20f);
                var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
                Assert.IsTrue(tower.TrySocket(gem, 0, allowSocket: true));

                var primary = CreateEnemyNearTower();
                var bystander = CreateEnemyAtProgress(0.12f);
                var registry = new EnemyRegistry();
                registry.Register(primary);
                registry.Register(bystander);
                var hpPrimary = primary.Hp;
                var hpBystander = bystander.Hp;

                TickThroughCast(director, tower, registry);

                var observedPayloads = false;
                for (var i = 0; i < 80; i++)
                {
                    director.Tick(0.05f, new List<TowerInstance>(), registry, _pipeline);
                    if (director.EffectPayloads.Count == 0)
                        continue;

                    Assert.AreEqual(rolePayloadCount + 1, director.EffectPayloads.Count);
                    observedPayloads = true;
                    break;
                }

                Assert.IsTrue(observedPayloads);
                for (var i = 0; i < 80; i++)
                    director.Tick(0.05f, new List<TowerInstance>(), registry, _pipeline);

                Assert.That(primary.Hp < hpPrimary || bystander.Hp < hpBystander);
            }
            finally
            {
                Object.DestroyImmediate(gem);
            }
        }

        [Test]
        public void Tick_GroundPulse_HitsPrimaryWithNoProjectile()
        {
            _towerRole.AimMode = AimMode.Ground;
            _towerRole.DeliveryPattern = DeliveryPattern.GroundPulse;
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f)
            };
            _towerDef.Tags = GemTag.Attack | GemTag.Melee | GemTag.Slam | GemTag.Aoe;

            var director = new CombatDirector(CellSize, projectileSpeed: 20f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);
            var hpBefore = enemy.Hp;

            TickThroughCast(director, tower, registry);

            Assert.AreEqual(0, director.Projectiles.Count);
            Assert.Less(enemy.Hp, hpBefore);
        }

        [Test]
        public void Tick_GroundPulse_SplashUsesAimPointNotMuzzle()
        {
            _towerRole.AimMode = AimMode.Ground;
            _towerRole.DeliveryPattern = DeliveryPattern.GroundPulse;
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.SplashRadius, 2f)
            };
            _towerDef.Tags = GemTag.Attack | GemTag.Melee | GemTag.Slam | GemTag.Aoe;

            var director = new CombatDirector(CellSize, projectileSpeed: 20f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var primary = CreateEnemyAtProgress(0.8f);
            var atMuzzle = new EnemyRuntime();
            atMuzzle.Init(_enemyDef, new[] { new Vector3(0.5f, 0f, 0.5f) });
            var registry = new EnemyRegistry();
            registry.Register(primary);
            registry.Register(atMuzzle);
            var muzzleHp = atMuzzle.Hp;

            TickThroughCast(director, tower, registry);

            Assert.Less(primary.Hp, 100f);
            Assert.AreEqual(muzzleHp, atMuzzle.Hp, 1e-4f);
        }

        [Test]
        public void Tick_Rain_DoesNotSnipePrimary_SpawnsTenPayloads()
        {
            _towerRole.AimMode = AimMode.Ground;
            _towerRole.DeliveryPattern = DeliveryPattern.Rain;
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f)
            };
            _towerRole.EffectPayloads = new[] { FirestormCombatPayload() };
            _towerDef.Tags = GemTag.Spell | GemTag.Aoe;
            _towerDef.Damage = 10f;

            var director = new CombatDirector(CellSize, projectileSpeed: 20f, payloadRng: new System.Random(1));
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);
            var hpBefore = enemy.Hp;

            TickThroughCast(director, tower, registry);

            Assert.AreEqual(0, director.Projectiles.Count);
            Assert.AreEqual(10, director.EffectPayloads.Count);
            Assert.AreEqual(hpBefore, enemy.Hp, 1e-4f);
        }

        [Test]
        public void Tick_Rain_RecastReplacesSameTowerStorm()
        {
            _towerRole.AimMode = AimMode.Ground;
            _towerRole.DeliveryPattern = DeliveryPattern.Rain;
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 0.05f),
                Modifier(RoleStat.AttackSpeed, 100f)
            };
            _towerRole.EffectPayloads = new[] { FirestormCombatPayload() };
            _towerDef.Tags = GemTag.Spell | GemTag.Aoe;

            var director = new CombatDirector(CellSize, projectileSpeed: 20f, payloadRng: new System.Random(1));
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            var towers = new List<TowerInstance> { tower };
            director.Tick(0.016f, towers, registry, _pipeline);
            director.Tick(0.1f, towers, registry, _pipeline);
            Assert.AreEqual(10, director.EffectPayloads.Count);
            director.Tick(0.016f, towers, registry, _pipeline);
            director.Tick(0.1f, towers, registry, _pipeline);
            Assert.AreEqual(10, director.EffectPayloads.Count);
        }

        [Test]
        public void Tick_Rain_EchoKeepsEachEchoVolley()
        {
            _towerRole.AimMode = AimMode.Ground;
            _towerRole.DeliveryPattern = DeliveryPattern.Rain;
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f)
            };
            _towerRole.EffectPayloads = new[] { FirestormCombatPayload() };
            _towerDef.Tags = GemTag.Spell | GemTag.Aoe;

            var echo = ScriptableObject.CreateInstance<GemDefinition>();
            echo.Id = GemId.SpellEcho;
            CatalogGemModifiers.Bind(echo);
            try
            {
                var director = new CombatDirector(CellSize, projectileSpeed: 20f);
                var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
                Assert.IsTrue(tower.TrySocket(echo, 0, allowSocket: true));
                var enemy = CreateEnemyNearTower();
                var registry = new EnemyRegistry();
                registry.Register(enemy);

                TickThroughCast(director, tower, registry);

                Assert.AreEqual(20, director.EffectPayloads.Count);
            }
            finally
            {
                Object.DestroyImmediate(echo);
            }
        }

        static EffectPayloadDefinition FirestormCombatPayload()
        {
            return new EffectPayloadDefinition
            {
                Trigger = EffectPayloadTrigger.AfterDelay,
                Anchor = EffectPayloadAnchor.GroundTarget,
                TravelPattern = EffectPayloadTravelPattern.FallFromSky,
                ScatterPattern = EffectPayloadScatterPattern.None,
                HitPolicy = EffectPayloadHitPolicy.PerImpact,
                Tags = GemTag.Aoe,
                Count = 10,
                DamageMultiplier = 1f,
                AoeRadius = 1.3f,
                MinDistance = 0f,
                MaxDistance = 2.5f,
                ArcHeight = 3f,
                DelaySeconds = 0f,
                IntervalSeconds = 0.15f
            };
        }

        [Test]
        public void Tick_CasterNova_HitsInsideTowerRadius()
        {
            _towerRole.AimMode = AimMode.Direct;
            _towerRole.DeliveryPattern = DeliveryPattern.CasterNova;
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 5f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.SplashRadius, 2.6f)
            };
            _towerDef.Tags = GemTag.Spell | GemTag.Aoe;

            var director = new CombatDirector(CellSize, projectileSpeed: 20f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            // Path (0,0)→(10,0): progress 0.4 ≈ x=4.5 (inside radius 5, outside splash 2.6) = primary (First).
            var primary = CreateEnemyAtProgress(0.4f);
            var nonPrimary = CreateEnemyAtProgress(0.1f);
            var registry = new EnemyRegistry();
            registry.Register(primary);
            registry.Register(nonPrimary);
            var primaryHp = primary.Hp;
            var nonPrimaryHp = nonPrimary.Hp;

            TickThroughCast(director, tower, registry);

            Assert.AreEqual(0, director.Projectiles.Count);
            Assert.Less(primary.Hp, primaryHp);
            Assert.Less(nonPrimary.Hp, nonPrimaryHp);
        }

        [Test]
        public void Tick_CasterNova_MissesEnemyOutsideTowerRadius()
        {
            _towerRole.AimMode = AimMode.Direct;
            _towerRole.DeliveryPattern = DeliveryPattern.CasterNova;
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 5f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.SplashRadius, 2.6f)
            };
            _towerDef.Tags = GemTag.Spell | GemTag.Aoe;

            var director = new CombatDirector(CellSize, projectileSpeed: 20f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var near = new EnemyRuntime();
            near.Init(_enemyDef, new[] { new Vector3(1.5f, 0f, 0.5f) });
            var outsider = new EnemyRuntime();
            outsider.Init(_enemyDef, new[] { new Vector3(8.5f, 0f, 0.5f) });
            var registry = new EnemyRegistry();
            registry.Register(near);
            registry.Register(outsider);
            var nearHp = near.Hp;
            var outsiderHp = outsider.Hp;

            TickThroughCast(director, tower, registry);

            Assert.AreEqual(0, director.Projectiles.Count);
            Assert.Less(near.Hp, nearHp);
            Assert.AreEqual(outsiderHp, outsider.Hp, 1e-4f);
        }

        [Test]
        public void Tick_CursePresence_AppliesHexWithoutDamage()
        {
            var curseRole = ScriptableObject.CreateInstance<CurseRoleDefinition>();
            curseRole.AimMode = AimMode.Direct;
            curseRole.DeliveryPattern = DeliveryPattern.CasterNova;
            curseRole.Modifiers = new[] { Modifier(RoleStat.TowerRadius, 3f) };
            curseRole.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 1,
                    Effects = new[]
                    {
                        RoleEffectModifier.Single(
                            RoleEffectKind.EnemyColdResistance,
                            RoleModifierOperation.Set,
                            -30f)
                    }
                }
            };
            var curseDef = ScriptableObject.CreateInstance<TowerDefinition>();
            curseDef.Roles = new TowerRoleDefinition[] { curseRole };
            curseDef.Damage = 0f;

            var director = new CombatDirector(CellSize);
            var tower = new TowerInstance(new Vector2Int(0, 0), curseDef);
            var enemy = new EnemyRuntime();
            enemy.Init(_enemyDef, new[] { new Vector3(1.5f, 0f, 0.5f) });
            var hp = enemy.Hp;
            var registry = new EnemyRegistry();
            registry.Register(enemy);
            var statuses = new StatusRuntime();

            TickThroughCast(director, tower, registry, statuses);

            Assert.AreEqual(0, director.Projectiles.Count);
            Assert.AreEqual(hp, enemy.Hp, 1e-4f);
            Assert.IsTrue(statuses.Has(enemy, StatusId.CurseFrostbite));
            Assert.IsTrue(statuses.TryGetMagnitude(enemy, StatusId.CurseFrostbite, out var mag));
            Assert.AreEqual(-30f, mag, 0.001f);

            Object.DestroyImmediate(curseRole);
            Object.DestroyImmediate(curseDef);
        }

        [Test]
        public void Tick_CursePresence_ElementalWeakness_AppliesElementalHexWithoutDamage()
        {
            var curseRole = ScriptableObject.CreateInstance<CurseRoleDefinition>();
            curseRole.AimMode = AimMode.Direct;
            curseRole.DeliveryPattern = DeliveryPattern.CasterNova;
            curseRole.Modifiers = new[] { Modifier(RoleStat.TowerRadius, 3f) };
            curseRole.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 1,
                    Effects = new[]
                    {
                        RoleEffectModifier.Single(
                            RoleEffectKind.EnemyFireResistance,
                            RoleModifierOperation.Set,
                            -30f),
                        RoleEffectModifier.Single(
                            RoleEffectKind.EnemyColdResistance,
                            RoleModifierOperation.Set,
                            -30f),
                        RoleEffectModifier.Single(
                            RoleEffectKind.EnemyLightningResistance,
                            RoleModifierOperation.Set,
                            -30f)
                    }
                }
            };
            var curseDef = ScriptableObject.CreateInstance<TowerDefinition>();
            curseDef.Roles = new TowerRoleDefinition[] { curseRole };
            curseDef.Damage = 0f;

            var director = new CombatDirector(CellSize);
            var tower = new TowerInstance(new Vector2Int(0, 0), curseDef);
            var enemy = new EnemyRuntime();
            enemy.Init(_enemyDef, new[] { new Vector3(1.5f, 0f, 0.5f) });
            var hp = enemy.Hp;
            var registry = new EnemyRegistry();
            registry.Register(enemy);
            var statuses = new StatusRuntime();

            TickThroughCast(director, tower, registry, statuses);

            Assert.AreEqual(0, director.Projectiles.Count);
            Assert.AreEqual(hp, enemy.Hp, 1e-4f);
            Assert.IsTrue(statuses.Has(enemy, StatusId.CurseElementalWeakness));
            Assert.IsFalse(statuses.Has(enemy, StatusId.CurseFlammability));
            Assert.IsTrue(statuses.TryGetMagnitude(enemy, StatusId.CurseElementalWeakness, out var mag));
            Assert.AreEqual(-30f, mag, 0.001f);

            Object.DestroyImmediate(curseRole);
            Object.DestroyImmediate(curseDef);
        }

        [Test]
        public void Tick_CursePresence_Hexproof_DoesNotApplyHex()
        {
            var curseRole = ScriptableObject.CreateInstance<CurseRoleDefinition>();
            curseRole.AimMode = AimMode.Direct;
            curseRole.DeliveryPattern = DeliveryPattern.CasterNova;
            curseRole.Modifiers = new[] { Modifier(RoleStat.TowerRadius, 3f) };
            curseRole.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 1,
                    Effects = new[]
                    {
                        RoleEffectModifier.Single(
                            RoleEffectKind.EnemyColdResistance,
                            RoleModifierOperation.Set,
                            -30f)
                    }
                }
            };
            var curseDef = ScriptableObject.CreateInstance<TowerDefinition>();
            curseDef.Roles = new TowerRoleDefinition[] { curseRole };
            curseDef.Damage = 0f;

            _enemyDef.Affixes = new[] { EnemyAffix.Hexproof };
            var director = new CombatDirector(CellSize);
            var tower = new TowerInstance(new Vector2Int(0, 0), curseDef);
            var enemy = new EnemyRuntime();
            enemy.Init(_enemyDef, new[] { new Vector3(1.5f, 0f, 0.5f) });
            var registry = new EnemyRegistry();
            registry.Register(enemy);
            var statuses = new StatusRuntime();

            TickThroughCast(director, tower, registry, statuses);

            Assert.IsFalse(statuses.Has(enemy, StatusId.CurseFrostbite));

            Object.DestroyImmediate(curseRole);
            Object.DestroyImmediate(curseDef);
        }

        [Test]
        public void Tick_CursePresence_Unhallowed_ScalesMagnitude()
        {
            var curseRole = ScriptableObject.CreateInstance<CurseRoleDefinition>();
            curseRole.AimMode = AimMode.Direct;
            curseRole.DeliveryPattern = DeliveryPattern.CasterNova;
            curseRole.Modifiers = new[] { Modifier(RoleStat.TowerRadius, 3f) };
            curseRole.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 1,
                    Effects = new[]
                    {
                        RoleEffectModifier.Single(
                            RoleEffectKind.EnemyColdResistance,
                            RoleModifierOperation.Set,
                            -30f)
                    }
                }
            };
            var curseDef = ScriptableObject.CreateInstance<TowerDefinition>();
            curseDef.Roles = new TowerRoleDefinition[] { curseRole };
            curseDef.Damage = 0f;

            _enemyDef.Affixes = new[] { EnemyAffix.Unhallowed };
            var director = new CombatDirector(CellSize);
            var tower = new TowerInstance(new Vector2Int(0, 0), curseDef);
            var enemy = new EnemyRuntime();
            enemy.Init(_enemyDef, new[] { new Vector3(1.5f, 0f, 0.5f) });
            var registry = new EnemyRegistry();
            registry.Register(enemy);
            var statuses = new StatusRuntime();

            TickThroughCast(director, tower, registry, statuses);

            Assert.IsTrue(statuses.TryGetMagnitude(enemy, StatusId.CurseFrostbite, out var mag));
            Assert.AreEqual(-18f, mag, 0.001f);

            Object.DestroyImmediate(curseRole);
            Object.DestroyImmediate(curseDef);
        }

        [Test]
        public void Tick_Bloody_AppliesPackHealthWithoutTowers()
        {
            _enemyDef.MaxHealth = 20f;
            _enemyDef.Affixes = new[] { EnemyAffix.Bloody };
            var enemy = new EnemyRuntime();
            enemy.Init(_enemyDef, new[] { new Vector3(1.5f, 0f, 0.5f) });
            var registry = new EnemyRegistry();
            registry.Register(enemy);
            var director = new CombatDirector(CellSize);

            director.Tick(0.016f, new List<TowerInstance>(), registry, _pipeline);

            var expected = 20f * DamageTypeCombat.PackAuraHealthFraction * DamageTypeCombat.PackAuraSelfMultiplier;
            Assert.AreEqual(20f + expected, enemy.MaxHealth, 1e-4f);
        }

        [Test]
        public void Tick_CursePresence_DropsHexWhenEnemyLeaves()
        {
            var curseRole = ScriptableObject.CreateInstance<CurseRoleDefinition>();
            curseRole.AimMode = AimMode.Direct;
            curseRole.DeliveryPattern = DeliveryPattern.CasterNova;
            curseRole.Modifiers = new[] { Modifier(RoleStat.TowerRadius, 3f) };
            curseRole.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 1,
                    Effects = new[]
                    {
                        RoleEffectModifier.Single(
                            RoleEffectKind.EnemyColdResistance,
                            RoleModifierOperation.Set,
                            -30f)
                    }
                }
            };
            var curseDef = ScriptableObject.CreateInstance<TowerDefinition>();
            curseDef.Roles = new TowerRoleDefinition[] { curseRole };

            var director = new CombatDirector(CellSize);
            var tower = new TowerInstance(new Vector2Int(0, 0), curseDef);
            var enemy = new EnemyRuntime();
            enemy.Init(_enemyDef, new[] { new Vector3(1.5f, 0f, 0.5f) });
            var registry = new EnemyRegistry();
            registry.Register(enemy);
            var statuses = new StatusRuntime();

            TickThroughCast(director, tower, registry, statuses);
            Assert.IsTrue(statuses.Has(enemy, StatusId.CurseFrostbite));

            enemy.SetWorldPosition(new Vector3(20f, 0f, 0.5f));
            TickThroughCast(director, tower, registry, statuses);
            Assert.IsFalse(statuses.Has(enemy, StatusId.CurseFrostbite));

            Object.DestroyImmediate(curseRole);
            Object.DestroyImmediate(curseDef);
        }

        [Test]
        public void Tick_TemporalChains_SlowsMoveSpeed()
        {
            var curseRole = ScriptableObject.CreateInstance<CurseRoleDefinition>();
            curseRole.AimMode = AimMode.Direct;
            curseRole.DeliveryPattern = DeliveryPattern.CasterNova;
            curseRole.Modifiers = new[] { Modifier(RoleStat.TowerRadius, 3f) };
            curseRole.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 1,
                    Effects = new[]
                    {
                        RoleEffectModifier.Single(
                            RoleEffectKind.EnemyActionSpeedLessNormal,
                            RoleModifierOperation.Set,
                            17f)
                    }
                }
            };
            var curseDef = ScriptableObject.CreateInstance<TowerDefinition>();
            curseDef.Roles = new TowerRoleDefinition[] { curseRole };

            var director = new CombatDirector(CellSize);
            var tower = new TowerInstance(new Vector2Int(0, 0), curseDef);
            var enemy = new EnemyRuntime();
            enemy.Init(_enemyDef, new[] { new Vector3(1.5f, 0f, 0.5f) });
            var registry = new EnemyRegistry();
            registry.Register(enemy);
            var statuses = new StatusRuntime();

            TickThroughCast(director, tower, registry, statuses);
            statuses.Tick(0f, new List<EnemyRuntime> { enemy });
            Assert.AreEqual(0.83f, enemy.MoveSpeedMultiplier, 0.001f);

            Object.DestroyImmediate(curseRole);
            Object.DestroyImmediate(curseDef);
        }

        [Test]
        public void TryFireOnce_UsesWorldMuzzle_NotCellCenter()
        {
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 5f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.ProjectileCount, 1f)
            };
            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = new EnemyRuntime();
            enemy.Init(_enemyDef, new[] { new Vector3(12f, 0f, 0f) });
            var living = new List<EnemyRuntime> { enemy };

            Assert.IsTrue(director.TryFireOnce(tower, new Vector3(10f, 0f, 0f), living, _pipeline));
            CompleteCast(director, living);
            Assert.AreEqual(1, director.Projectiles.Count);

            var missed = new CombatDirector(CellSize, projectileSpeed: 100f);
            var registry = new EnemyRegistry();
            registry.Register(enemy);
            missed.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);
            Assert.AreEqual(0, missed.Projectiles.Count);
        }

        [Test]
        public void TryFireOnce_GroundPulse_SpawnsSlamVisualAndAftershock()
        {
            _towerRole.AimMode = AimMode.Ground;
            _towerRole.DeliveryPattern = DeliveryPattern.GroundPulse;
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 0.2f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.SplashRadius, 1.8f),
                Modifier(RoleStat.Damage, 10f)
            };
            _towerRole.EffectPayloads = new[]
            {
                new EffectPayloadDefinition
                {
                    Trigger = EffectPayloadTrigger.AfterDelay,
                    Anchor = EffectPayloadAnchor.GroundTarget,
                    TravelPattern = EffectPayloadTravelPattern.StationaryPulse,
                    ScatterPattern = EffectPayloadScatterPattern.None,
                    HitPolicy = EffectPayloadHitPolicy.PerImpact,
                    Tags = GemTag.Aoe,
                    Count = 1,
                    DamageMultiplier = 2.5f,
                    AoeRadius = 2.8f,
                    DelaySeconds = 1f
                }
            };
            _towerDef.Tags = GemTag.Attack | GemTag.Melee | GemTag.Slam | GemTag.Aoe;
            _towerDef.Damage = 10f;

            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var primary = new EnemyRuntime();
            primary.Init(_enemyDef, new[] { new Vector3(4f, 0f, 0f) });
            var fringe = new EnemyRuntime();
            fringe.Init(_enemyDef, new[] { new Vector3(6.2f, 0f, 0f) });
            var living = new List<EnemyRuntime> { primary, fringe };
            var muzzle = new Vector3(0.5f, 0f, 0.5f);

            Assert.IsTrue(director.TryFireOnce(tower, muzzle, living, _pipeline));
            Assert.AreEqual(0, director.Projectiles.Count);
            Assert.AreEqual(0, director.EffectPayloads.Count);
            Assert.IsTrue(director.HasActiveVolley);

            director.TickInFlight(0.25f, living);
            Assert.AreEqual(2, director.EffectPayloads.Count);
            Assert.AreEqual(EffectPayloadVisual.Slam, director.EffectPayloads[0].Plan.Visual);
            Assert.AreEqual(EffectPayloadVisual.Aftershock, director.EffectPayloads[1].Plan.Visual);
            Assert.IsTrue(director.EffectPayloads[0].ShowsSlamVisual);
            Assert.IsFalse(director.EffectPayloads[1].ShowsAftershockVisual);
            Assert.Less(primary.Hp, 100f);
            Assert.AreEqual(100f, fringe.Hp, 1e-4f);

            director.TickInFlight(1.02f, living);
            director.TickInFlight(0.02f, living);
            Assert.Less(fringe.Hp, 100f);
            Assert.GreaterOrEqual(director.EffectPayloads.Count, 1);

            director.TickInFlight(EffectPayloadRuntime.StationaryPulseVisualSeconds, living);
            Assert.AreEqual(0, director.EffectPayloads.Count);
        }

        [Test]
        public void TryFireOnce_GroundPulse_SecondSlamSpawnsSecondAftershock()
        {
            _towerRole.AimMode = AimMode.Ground;
            _towerRole.DeliveryPattern = DeliveryPattern.GroundPulse;
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 0.2f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.SplashRadius, 1.8f),
                Modifier(RoleStat.Damage, 10f)
            };
            _towerRole.EffectPayloads = new[]
            {
                new EffectPayloadDefinition
                {
                    Trigger = EffectPayloadTrigger.AfterDelay,
                    Anchor = EffectPayloadAnchor.GroundTarget,
                    TravelPattern = EffectPayloadTravelPattern.StationaryPulse,
                    ScatterPattern = EffectPayloadScatterPattern.None,
                    HitPolicy = EffectPayloadHitPolicy.PerImpact,
                    Tags = GemTag.Aoe,
                    Count = 1,
                    DamageMultiplier = 2.5f,
                    AoeRadius = 2.8f,
                    DelaySeconds = 1f
                }
            };
            _towerDef.Tags = GemTag.Attack | GemTag.Melee | GemTag.Slam | GemTag.Aoe;
            _towerDef.Damage = 10f;

            var director = new CombatDirector(CellSize, projectileSpeed: 100f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var primary = new EnemyRuntime();
            primary.Init(_enemyDef, new[] { new Vector3(4f, 0f, 0f) });
            var living = new List<EnemyRuntime> { primary };
            var muzzle = new Vector3(0.5f, 0f, 0.5f);

            Assert.IsTrue(director.TryFireOnce(tower, muzzle, living, _pipeline));
            director.TickInFlight(0.1f, living);
            Assert.IsTrue(director.TryFireOnce(tower, muzzle, living, _pipeline));
            director.TickInFlight(0.1f, living);

            var aftershocks = 0;
            var slams = 0;
            for (var i = 0; i < director.EffectPayloads.Count; i++)
            {
                var visual = director.EffectPayloads[i].Plan.Visual;
                if (visual == EffectPayloadVisual.Aftershock)
                    aftershocks++;
                else if (visual == EffectPayloadVisual.Slam)
                    slams++;
            }

            Assert.AreEqual(2, slams);
            Assert.AreEqual(2, aftershocks);
        }

        [Test]
        public void TryFireOnce_Curse_UsesWorldMuzzle_NotCellCenter()
        {
            var curseRole = ScriptableObject.CreateInstance<CurseRoleDefinition>();
            curseRole.AimMode = AimMode.Direct;
            curseRole.DeliveryPattern = DeliveryPattern.CasterNova;
            curseRole.Modifiers = new[] { Modifier(RoleStat.TowerRadius, 3f) };
            curseRole.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 1,
                    Effects = new[]
                    {
                        RoleEffectModifier.Single(
                            RoleEffectKind.EnemyColdResistance,
                            RoleModifierOperation.Set,
                            -30f)
                    }
                }
            };
            var curseDef = ScriptableObject.CreateInstance<TowerDefinition>();
            curseDef.Roles = new TowerRoleDefinition[] { curseRole };
            curseDef.Damage = 0f;

            var director = new CombatDirector(CellSize);
            var tower = new TowerInstance(new Vector2Int(0, 0), curseDef);
            var nearMuzzle = new EnemyRuntime();
            nearMuzzle.Init(_enemyDef, new[] { new Vector3(10f, 0f, 0f) });
            var nearCell = new EnemyRuntime();
            nearCell.Init(_enemyDef, new[] { new Vector3(0.5f, 0f, 0.5f) });
            var living = new List<EnemyRuntime> { nearMuzzle, nearCell };
            var statuses = new StatusRuntime();

            Assert.IsTrue(director.TryFireOnce(
                tower,
                new Vector3(10f, 0f, 0f),
                living,
                _pipeline,
                statuses));
            Assert.IsTrue(statuses.Has(nearMuzzle, StatusId.CurseFrostbite));
            Assert.IsFalse(statuses.Has(nearCell, StatusId.CurseFrostbite));

            Object.DestroyImmediate(curseRole);
            Object.DestroyImmediate(curseDef);
        }

        [Test]
        public void TickInFlight_AdvancesExistingBolt_DoesNotRefire()
        {
            var director = new CombatDirector(CellSize, projectileSpeed: 20f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = CreateEnemyNearTower();
            var living = new List<EnemyRuntime> { enemy };

            Assert.IsTrue(director.TryFireOnce(tower, Vector3.zero, living, _pipeline));
            CompleteCast(director, living);
            var origin = director.Projectiles[0].Position;

            director.TickInFlight(0.05f, living);

            Assert.AreEqual(1, director.Projectiles.Count);
            Assert.Greater((director.Projectiles[0].Position - origin).sqrMagnitude, 0f);
            Assert.IsTrue(director.HasActiveVolley);
        }

        [Test]
        public void TryFireOnce_SequentialInterval_StaggersStraightBolts()
        {
            _towerRole.SpreadDegrees = 12f;
            _towerRole.SequentialIntervalSeconds = 0.1f;
            _towerRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.ProjectileCount, 3f),
                Modifier(RoleStat.Damage, 10f)
            };

            var director = new CombatDirector(CellSize, projectileSpeed: 0.01f);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var primary = new EnemyRuntime();
            primary.Init(_enemyDef, new[] { new Vector3(8f, 0f, 0f) });
            var living = new List<EnemyRuntime> { primary };
            var muzzle = new Vector3(0.5f, 0f, 0.5f);

            Assert.IsTrue(director.TryFireOnce(tower, muzzle, living, _pipeline));
            CompleteCast(director, living);
            Assert.AreEqual(1, director.Projectiles.Count);
            Assert.IsTrue(director.HasActiveVolley);

            director.TickInFlight(0.1f, living);
            Assert.AreEqual(2, director.Projectiles.Count);

            director.TickInFlight(0.1f, living);
            Assert.AreEqual(3, director.Projectiles.Count);
            Assert.AreEqual(3, director.Projectiles.Count);
        }

        void TickThroughCast(
            CombatDirector director,
            TowerInstance tower,
            EnemyRegistry registry,
            StatusRuntime statuses = null)
        {
            var towers = new List<TowerInstance> { tower };
            director.Tick(0.016f, towers, registry, _pipeline, statuses);
            director.Tick(CastCompleteDt, towers, registry, _pipeline, statuses);
        }

        static void CompleteCast(CombatDirector director, List<EnemyRuntime> living)
        {
            director.TickInFlight(CastCompleteDt, living);
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
