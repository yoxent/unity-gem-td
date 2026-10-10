using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;

namespace GemTD.Tests.EditMode
{
    public sealed class StatusRuntimeTests
    {
        EnemyDefinition _def;

        [SetUp]
        public void SetUp()
        {
            _def = ScriptableObject.CreateInstance<EnemyDefinition>();
            _def.Armor = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_def);
        }

        [Test]
        public void Ignite_TicksDamageOverDuration()
        {
            var enemy = MakeEnemy(hp: 100f);
            var statuses = new StatusRuntime();
            statuses.Apply(enemy, StatusId.Ignite, duration: 1f, magnitude: 20f);
            statuses.Tick(0.5f, Living(enemy));
            Assert.Less(enemy.Hp, 100f);
            statuses.Tick(0.5f, Living(enemy));
            Assert.AreEqual(80f, enemy.Hp, 0.5f);
        }

        [Test]
        public void Bleed_TicksDamageOverDuration()
        {
            var enemy = MakeEnemy(hp: 100f);
            var statuses = new StatusRuntime();
            statuses.Apply(enemy, StatusId.Bleed, duration: 1f, magnitude: 20f);
            statuses.Tick(0.5f, Living(enemy));
            Assert.Less(enemy.Hp, 100f);
            statuses.Tick(0.5f, Living(enemy));
            Assert.AreEqual(80f, enemy.Hp, 0.5f);
        }

        [Test]
        public void Poison_TicksDamageOverDuration()
        {
            var enemy = MakeEnemy(hp: 100f);
            var statuses = new StatusRuntime();
            statuses.Apply(enemy, StatusId.Poison, duration: 1f, magnitude: 20f);
            statuses.Tick(0.5f, Living(enemy));
            Assert.Less(enemy.Hp, 100f);
            statuses.Tick(0.5f, Living(enemy));
            Assert.AreEqual(80f, enemy.Hp, 0.5f);
        }

        [Test]
        public void Freeze_StopsMovementWhileActive()
        {
            var enemy = MakeEnemy(hp: 50f);
            enemy.MoveSpeedMultiplier = 1f;
            var statuses = new StatusRuntime();
            statuses.Apply(enemy, StatusId.Freeze, 0.5f, magnitude: 0f);
            statuses.Tick(0f, Living(enemy));
            Assert.AreEqual(0f, enemy.MoveSpeedMultiplier, 0.001f);
            statuses.Tick(0.5f, Living(enemy));
            Assert.AreEqual(1f, enemy.MoveSpeedMultiplier, 0.001f);
        }

        [Test]
        public void Chill_SlowsMoveSpeed()
        {
            var enemy = MakeEnemy(hp: 50f);
            enemy.MoveSpeedMultiplier = 1f;
            var statuses = new StatusRuntime();
            statuses.Apply(enemy, StatusId.Chill, 2f, magnitude: 0.6f);
            statuses.Tick(0f, Living(enemy));
            Assert.AreEqual(0.6f, enemy.MoveSpeedMultiplier, 0.001f);
        }

        [Test]
        public void Chill_ExpiryRestoresNormalMoveSpeed()
        {
            var enemy = MakeEnemy(hp: 50f);
            var statuses = new StatusRuntime();
            statuses.Apply(enemy, StatusId.Chill, 0.5f, magnitude: 0.6f);

            statuses.Tick(0.25f, Living(enemy));
            Assert.AreEqual(0.6f, enemy.MoveSpeedMultiplier, 0.001f);

            statuses.Tick(0.25f, Living(enemy));
            Assert.AreEqual(1f, enemy.MoveSpeedMultiplier, 0.001f);
        }

        [Test]
        public void Shock_AmplifiesDamageTaken()
        {
            var enemy = MakeEnemy(hp: 100f);
            var statuses = new StatusRuntime();
            statuses.Apply(enemy, StatusId.Shock, 3f, magnitude: 1.25f);
            statuses.ApplyDamage(enemy, 40f);
            Assert.AreEqual(50f, 100f - enemy.Hp, 0.001f);
        }

        [Test]
        public void Prolif_SpreadsIgniteChillShockToNearby()
        {
            var src = MakeEnemyAt(Vector3.zero, 50f);
            var near = MakeEnemyAt(new Vector3(1f, 0f, 0f), 50f);
            var far = MakeEnemyAt(new Vector3(10f, 0f, 0f), 50f);
            var statuses = new StatusRuntime();
            statuses.Apply(src, StatusId.Ignite, 2f, 10f);
            statuses.Apply(src, StatusId.Chill, 2f, 0.6f);
            statuses.Apply(src, StatusId.Shock, 2f, 1.25f);
            statuses.ProliferateIgniteChillShock(src, radius: 1.5f, Living(src, near, far));
            Assert.IsTrue(statuses.Has(near, StatusId.Ignite));
            Assert.IsFalse(statuses.Has(far, StatusId.Ignite));
        }

        EnemyRuntime MakeEnemy(float hp)
        {
            return MakeEnemyAt(Vector3.zero, hp);
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
