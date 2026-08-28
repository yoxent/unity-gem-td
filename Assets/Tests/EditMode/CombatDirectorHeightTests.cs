using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Map;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class CombatDirectorHeightTests
    {
        const float CellSize = 1f;

        EnemyDefinition _enemyDef;
        TowerDefinition _towerDef;
        AttackRoleDefinition _towerRole;
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
                RoleStatModifier.Single(RoleStat.TowerRadius, RoleModifierOperation.Set, 2f),
                RoleStatModifier.Single(RoleStat.AttackTime, RoleModifierOperation.Set, 1f),
                RoleStatModifier.Single(RoleStat.AttackSpeed, RoleModifierOperation.Set, 100f),
                RoleStatModifier.Single(RoleStat.ProjectileCount, RoleModifierOperation.Set, 1f)
            };
            _towerDef.Roles = new TowerRoleDefinition[] { _towerRole };
            _towerDef.Tags = GemTag.Attack | GemTag.Projectile;
            _towerDef.Damage = 10f;
            _pipeline = new GemModifierPipeline();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemyDef);
            Object.DestroyImmediate(_towerRole);
            Object.DestroyImmediate(_towerDef);
        }

        [Test]
        public void Tick_TallestPad_SpawnsFromPadTop()
        {
            var heights = new TileHeightMap(8, 8);
            heights.Set(0, 0, 2);
            var director = new CombatDirector(CellSize, projectileSpeed: 100f, recordDamage: null, heights);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = EnemyAt(new Vector3(0.5f, 0f, 1.5f));
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);

            Assert.AreEqual(1, director.Projectiles.Count);
            Assert.AreEqual(TileHeightVisual.TopY(2), director.Projectiles[0].Position.y, 1e-4f);
        }

        [Test]
        public void Tick_LayerTwo_ReachesEnemyOutsideBaseRadius()
        {
            var heights = new TileHeightMap(8, 8);
            heights.Set(0, 0, 2);
            var director = new CombatDirector(CellSize, projectileSpeed: 100f, recordDamage: null, heights);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            // Base radius 2; layer 2 → 2.6. Enemy at 2.2 from cell center (0.5,0.5).
            var enemy = EnemyAt(new Vector3(2.7f, 0f, 0.5f));
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);

            Assert.AreEqual(1, director.Projectiles.Count);
        }

        [Test]
        public void Tick_LayerZero_DoesNotReachEnemyOutsideBaseRadius()
        {
            var heights = new TileHeightMap(8, 8);
            heights.Set(0, 0, 0);
            var director = new CombatDirector(CellSize, projectileSpeed: 100f, recordDamage: null, heights);
            var tower = new TowerInstance(new Vector2Int(0, 0), _towerDef);
            var enemy = EnemyAt(new Vector3(2.7f, 0f, 0.5f));
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            director.Tick(0.016f, new List<TowerInstance> { tower }, registry, _pipeline);

            Assert.AreEqual(0, director.Projectiles.Count);
        }

        EnemyRuntime EnemyAt(Vector3 world)
        {
            var enemy = new EnemyRuntime();
            enemy.Init(_enemyDef, new List<Vector3> { world, world + Vector3.right * 0.01f });
            return enemy;
        }
    }
}
