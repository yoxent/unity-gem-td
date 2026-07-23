using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class BeaconAuraSystemTests
    {
        const float CellSize = 1f;

        EnemyDefinition _enemyDef;
        TowerDefinition _beaconDef;
        TowerDefinition _allyDef;
        BeaconAuraSystem _aura;

        [SetUp]
        public void SetUp()
        {
            _enemyDef = ScriptableObject.CreateInstance<EnemyDefinition>();
            _enemyDef.MaxHealth = 100f;
            _enemyDef.MoveSpeed = 1f;

            _beaconDef = ScriptableObject.CreateInstance<TowerDefinition>();
            _beaconDef.Kind = TowerKind.Aura;
            _beaconDef.AllyAuraRadius = 3f;
            _beaconDef.AllyDamageMultiplier = 1.25f;
            _beaconDef.EnemyAuraRadius = 3f;
            _beaconDef.EnemySlowMultiplier = 0.7f;

            _allyDef = ScriptableObject.CreateInstance<TowerDefinition>();
            _allyDef.Kind = TowerKind.Projectile;
            _allyDef.Damage = 10f;

            _aura = new BeaconAuraSystem();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemyDef);
            Object.DestroyImmediate(_beaconDef);
            Object.DestroyImmediate(_allyDef);
        }

        [Test]
        public void AllyInRange_GetsOutgoingDamageMultiplier125()
        {
            var beacon = new TowerRuntime(new Vector2Int(0, 0), _beaconDef);
            var ally = new TowerRuntime(new Vector2Int(2, 0), _allyDef);
            var outOfRange = new TowerRuntime(new Vector2Int(5, 0), _allyDef);
            var towers = new List<TowerRuntime> { beacon, ally, outOfRange };

            _aura.Tick(towers, null, CellSize);

            Assert.AreEqual(1.25f, ally.OutgoingDamageMultiplier, 1e-4f);
            Assert.AreEqual(1f, outOfRange.OutgoingDamageMultiplier, 1e-4f);
        }

        [Test]
        public void EnemyInRange_GetsMoveSpeedMultiplier07()
        {
            var beacon = new TowerRuntime(new Vector2Int(0, 0), _beaconDef);
            var inRange = CreateEnemyAtCell(new Vector2Int(2, 0));
            var outOfRange = CreateEnemyAtCell(new Vector2Int(5, 0));
            var registry = new EnemyRegistry();
            registry.Register(inRange);
            registry.Register(outOfRange);

            _aura.Tick(new List<TowerRuntime> { beacon }, registry, CellSize);

            Assert.AreEqual(0.7f, inRange.MoveSpeedMultiplier, 1e-4f);
            Assert.AreEqual(1f, outOfRange.MoveSpeedMultiplier, 1e-4f);
        }

        [Test]
        public void OutOfRange_Unaffected_MultipliersStayOne()
        {
            var beacon = new TowerRuntime(new Vector2Int(0, 0), _beaconDef);
            var ally = new TowerRuntime(new Vector2Int(6, 0), _allyDef);
            var enemy = CreateEnemyAtCell(new Vector2Int(6, 0));
            var registry = new EnemyRegistry();
            registry.Register(enemy);

            _aura.Tick(new List<TowerRuntime> { beacon, ally }, registry, CellSize);

            Assert.AreEqual(1f, ally.OutgoingDamageMultiplier, 1e-4f);
            Assert.AreEqual(1f, enemy.MoveSpeedMultiplier, 1e-4f);
        }

        EnemyRuntime CreateEnemyAtCell(Vector2Int cell)
        {
            var half = CellSize * 0.5f;
            var pos = new Vector3(cell.x * CellSize + half, 0f, cell.y * CellSize + half);
            var waypoints = new List<Vector3> { pos, pos + Vector3.right * 10f };
            var enemy = new EnemyRuntime();
            enemy.Init(_enemyDef, waypoints);
            return enemy;
        }
    }
}
