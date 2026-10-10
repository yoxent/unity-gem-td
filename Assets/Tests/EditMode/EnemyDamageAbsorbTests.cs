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
        public void ApplyDamage_Invulnerable_StaysAliveAtFullHp()
        {
            _def.MaxHealth = 10f;
            var enemy = CreateEnemy();
            enemy.Invulnerable = true;
            enemy.ApplyDamage(999f);
            Assert.IsTrue(enemy.IsAlive);
            Assert.AreEqual(10f, enemy.Hp, 1e-4f);
        }

        [Test]
        public void Init_HealthScale_SetsHpAndMaxHealthWithoutMutatingDefinition()
        {
            _def.MaxHealth = 20f;
            var waypoints = new List<Vector3> { Vector3.zero, Vector3.right };
            var enemy = new EnemyRuntime();
            enemy.Init(_def, waypoints, 2f);

            Assert.AreEqual(40f, enemy.MaxHealth, 1e-4f);
            Assert.AreEqual(40f, enemy.Hp, 1e-4f);
            Assert.AreEqual(20f, _def.MaxHealth, 1e-4f);
        }

        [Test]
        public void Init_HealthScale_ScalesShieldLinearly_AndArmorWithSqrt()
        {
            _def.MaxHealth = 20f;
            _def.ShieldMax = 20f;
            _def.Armor = 5;
            var waypoints = new List<Vector3> { Vector3.zero, Vector3.right };
            var enemy = new EnemyRuntime();
            enemy.Init(_def, waypoints, 4f);

            Assert.AreEqual(80f, enemy.MaxHealth, 1e-4f);
            Assert.AreEqual(80f, enemy.ShieldMax, 1e-4f);
            Assert.AreEqual(80f, enemy.ShieldHp, 1e-4f);
            Assert.AreEqual(5, enemy.Armor);
            Assert.AreEqual(20f, _def.ShieldMax, 1e-4f);
            Assert.AreEqual(5, _def.Armor);
        }

        [Test]
        public void Init_ArmorSpeedAndResist_FollowWaveScales_AndSpareZeroResist()
        {
            _def.MaxHealth = 20f;
            _def.Armor = 5;
            _def.MoveSpeed = 2f;
            _def.FireResistance = 25;
            _def.ColdResistance = 0;
            var waypoints = new List<Vector3> { Vector3.zero, Vector3.right };
            var enemy = new EnemyRuntime();
            enemy.Init(_def, waypoints, healthScale: 1f, speedScale: 1.25f, armorScale: 2f, resistBonus: 10);

            Assert.AreEqual(10, enemy.Armor);
            Assert.AreEqual(1.25f, enemy.MoveSpeedMultiplier, 1e-4f);
            Assert.AreEqual(35, enemy.FireResistance);
            Assert.AreEqual(0, enemy.ColdResistance);
            Assert.AreEqual(25, _def.FireResistance);
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
