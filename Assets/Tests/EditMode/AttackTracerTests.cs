using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.SkillLab;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class AttackTracerTests
    {
        EnemyDefinition _enemyDef;
        TowerDefinition _projectileTower;
        TowerDefinition _splashTower;
        AttackRoleDefinition _projectileRole;
        AttackRoleDefinition _splashRole;
        GemDefinition _multipleProjectiles;
        GemDefinition _chain;
        GemDefinition _fork;
        GemDefinition _pierce;
        GemDefinition _area;

        [SetUp]
        public void SetUp()
        {
            _enemyDef = ScriptableObject.CreateInstance<EnemyDefinition>();
            _enemyDef.MaxHealth = 1000f;
            _enemyDef.MoveSpeed = 0.01f;

            _projectileTower = ScriptableObject.CreateInstance<TowerDefinition>();
            _projectileTower.DisplayName = "Projectile Tower";
            _projectileRole = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            _projectileRole.Modifiers = new[]
            {
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.ProjectileCount, 1f)
            };
            _projectileTower.Roles = new TowerRoleDefinition[] { _projectileRole };
            _projectileTower.Tags = GemTag.Attack | GemTag.Projectile;
            _projectileTower.AllowsHydraEvolution = true;
            _projectileTower.SocketCount = 3;
            _projectileTower.Damage = 10f;

            _splashTower = ScriptableObject.CreateInstance<TowerDefinition>();
            _splashTower.DisplayName = "Splash Tower";
            _splashRole = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            _splashRole.Modifiers = new[]
            {
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.SplashRadius, 1.5f),
                Modifier(RoleStat.ProjectileCount, 1f)
            };
            _splashTower.Roles = new TowerRoleDefinition[] { _splashRole };
            _splashTower.Tags = GemTag.Attack | GemTag.Projectile | GemTag.Aoe;
            _splashTower.SocketCount = 3;
            _splashTower.Damage = 8f;

            _multipleProjectiles = ScriptableObject.CreateInstance<GemDefinition>();
            _multipleProjectiles.Id = GemId.MultipleProjectiles;
            CatalogGemModifiers.Bind(_multipleProjectiles);
            _chain = ScriptableObject.CreateInstance<GemDefinition>();
            _chain.Id = GemId.Chain;
            CatalogGemModifiers.Bind(_chain);
            _fork = ScriptableObject.CreateInstance<GemDefinition>();
            _fork.Id = GemId.Fork;
            CatalogGemModifiers.Bind(_fork);
            _pierce = ScriptableObject.CreateInstance<GemDefinition>();
            _pierce.Id = GemId.Pierce;
            CatalogGemModifiers.Bind(_pierce);
            _area = ScriptableObject.CreateInstance<GemDefinition>();
            _area.Id = GemId.IncreasedArea;
            CatalogGemModifiers.Bind(_area);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemyDef);
            Object.DestroyImmediate(_projectileRole);
            Object.DestroyImmediate(_splashRole);
            Object.DestroyImmediate(_projectileTower);
            Object.DestroyImmediate(_splashTower);
            Object.DestroyImmediate(_multipleProjectiles);
            Object.DestroyImmediate(_chain);
            Object.DestroyImmediate(_fork);
            Object.DestroyImmediate(_pierce);
            Object.DestroyImmediate(_area);
        }

        [Test]
        public void Chain_HopsNearest_AndFalloffOnHopSegment()
        {
            var tower = new TowerInstance(Vector2Int.zero, _projectileTower);
            Assert.IsTrue(tower.TrySocket(_chain, 0, true));

            var e1 = MakeEnemy(new Vector3(3f, 0f, 0f));
            var e2 = MakeEnemy(new Vector3(4f, 0f, 0f));
            var e3 = MakeEnemy(new Vector3(5f, 0f, 0f));
            var tracer = new AttackTracer();
            var trace = tracer.Trace(tower, Vector3.zero, Living(e1, e2, e3));

            Assert.IsTrue(trace.HasTarget);
            Assert.AreEqual(1, CountKind(trace, AttackTraceKind.Chain));
            var hop = FirstKind(trace, AttackTraceKind.Chain);
            Assert.AreEqual(10f * 0.7f * ProjectileRuntime.DefaultChainHopFalloff, hop.Damage, 1e-3f);
            Assert.AreEqual(1000f, e1.Hp, 1e-3f);
            Assert.AreEqual(1000f, e2.Hp, 1e-3f);
        }

        [Test]
        public void Fork_TwoChildren_SameCollisionDoesNotChain()
        {
            var tower = new TowerInstance(Vector2Int.zero, _projectileTower);
            Assert.IsTrue(tower.TrySocket(_fork, 0, true));
            Assert.IsTrue(tower.TrySocket(_chain, 1, true));

            var hit = MakeEnemy(new Vector3(3f, 0f, 0f));
            var other = MakeEnemy(new Vector3(4f, 0f, 0f));
            var tracer = new AttackTracer();
            var trace = tracer.Trace(tower, Vector3.zero, Living(hit, other));

            Assert.AreEqual(2, CountKind(trace, AttackTraceKind.Fork));
            Assert.AreEqual(0, CountKind(trace, AttackTraceKind.Chain));
        }

        [Test]
        public void Pierce_ContinuesWithoutForkOnSameHit()
        {
            var tower = new TowerInstance(Vector2Int.zero, _projectileTower);
            Assert.IsTrue(tower.TrySocket(_pierce, 0, true));
            Assert.IsTrue(tower.TrySocket(_fork, 1, true));

            var first = MakeEnemy(new Vector3(2f, 0f, 0f));
            var second = MakeEnemy(new Vector3(4f, 0f, 0f));
            var third = MakeEnemy(new Vector3(6f, 0f, 0f));
            var tracer = new AttackTracer();
            var trace = tracer.Trace(tower, Vector3.zero, Living(first, second, third));

            Assert.AreEqual(1, CountKind(trace, AttackTraceKind.Pierce));
            Assert.AreEqual(2, CountKind(trace, AttackTraceKind.Fork));
        }

        [Test]
        public void SplashTowerIncreasedArea_RecordsAoeDisc()
        {
            var tower = new TowerInstance(Vector2Int.zero, _splashTower);
            Assert.IsTrue(tower.TrySocket(_area, 0, true));
            var dummy = MakeEnemy(new Vector3(3f, 0f, 0f));
            var tracer = new AttackTracer();
            var trace = tracer.Trace(tower, Vector3.zero, Living(dummy));

            Assert.IsTrue(trace.HasTarget);
            Assert.GreaterOrEqual(trace.Discs.Count, 1);
            Assert.Greater(trace.Discs[0].Radius, 1.5f);
            Assert.AreEqual(AttackTraceKind.Aoe, trace.Discs[0].Kind);
        }

        [Test]
        public void RoleSplash_UsesRoleValueInTrace()
        {
            _splashRole.Modifiers = new[]
            {
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.SplashRadius, 2f),
                Modifier(RoleStat.ProjectileCount, 1f)
            };
            var tower = new TowerInstance(Vector2Int.zero, _splashTower);
            var dummy = MakeEnemy(new Vector3(3f, 0f, 0f));
            var tracer = new AttackTracer();
            var trace = tracer.Trace(tower, Vector3.zero, Living(dummy));

            Assert.IsTrue(trace.HasTarget);
            Assert.AreEqual(2f, trace.Discs[0].Radius, 0.001f);
        }

        [Test]
        public void Straight_Hit_RecordsTargetForSkillLabFeedback()
        {
            var tower = new TowerInstance(Vector2Int.zero, _projectileTower);
            var dummy = MakeEnemy(new Vector3(3f, 0f, 0f));
            var tracer = new AttackTracer();
            var trace = tracer.Trace(tower, Vector3.zero, Living(dummy));

            Assert.IsTrue(trace.HasTarget);
            Assert.AreEqual(1, trace.HitTargets.Count);
            Assert.AreSame(dummy, trace.HitTargets[0]);
        }

        [Test]
        public void CasterNova_DiscUsesTowerRadiusNotSplash()
        {
            _projectileRole.AimMode = AimMode.Direct;
            _projectileRole.DeliveryPattern = DeliveryPattern.CasterNova;
            _projectileRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 5f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.SplashRadius, 2.6f)
            };
            _projectileTower.Tags = GemTag.Spell | GemTag.Aoe;

            var dummy = MakeEnemy(new Vector3(1.5f, 0f, 0f));
            var trace = new AttackTracer().Trace(
                new TowerInstance(Vector2Int.zero, _projectileTower),
                Vector3.zero,
                Living(dummy));

            Assert.IsTrue(trace.HasTarget);
            Assert.AreEqual(1, trace.Discs.Count);
            Assert.AreEqual(5f, trace.Discs[0].Radius, 0.001f);
            Assert.AreEqual(AttackTraceKind.Aoe, trace.Discs[0].Kind);
        }

        [Test]
        public void Curse_TracesCasterNovaWithoutPrimaryInTargetRange()
        {
            var curseRole = ScriptableObject.CreateInstance<CurseRoleDefinition>();
            curseRole.AimMode = AimMode.Direct;
            curseRole.DeliveryPattern = DeliveryPattern.CasterNova;
            curseRole.Modifiers = new[] { Modifier(RoleStat.TowerRadius, 3f) };
            var curseTower = ScriptableObject.CreateInstance<TowerDefinition>();
            curseTower.Roles = new TowerRoleDefinition[] { curseRole };

            try
            {
                var dummy = MakeEnemy(new Vector3(20f, 0f, 0f));
                var trace = new AttackTracer().Trace(
                    new TowerInstance(Vector2Int.zero, curseTower),
                    Vector3.zero,
                    Living(dummy),
                    payloadRng: null,
                    includeRandomPayloads: false);

                Assert.IsTrue(trace.HasTarget);
                Assert.AreEqual(1, trace.Discs.Count);
                Assert.AreEqual(3f, trace.Discs[0].Radius, 0.001f);
                Assert.AreEqual(0, trace.HitTargets.Count);
            }
            finally
            {
                Object.DestroyImmediate(curseRole);
                Object.DestroyImmediate(curseTower);
            }
        }

        [Test]
        public void GroundPulse_SplashUsesAimPointNotOrigin()
        {
            _projectileRole.AimMode = AimMode.Ground;
            _projectileRole.DeliveryPattern = DeliveryPattern.GroundPulse;
            _projectileRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.SplashRadius, 2f)
            };
            _projectileTower.Tags = GemTag.Attack | GemTag.Melee | GemTag.Slam | GemTag.Aoe;

            var primary = MakeEnemy(new Vector3(8f, 0f, 0f));
            var atOrigin = MakeEnemy(Vector3.zero);
            var trace = new AttackTracer().Trace(
                new TowerInstance(Vector2Int.zero, _projectileTower),
                Vector3.zero,
                Living(primary, atOrigin));

            Assert.IsTrue(trace.HasTarget);
            Assert.AreEqual(1, trace.HitTargets.Count);
            Assert.AreSame(primary, trace.HitTargets[0]);
        }

        [Test]
        public void GroundPulse_RecordsSlamDiscAndAftershockDisc()
        {
            _projectileRole.AimMode = AimMode.Ground;
            _projectileRole.DeliveryPattern = DeliveryPattern.GroundPulse;
            _projectileRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.SplashRadius, SkillGemTowerMap.EarthquakeSlamRadius)
            };
            _projectileRole.EffectPayloads = new[]
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
                    DamageMultiplier = SkillGemTowerMap.EarthquakeAftershockDamageMultiplier,
                    AoeRadius = SkillGemTowerMap.EarthquakeAftershockRadius,
                    DelaySeconds = SkillGemTowerMap.EarthquakeAftershockDelaySeconds
                }
            };
            _projectileTower.Tags = GemTag.Attack | GemTag.Melee | GemTag.Slam | GemTag.Aoe;

            var primary = MakeEnemy(new Vector3(4f, 0f, 0f));
            var trace = new AttackTracer().Trace(
                new TowerInstance(Vector2Int.zero, _projectileTower),
                Vector3.zero,
                Living(primary),
                payloadRng: null,
                includeRandomPayloads: false);

            Assert.IsTrue(trace.HasTarget);
            Assert.GreaterOrEqual(trace.Discs.Count, 2);
            Assert.AreEqual(SkillGemTowerMap.EarthquakeSlamRadius, trace.Discs[0].Radius, 0.001f);
            Assert.AreEqual(AttackTraceKind.Aoe, trace.Discs[0].Kind);
            Assert.AreEqual(SkillGemTowerMap.EarthquakeAftershockRadius, trace.Discs[1].Radius, 0.001f);
            Assert.AreEqual(AttackTraceKind.Aftershock, trace.Discs[1].Kind);
        }

        [Test]
        public void GroundAim_UsesPredictedAimPointForProjectileDirection()
        {
            _enemyDef.MoveSpeed = 4f;
            _projectileRole.AimMode = AimMode.Ground;
            _projectileRole.DeliveryPattern = DeliveryPattern.Straight;
            _projectileRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.ProjectileCount, 1f)
            };

            var enemy = new EnemyRuntime();
            enemy.Init(
                _enemyDef,
                new[]
                {
                    new Vector3(4f, 0f, 0f),
                    new Vector3(8f, 0f, 4f)
                });
            var trace = new AttackTracer().Trace(
                new TowerInstance(Vector2Int.zero, _projectileTower),
                Vector3.zero,
                Living(enemy));

            var intercept = PathIntercept.Predict(Vector3.zero, ProjectileRuntime.DefaultProjectileSpeed, enemy);
            Assert.IsTrue(trace.HasTarget);
            Assert.Greater(trace.Segments.Count, 0);
            var direction = trace.Segments[0].To.normalized;
            Assert.AreEqual(intercept.normalized.x, direction.x, 1e-3f);
            Assert.AreEqual(intercept.normalized.z, direction.z, 1e-3f);
        }

        [Test]
        public void Rain_RecordsPayloadHitsAtAim()
        {
            _projectileRole.AimMode = AimMode.Ground;
            _projectileRole.DeliveryPattern = DeliveryPattern.Rain;
            _projectileRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f)
            };
            _projectileRole.EffectPayloads = new[]
            {
                new EffectPayloadDefinition
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
                    IntervalSeconds = 0.15f
                }
            };
            _projectileTower.Tags = GemTag.Spell | GemTag.Aoe;

            var dummy = MakeEnemy(new Vector3(4f, 0f, 0f));
            var trace = new AttackTracer().Trace(
                new TowerInstance(Vector2Int.zero, _projectileTower),
                Vector3.zero,
                Living(dummy));

            Assert.IsTrue(trace.HasTarget);
            Assert.Greater(trace.PayloadHitRecords.Count, 0);
            Assert.AreEqual(10, CountKind(trace, AttackTraceKind.Rain));
            Assert.AreEqual(10, CountDiscKind(trace, AttackTraceKind.Rain));
            Assert.AreEqual(1, CountDiscKind(trace, AttackTraceKind.Aoe));

            var storm = FirstDiscKind(trace, AttackTraceKind.Aoe);
            Assert.AreEqual(2.5f, storm.Radius, 0.001f);
            Assert.AreEqual(4f, storm.Center.x, 0.2f);
            Assert.AreEqual(0f, storm.Center.z, 0.2f);

            var explosion = FirstDiscKind(trace, AttackTraceKind.Rain);
            Assert.AreEqual(1.3f, explosion.Radius, 0.001f);

            var fall = FirstKind(trace, AttackTraceKind.Rain);
            Assert.Greater(fall.From.y, fall.To.y);
            Assert.AreEqual(fall.From.x, fall.To.x, 1e-4f);
            Assert.AreEqual(fall.From.z, fall.To.z, 1e-4f);
        }

        [Test]
        public void PayloadNova_RecordsPayloadTravelAndRadialVolley()
        {
            _projectileRole.AimMode = AimMode.Direct;
            _projectileRole.DeliveryPattern = DeliveryPattern.PayloadNova;
            _projectileRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.Damage, 10f),
                Modifier(RoleStat.ProjectileCount, 4f)
            };

            var primary = MakeEnemy(new Vector3(4f, 0f, 0f));
            var trace = new AttackTracer().Trace(
                new TowerInstance(Vector2Int.zero, _projectileTower),
                Vector3.zero,
                Living(primary));

            Assert.IsTrue(trace.HasTarget);
            Assert.AreEqual(5, trace.Segments.Count);
            Assert.AreEqual(Vector3.zero, trace.Segments[0].From);
            Assert.AreEqual(primary.WorldPosition, trace.Segments[0].To);
        }

        [Test]
        public void WarpStrike_RecordsCueAndMagmaHits()
        {
            _projectileTower.Tags = GemTag.Attack | GemTag.Melee | GemTag.Strike;
            _projectileRole.AimMode = AimMode.Direct;
            _projectileRole.DeliveryPattern = DeliveryPattern.WarpStrike;
            _projectileRole.Modifiers = new[]
            {
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.Damage, 10f)
            };
            _projectileRole.EffectPayloads = new[]
            {
                new EffectPayloadDefinition
                {
                    Trigger = EffectPayloadTrigger.OnImpact,
                    Anchor = EffectPayloadAnchor.PrimaryTarget,
                    TravelPattern = EffectPayloadTravelPattern.Fountain,
                    ScatterPattern = EffectPayloadScatterPattern.FixedRadial,
                    HitPolicy = EffectPayloadHitPolicy.PerImpact,
                    Count = 4,
                    DamageMultiplier = 0.4f,
                    AoeRadius = 1f,
                    MinDistance = 2f,
                    MaxDistance = 2f
                }
            };

            var primary = MakeEnemy(new Vector3(4f, 0f, 0f));
            var magmaHit = MakeEnemy(new Vector3(4f, 0f, 2f));
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
                    AoeRadius = 0.6f,
                    MinDistance = 0.5f,
                    MaxDistance = 0.5f
                }
            };

            try
            {
                var tower = new TowerInstance(Vector2Int.zero, _projectileTower);
                var roleOnlyTrace = new AttackTracer().Trace(
                    tower,
                    Vector3.zero,
                    Living(primary, magmaHit));
                var roleOnlyMagmaDiscCount = CountDiscKind(roleOnlyTrace, AttackTraceKind.Magma);
                var roleOnlyPayloadHitCount = roleOnlyTrace.PayloadHitRecords.Count;

                Assert.IsTrue(tower.TrySocket(gem, 0, allowSocket: true));
                var trace = new AttackTracer().Trace(
                    tower,
                    Vector3.zero,
                    Living(primary, magmaHit));

                Assert.IsTrue(trace.HasTarget);
                Assert.AreEqual(1, CountKind(trace, AttackTraceKind.WarpRise));
                Assert.AreEqual(1, CountKind(trace, AttackTraceKind.WarpDrop));
                Assert.AreEqual(roleOnlyMagmaDiscCount + 1, CountDiscKind(trace, AttackTraceKind.Magma));
                Assert.AreEqual(roleOnlyPayloadHitCount + 1, trace.PayloadHitRecords.Count);
                CollectionAssert.Contains(trace.HitTargets, primary);
                CollectionAssert.Contains(trace.HitTargets, magmaHit);
                Assert.AreEqual(1000f, primary.Hp, 1e-3f);
                Assert.AreEqual(1000f, magmaHit.Hp, 1e-3f);
            }
            finally
            {
                Object.DestroyImmediate(gem);
            }
        }

        [Test]
        public void OutOfRange_HasNoTarget()
        {
            _projectileRole.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 1,
                    Modifiers = new[]
                    {
                        Modifier(RoleStat.TowerRadius, 2f)
                    }
                }
            };
            var tower = new TowerInstance(Vector2Int.zero, _projectileTower);
            var dummy = MakeEnemy(new Vector3(20f, 0f, 0f));
            var tracer = new AttackTracer();
            var trace = tracer.Trace(tower, Vector3.zero, Living(dummy));
            Assert.IsFalse(trace.HasTarget);
            Assert.AreEqual(0, trace.Segments.Count);
        }

        [Test]
        public void RoleProjectileSpeed_ControlsTraceReachThroughLifetime()
        {
            _projectileRole.Modifiers = new[]
            {
                Modifier(RoleStat.AttackTime, 1f),
                Modifier(RoleStat.AttackSpeed, 100f),
                Modifier(RoleStat.TowerRadius, 50f),
                Modifier(RoleStat.ProjectileCount, 1f),
                RoleStatModifier.Single(RoleStat.ProjectileSpeed, RoleModifierOperation.Set, 0.5f)
            };

            var tower = new TowerInstance(Vector2Int.zero, _projectileTower);
            var dummy = MakeEnemy(new Vector3(30f, 0f, 0f));
            var tracer = new AttackTracer(baseProjectileSpeed: 20f);
            var trace = tracer.Trace(tower, Vector3.zero, Living(dummy));

            Assert.IsTrue(trace.HasTarget);
            Assert.AreEqual(1, trace.Segments.Count);
            Assert.AreEqual(20f, trace.Segments[0].To.x, 0.001f);
        }

        static RoleStatModifier Modifier(RoleStat stat, float value)
        {
            return RoleStatModifier.Single(stat, RoleModifierOperation.Set, value);
        }

        EnemyRuntime MakeEnemy(Vector3 pos)
        {
            var enemy = new EnemyRuntime();
            enemy.Init(_enemyDef, new[] { pos });
            return enemy;
        }

        static List<EnemyRuntime> Living(params EnemyRuntime[] enemies)
        {
            var list = new List<EnemyRuntime>(enemies.Length);
            for (var i = 0; i < enemies.Length; i++)
                list.Add(enemies[i]);
            return list;
        }

        static int CountKind(AttackTrace trace, AttackTraceKind kind)
        {
            var n = 0;
            for (var i = 0; i < trace.Segments.Count; i++)
            {
                if (trace.Segments[i].Kind == kind)
                    n++;
            }

            return n;
        }

        static int CountDiscKind(AttackTrace trace, AttackTraceKind kind)
        {
            var n = 0;
            for (var i = 0; i < trace.Discs.Count; i++)
            {
                if (trace.Discs[i].Kind == kind)
                    n++;
            }

            return n;
        }

        static AttackTraceSegment FirstKind(AttackTrace trace, AttackTraceKind kind)
        {
            for (var i = 0; i < trace.Segments.Count; i++)
            {
                if (trace.Segments[i].Kind == kind)
                    return trace.Segments[i];
            }

            Assert.Fail("missing kind " + kind);
            return default;
        }

        static AttackTraceDisc FirstDiscKind(AttackTrace trace, AttackTraceKind kind)
        {
            for (var i = 0; i < trace.Discs.Count; i++)
            {
                if (trace.Discs[i].Kind == kind)
                    return trace.Discs[i];
            }

            Assert.Fail("missing disc kind " + kind);
            return default;
        }
    }
}
