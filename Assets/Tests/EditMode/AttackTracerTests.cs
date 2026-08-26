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
        public void Hydra_FirstSegments_ThreeHeadsTimesMultipleProjectilesPellets()
        {
            var tower = new TowerInstance(Vector2Int.zero, _projectileTower);
            Assert.IsTrue(tower.TrySocket(_multipleProjectiles, 0, true));
            Assert.IsTrue(tower.TrySocket(_chain, 1, true));
            Assert.IsTrue(tower.TrySocket(_fork, 2, true));
            Assert.IsFalse(EvolutionEvaluator.IsHydraTower(tower));

            var dummy = MakeEnemy(new Vector3(4f, 0f, 0f));
            var tracer = new AttackTracer();
            var trace = tracer.Trace(tower, Vector3.zero, Living(dummy));

            Assert.IsTrue(trace.HasTarget);
            Assert.AreEqual(0, CountKind(trace, AttackTraceKind.HydraHead));
            Assert.AreEqual(1000f, dummy.Hp, 1e-3f);
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
    }
}
