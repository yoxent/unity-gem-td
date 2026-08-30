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
                chainRange: ProjectileRuntime.DefaultChainRange,
                chainHopFalloff: ProjectileRuntime.DefaultChainHopFalloff);

            Assert.IsTrue(projectile.Tick(0.05f, living));
            Assert.AreEqual(90f, e1.Hp, 1e-3f);
            Assert.AreEqual(6f, projectile.Damage, 1e-3f);
            Assert.AreSame(e2, projectile.Target);
            Assert.IsFalse(projectile.Seeking);

            Assert.IsTrue(projectile.Tick(0.05f, living));
            Assert.AreEqual(94f, e2.Hp, 1e-3f);
            Assert.AreEqual(3.6f, projectile.Damage, 1e-3f);
            Assert.AreSame(e3, projectile.Target);
            Assert.IsFalse(projectile.Seeking);

            Assert.IsFalse(projectile.Tick(0.05f, living));
            Assert.AreEqual(96.4f, e3.Hp, 1e-3f);
            Assert.IsFalse(projectile.IsActive);
        }

        [Test]
        public void Chain_OneHop_DoesNotContinueToThird()
        {
            var e1 = MakeEnemyAt(Vector3.zero, 100f);
            var e2 = MakeEnemyAt(new Vector3(1f, 0f, 0f), 100f);
            var e3 = MakeEnemyAt(new Vector3(2f, 0f, 0f), 100f);
            var living = Living(e1, e2, e3);

            var projectile = new ProjectileRuntime();
            projectile.Init(
                origin: Vector3.zero,
                direction: Vector3.right,
                target: e1,
                damage: 10f,
                chainCount: 1,
                speed: 100f,
                chainRange: ProjectileRuntime.DefaultChainRange,
                chainHopFalloff: ProjectileRuntime.DefaultChainHopFalloff);

            Assert.IsTrue(projectile.Tick(0.05f, living));
            Assert.AreSame(e2, projectile.Target);

            Assert.IsFalse(projectile.Tick(0.05f, living));
            Assert.AreEqual(94f, e2.Hp, 1e-3f);
            Assert.AreEqual(100f, e3.Hp, 1e-3f);
            Assert.IsFalse(projectile.IsActive);
        }

        [Test]
        public void Chain_DoesNotHopBeyondRange()
        {
            var e1 = MakeEnemyAt(Vector3.zero, 100f);
            var far = MakeEnemyAt(new Vector3(ProjectileRuntime.DefaultChainRange + 1f, 0f, 0f), 100f);
            var living = Living(e1, far);

            var projectile = new ProjectileRuntime();
            projectile.Init(
                origin: Vector3.zero,
                direction: Vector3.right,
                target: e1,
                damage: 10f,
                chainCount: 2,
                speed: 100f,
                chainRange: ProjectileRuntime.DefaultChainRange);

            Assert.IsFalse(projectile.Tick(0.05f, living));
            Assert.AreEqual(90f, e1.Hp, 1e-3f);
            Assert.AreEqual(100f, far.Hp, 1e-3f);
            Assert.IsFalse(projectile.IsActive);
        }

        [Test]
        public void Shotgun_EachPellet_FullDamage_Regression()
        {
            var towerDef = ScriptableObject.CreateInstance<TowerDefinition>();
            var attackRole = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            attackRole.Modifiers = new[]
            {
                RoleStatModifier.Single(RoleStat.TowerRadius, RoleModifierOperation.Set, 20f),
                RoleStatModifier.Single(RoleStat.AttackTime, RoleModifierOperation.Set, 1f),
                RoleStatModifier.Single(RoleStat.AttackSpeed, RoleModifierOperation.Set, 100f),
                RoleStatModifier.Single(RoleStat.ProjectileCount, RoleModifierOperation.Set, 1f)
            };
            towerDef.Roles = new TowerRoleDefinition[] { attackRole };
            towerDef.Damage = 10f;
            towerDef.SocketCount = 2;

            var multipleProjectiles = ScriptableObject.CreateInstance<GemDefinition>();
            multipleProjectiles.Id = GemId.MultipleProjectiles;
            CatalogGemModifiers.Bind(multipleProjectiles);

            try
            {
                var director = new CombatDirector(CellSize, projectileSpeed: 100f);
                var tower = new TowerInstance(new Vector2Int(0, 0), towerDef);
                Assert.IsTrue(tower.TrySocket(multipleProjectiles, 0, allowSocket: true));

                var enemy = MakeEnemyAt(new Vector3(1.5f, 0f, 0.5f), 100f);
                var registry = new EnemyRegistry();
                registry.Register(enemy);
                var pipeline = new GemModifierPipeline();

                director.Tick(0.016f, new List<TowerInstance> { tower }, registry, pipeline);

                Assert.AreEqual(3, director.Projectiles.Count);
                // Multiple Projectiles post-mod damage is base*0.8; each pellet deals that full amount (not split).
                // Pellets fan and fly straight (no homing).
                for (var i = 0; i < director.Projectiles.Count; i++)
                {
                    Assert.AreSame(enemy, director.Projectiles[i].Target);
                    Assert.IsFalse(director.Projectiles[i].Seeking);
                    Assert.IsFalse(director.Projectiles[i].SoftSeek);
                    Assert.AreEqual(8f, director.Projectiles[i].Damage, 1e-4f);
                }

                Assert.Greater(
                    Vector3.Angle(director.Projectiles[0].Direction, director.Projectiles[2].Direction),
                    1f);
            }
            finally
            {
                Object.DestroyImmediate(towerDef);
                Object.DestroyImmediate(attackRole);
                Object.DestroyImmediate(multipleProjectiles);
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
                forkRemaining: 2,
                spawnBuffer: spawnBuffer);

            projectile.Tick(0.05f, living);

            Assert.AreEqual(2, spawnBuffer.Count);
            Assert.AreEqual(0, projectile.ForkRemaining);
            Assert.IsFalse(projectile.IsActive);

            var expectedPlus = Quaternion.Euler(0f, ProjectileRuntime.ForkHalfAngleDegrees, 0f) * inbound;
            var expectedMinus = Quaternion.Euler(0f, -ProjectileRuntime.ForkHalfAngleDegrees, 0f) * inbound;
            Assert.AreEqual(0f, Vector3.Angle(expectedMinus, spawnBuffer[0].Direction), 0.1f);
            Assert.AreEqual(0f, Vector3.Angle(expectedPlus, spawnBuffer[1].Direction), 0.1f);
            // Forward bias: children continue past the hit (-o<), not back toward the tower.
            Assert.Greater(Vector3.Dot(spawnBuffer[0].Direction, inbound), 0.5f);
            Assert.Greater(Vector3.Dot(spawnBuffer[1].Direction, inbound), 0.5f);
            Assert.Greater(Vector3.Dot(spawnBuffer[0].Position - enemy.WorldPosition, inbound), 0f);
            Assert.Greater(Vector3.Dot(spawnBuffer[1].Position - enemy.WorldPosition, inbound), 0f);
            Assert.AreEqual(0, spawnBuffer[0].ForkRemaining);
            Assert.AreEqual(0, spawnBuffer[1].ForkRemaining);
            Assert.AreEqual(10f, spawnBuffer[0].Damage, 1e-4f);
            Assert.AreEqual(10f, spawnBuffer[1].Damage, 1e-4f);
            Assert.IsFalse(spawnBuffer[0].Seeking);
            Assert.IsFalse(spawnBuffer[1].Seeking);
            Assert.IsNull(spawnBuffer[0].Target);
            Assert.IsNull(spawnBuffer[1].Target);

            var plusDir = spawnBuffer[0].Direction;
            var minusDir = spawnBuffer[1].Direction;
            spawnBuffer[0].Tick(0.1f, living);
            spawnBuffer[1].Tick(0.1f, living);
            Assert.AreEqual(0f, Vector3.Angle(plusDir, spawnBuffer[0].Direction), 0.1f);
            Assert.AreEqual(0f, Vector3.Angle(minusDir, spawnBuffer[1].Direction), 0.1f);
        }

        [Test]
        public void Fork_CountThree_FansAcrossHalfAngle()
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
                forkRemaining: 3,
                spawnBuffer: spawnBuffer);

            projectile.Tick(0.05f, living);

            Assert.AreEqual(3, spawnBuffer.Count);
            Assert.AreEqual(
                0f,
                Vector3.Angle(
                    Quaternion.Euler(0f, -ProjectileRuntime.ForkHalfAngleDegrees, 0f) * inbound,
                    spawnBuffer[0].Direction),
                0.1f);
            Assert.AreEqual(0f, Vector3.Angle(inbound, spawnBuffer[1].Direction), 0.1f);
            Assert.AreEqual(
                0f,
                Vector3.Angle(
                    Quaternion.Euler(0f, ProjectileRuntime.ForkHalfAngleDegrees, 0f) * inbound,
                    spawnBuffer[2].Direction),
                0.1f);
        }

        [Test]
        public void Fork_ParentDies_ChildrenNeverFork_EvenWithExtraCount()
        {
            var first = MakeEnemyAt(Vector3.zero, 100f);
            var inbound = Vector3.forward;
            var plusDir = Quaternion.Euler(0f, ProjectileRuntime.ForkHalfAngleDegrees, 0f) * inbound;
            var second = MakeEnemyAt(plusDir * 2f, 100f);
            var living = Living(first, second);
            var spawnBuffer = new List<ProjectileRuntime>();

            var projectile = new ProjectileRuntime();
            projectile.Init(
                origin: Vector3.zero,
                direction: inbound,
                target: first,
                damage: 10f,
                chainCount: 0,
                speed: 100f,
                chainRange: 0f,
                aoeRadius: 0f,
                pierceRemaining: 0,
                forkRemaining: 3,
                spawnBuffer: spawnBuffer);

            projectile.Tick(0.05f, living);

            Assert.IsFalse(projectile.IsActive);
            Assert.AreEqual(0, projectile.ForkRemaining);
            Assert.AreEqual(3, spawnBuffer.Count);
            Assert.AreEqual(0, spawnBuffer[0].ForkRemaining);
            Assert.AreEqual(0, spawnBuffer[1].ForkRemaining);
            Assert.AreEqual(0, spawnBuffer[2].ForkRemaining);

            spawnBuffer[2].Tick(0.05f, living);

            Assert.AreEqual(3, spawnBuffer.Count);
            Assert.AreEqual(0, spawnBuffer[2].ForkRemaining);
            Assert.IsFalse(spawnBuffer[2].IsActive);
            Assert.AreEqual(90f, second.Hp, 1e-3f);
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
                forkRemaining: 2,
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
                pierceRemaining: 1,
                forkRemaining: 1,
                spawnBuffer: spawnBuffer);

            Assert.IsTrue(projectile.Tick(0.05f, living));
            Assert.IsTrue(projectile.IsActive);
            Assert.IsFalse(projectile.Seeking);
            Assert.AreEqual(0, spawnBuffer.Count);
            Assert.AreEqual(0, projectile.PierceRemaining);
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
                pierceRemaining: ProjectileRuntime.DefaultPierceRemaining);

            Assert.IsTrue(projectile.Tick(0.05f, living));
            Assert.AreEqual(90f, first.Hp, 1e-3f);
            Assert.IsTrue(projectile.IsActive);
            Assert.IsFalse(projectile.Seeking);
            Assert.IsNull(projectile.Target);
            Assert.AreEqual(0, projectile.PierceRemaining);

            for (var i = 0; i < 20 && projectile.IsActive && second.Hp >= 100f; i++)
                projectile.Tick(0.05f, living);

            Assert.AreEqual(90f, second.Hp, 1e-3f);
            Assert.IsFalse(projectile.IsActive);
        }

        [Test]
        public void InfinitePierce_ContinuesThroughAllEnemies()
        {
            var first = MakeEnemyAt(Vector3.zero, 100f);
            var second = MakeEnemyAt(new Vector3(2f, 0f, 0f), 100f);
            var third = MakeEnemyAt(new Vector3(4f, 0f, 0f), 100f);
            var living = Living(first, second, third);

            var projectile = new ProjectileRuntime();
            projectile.Init(
                origin: new Vector3(-0.5f, 0f, 0f),
                direction: Vector3.right,
                target: first,
                damage: 10f,
                chainCount: 0,
                speed: 50f,
                chainRange: 0f,
                pierceRemaining: ProjectileRuntime.InfinitePierceRemaining);

            Assert.IsTrue(projectile.Tick(0.05f, living));
            Assert.IsTrue(projectile.Tick(0.05f, living));
            Assert.IsTrue(projectile.Tick(0.05f, living));

            Assert.AreEqual(90f, first.Hp, 1e-3f);
            Assert.AreEqual(90f, second.Hp, 1e-3f);
            Assert.AreEqual(90f, third.Hp, 1e-3f);
            Assert.AreEqual(ProjectileRuntime.InfinitePierceRemaining, projectile.PierceRemaining);
            Assert.IsTrue(projectile.IsActive);
        }

        [Test]
        public void Pierce_ZeroRemaining_ExpiresOnHit()
        {
            var hit = MakeEnemyAt(Vector3.zero, 100f);
            var living = Living(hit);

            var projectile = new ProjectileRuntime();
            projectile.Init(
                origin: Vector3.zero,
                direction: Vector3.right,
                target: hit,
                damage: 10f,
                chainCount: 0,
                speed: 100f,
                chainRange: 0f,
                pierceRemaining: 0);

            Assert.IsFalse(projectile.Tick(0.05f, living));
            Assert.IsFalse(projectile.IsActive);
            Assert.AreEqual(0, projectile.PierceRemaining);
            Assert.AreEqual(90f, hit.Hp, 1e-3f);
        }

        [Test]
        public void LifetimeControlsReachWithoutFixedDistanceCap()
        {
            var projectile = new ProjectileRuntime();
            projectile.Init(
                origin: Vector3.zero,
                direction: Vector3.right,
                target: null,
                damage: 10f,
                chainCount: 0,
                speed: 100f,
                chainRange: 0f);

            Assert.IsTrue(projectile.Tick(1.9f, null));
            Assert.AreEqual(190f, projectile.Position.x, 1e-3f);
            Assert.IsFalse(projectile.Tick(0.1f, null));
            Assert.IsFalse(projectile.IsActive);
        }

        [Test]
        public void NonSeeking_ExpiresAfterMaxLifetime()
        {
            var projectile = new ProjectileRuntime();
            projectile.Init(
                origin: Vector3.zero,
                direction: Vector3.right,
                target: null,
                damage: 10f,
                chainCount: 0,
                speed: 10f,
                chainRange: 0f);

            Assert.IsTrue(projectile.Tick(ProjectileRuntime.MaxLifetimeSeconds - 0.01f, null));
            Assert.IsTrue(projectile.IsActive);
            Assert.IsFalse(projectile.Tick(0.02f, null));
            Assert.IsFalse(projectile.IsActive);
        }

        [Test]
        public void Bleed_ChanceOne_AppliesBleedDot()
        {
            var enemy = MakeEnemyAt(Vector3.zero, 100f);
            var statuses = new StatusRuntime();
            var projectile = new ProjectileRuntime();
            projectile.Init(
                origin: enemy.WorldPosition,
                direction: Vector3.right,
                target: enemy,
                damage: 10f,
                chainCount: 0,
                speed: 100f,
                chainRange: 0f,
                statuses: statuses,
                bleedChance: 1f,
                bleedDamageMultiplier: 1.19f);

            Assert.IsFalse(projectile.Tick(0.05f, Living(enemy)));
            Assert.AreEqual(90f, enemy.Hp, 1e-3f);
            Assert.IsTrue(statuses.Has(enemy, StatusId.Bleed));
            statuses.Tick(ProjectileRuntime.BleedDuration, Living(enemy));
            Assert.AreEqual(
                90f - 10f * ProjectileRuntime.BleedHitFraction * 1.19f,
                enemy.Hp,
                0.5f);
        }

        [Test]
        public void Ignite_ChanceOneWithoutFlag_AppliesWithAuthoredDuration()
        {
            var enemy = MakeEnemyAt(Vector3.zero, 100f);
            var statuses = new StatusRuntime();
            var projectile = new ProjectileRuntime();
            projectile.Init(
                origin: enemy.WorldPosition,
                direction: Vector3.right,
                target: enemy,
                damage: 10f,
                chainCount: 0,
                speed: 100f,
                chainRange: 0f,
                statuses: statuses,
                ailments: new AilmentTune
                {
                    IgniteChance = 1f,
                    IgniteDuration = 1f
                });

            Assert.IsFalse(projectile.Tick(0.05f, Living(enemy)));
            Assert.AreEqual(90f, enemy.Hp, 1e-3f);
            Assert.IsTrue(statuses.Has(enemy, StatusId.Ignite));
            statuses.Tick(1f, Living(enemy));
            Assert.AreEqual(90f - 10f * ProjectileRuntime.IgniteHitFraction, enemy.Hp, 0.5f);
        }

        [Test]
        public void BurningDamage_ScalesIgniteDot()
        {
            var enemy = MakeEnemyAt(Vector3.zero, 100f);
            var statuses = new StatusRuntime();
            var projectile = new ProjectileRuntime();
            projectile.Init(
                origin: enemy.WorldPosition,
                direction: Vector3.right,
                target: enemy,
                damage: 10f,
                chainCount: 0,
                speed: 100f,
                chainRange: 0f,
                ignite: true,
                statuses: statuses,
                ailments: new AilmentTune
                {
                    Ignite = true,
                    BurningDamageMultiplier = 1.5f
                });

            Assert.IsFalse(projectile.Tick(0.05f, Living(enemy)));
            Assert.AreEqual(90f, enemy.Hp, 1e-3f);
            statuses.Tick(ProjectileRuntime.IgniteDuration, Living(enemy));
            Assert.AreEqual(
                90f - 10f * ProjectileRuntime.IgniteHitFraction * 1.5f,
                enemy.Hp,
                0.5f);
        }

        [Test]
        public void Knockback_ChanceOne_RewindsLivingEnemyAlongPath()
        {
            _def.MoveSpeed = 2f;
            var enemy = MakeEnemyAt(Vector3.zero, 100f);
            Assert.IsFalse(enemy.TickMove(0.25f));
            var before = enemy.Progress;
            Assert.Greater(before, 0.4f);

            var projectile = new ProjectileRuntime();
            projectile.Init(
                origin: enemy.WorldPosition,
                direction: Vector3.right,
                target: enemy,
                damage: 10f,
                chainCount: 0,
                speed: 100f,
                chainRange: 0f,
                knockbackChance: 1f,
                knockbackDistance: 0.25f);

            Assert.IsFalse(projectile.Tick(0.05f, Living(enemy)));
            Assert.AreEqual(90f, enemy.Hp, 1e-3f);
            Assert.AreEqual(before - 0.25f, enemy.Progress, 1e-3f);
        }

        [Test]
        public void PhysAsExtraFire_AddsToHitWhenNotHallow()
        {
            var enemy = MakeEnemyAt(Vector3.zero, 100f);
            var projectile = new ProjectileRuntime();
            projectile.Init(
                origin: enemy.WorldPosition,
                direction: Vector3.right,
                target: enemy,
                damage: 10f,
                chainCount: 0,
                speed: 100f,
                chainRange: 0f,
                ailments: new AilmentTune { PhysAsExtraFire = 0.25f });

            Assert.IsFalse(projectile.Tick(0.05f, Living(enemy)));
            Assert.AreEqual(87.5f, enemy.Hp, 1e-3f);
        }

        [Test]
        public void HallowingFlame_OtherTowerConsumesExtra_InflictorDoesNot()
        {
            var inflictor = ScriptableObject.CreateInstance<TowerDefinition>();
            var other = ScriptableObject.CreateInstance<TowerDefinition>();
            try
            {
                var enemy = MakeEnemyAt(Vector3.zero, 100f);
                var statuses = new StatusRuntime();
                var mark = new ProjectileRuntime();
                mark.Init(
                    origin: enemy.WorldPosition,
                    direction: Vector3.right,
                    target: enemy,
                    damage: 10f,
                    chainCount: 0,
                    speed: 100f,
                    chainRange: 0f,
                    statuses: statuses,
                    sourceTower: inflictor,
                    ailments: new AilmentTune
                    {
                        HallowingFlame = true,
                        PhysAsExtraFire = 0.29f
                    });
                Assert.IsFalse(mark.Tick(0.05f, Living(enemy)));
                Assert.AreEqual(90f, enemy.Hp, 1e-3f);
                Assert.IsTrue(statuses.Has(enemy, StatusId.HallowingFlame));

                var secondFromInflictor = new ProjectileRuntime();
                secondFromInflictor.Init(
                    origin: enemy.WorldPosition,
                    direction: Vector3.right,
                    target: enemy,
                    damage: 10f,
                    chainCount: 0,
                    speed: 100f,
                    chainRange: 0f,
                    statuses: statuses,
                    sourceTower: inflictor);
                Assert.IsFalse(secondFromInflictor.Tick(0.05f, Living(enemy)));
                Assert.AreEqual(80f, enemy.Hp, 1e-3f);
                Assert.IsTrue(statuses.Has(enemy, StatusId.HallowingFlame));

                var beneficiary = new ProjectileRuntime();
                beneficiary.Init(
                    origin: enemy.WorldPosition,
                    direction: Vector3.right,
                    target: enemy,
                    damage: 10f,
                    chainCount: 0,
                    speed: 100f,
                    chainRange: 0f,
                    statuses: statuses,
                    sourceTower: other);
                Assert.IsFalse(beneficiary.Tick(0.05f, Living(enemy)));
                Assert.AreEqual(80f - 10f - 10f * 0.29f, enemy.Hp, 1e-3f);
                Assert.IsFalse(statuses.Has(enemy, StatusId.HallowingFlame));
            }
            finally
            {
                Object.DestroyImmediate(inflictor);
                Object.DestroyImmediate(other);
            }
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
