using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Enemies;

namespace GemTD.Tests.EditMode
{
    public sealed class EnemyDamageAbsorbTests
    {
        EnemyDefinition _def;

        [SetUp]
        public void SetUp()
        {
            _def = ScriptableObject.CreateInstance<EnemyDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_def);
        }

        [Test]
        public void ApplyDamage_ArmorReducesDamageToHp()
        {
            _def.MaxHealth = 40f;
            _def.Armor = 5;

            var enemy = CreateEnemy();
            enemy.ApplyDamage(10f);

            Assert.AreEqual(35f, enemy.Hp, 1e-4f);
            Assert.AreEqual(0f, enemy.ShieldHp, 1e-4f);
            Assert.IsTrue(enemy.IsAlive);
        }

        [Test]
        public void ApplyDamage_ShieldAbsorbsBeforeHp()
        {
            _def.MaxHealth = 25f;
            _def.ShieldMax = 20f;

            var enemy = CreateEnemy();
            Assert.AreEqual(20f, enemy.ShieldHp, 1e-4f);

            enemy.ApplyDamage(12f);
            Assert.AreEqual(8f, enemy.ShieldHp, 1e-4f);
            Assert.AreEqual(25f, enemy.Hp, 1e-4f);

            enemy.ApplyDamage(15f);
            Assert.AreEqual(0f, enemy.ShieldHp, 1e-4f);
            Assert.AreEqual(18f, enemy.Hp, 1e-4f);
            Assert.IsTrue(enemy.IsAlive);
        }

        [Test]
        public void ArmoredDefinition_LeakDamageIsReadable()
        {
            _def.LeakDamage = 2;

            Assert.AreEqual(2, _def.LeakDamage);
        }

        EnemyRuntime CreateEnemy()
        {
            var waypoints = new List<Vector3> { Vector3.zero, Vector3.right };
            var enemy = new EnemyRuntime();
            enemy.Init(_def, waypoints);
            return enemy;
        }
    }
}
