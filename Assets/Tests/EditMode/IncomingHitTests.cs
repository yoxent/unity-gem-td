using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;

namespace GemTD.Tests.EditMode
{
    public sealed class IncomingHitTests
    {
        EnemyDefinition _def;
        EnemyRuntime _enemy;
        StatusRuntime _statuses;

        [SetUp]
        public void SetUp()
        {
            _def = ScriptableObject.CreateInstance<EnemyDefinition>();
            _def.MaxHealth = 1000f;
            _enemy = new EnemyRuntime();
            _enemy.Init(_def, new List<Vector3> { Vector3.zero, Vector3.right });
            _statuses = new StatusRuntime();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_def);
        }

        [Test]
        public void EmptyMix_UsesFlatArmor()
        {
            _def.Armor = 5;
            _enemy.Init(_def, new List<Vector3> { Vector3.zero, Vector3.right });
            var remaining = IncomingHit.Mitigate(10f, default, _enemy, null);
            Assert.AreEqual(5f, remaining, 1e-4f);
        }

        [Test]
        public void HundredFire_NoResist_IsFull()
        {
            var spec = Mix(DamageType.Fire, 100);
            var remaining = IncomingHit.Mitigate(10f, spec, _enemy, null);
            Assert.AreEqual(10f, remaining, 1e-4f);
        }

        [Test]
        public void HundredFire_FlammabilityMinusThirty_TakesMore()
        {
            _statuses.Apply(_enemy, StatusId.CurseFlammability, 10f, -30f);
            var spec = Mix(DamageType.Fire, 100);
            var remaining = IncomingHit.Mitigate(10f, spec, _enemy, _statuses);
            Assert.AreEqual(13f, remaining, 1e-4f);
        }

        [Test]
        public void HundredFire_AuthoredResistTwenty_TakesLess()
        {
            _def.FireResistance = 20;
            _enemy.Init(_def, new List<Vector3> { Vector3.zero, Vector3.right });
            var spec = Mix(DamageType.Fire, 100);
            var remaining = IncomingHit.Mitigate(10f, spec, _enemy, null);
            Assert.AreEqual(8f, remaining, 1e-4f);
        }

        [Test]
        public void ResistCeiling_IsNinetyPercent()
        {
            _def.FireResistance = 99;
            _enemy.Init(_def, new List<Vector3> { Vector3.zero, Vector3.right });
            var spec = Mix(DamageType.Fire, 100);
            var remaining = IncomingHit.Mitigate(10f, spec, _enemy, null);
            Assert.AreEqual(1f, remaining, 1e-4f);
        }

        [Test]
        public void ResistFloor_IsMinusTwoHundredPercent()
        {
            _statuses.Apply(_enemy, StatusId.CurseFlammability, 10f, -250f);
            var spec = Mix(DamageType.Fire, 100);
            var remaining = IncomingHit.Mitigate(10f, spec, _enemy, _statuses);
            Assert.AreEqual(30f, remaining, 1e-4f);
        }

        [Test]
        public void Physical_ArmourFormula_NotFlatSubtract()
        {
            _def.Armor = 5;
            _enemy.Init(_def, new List<Vector3> { Vector3.zero, Vector3.right });
            var spec = Mix(DamageType.Physical, 100);
            var remaining = IncomingHit.Mitigate(10f, spec, _enemy, null);
            var expected = 10f * (1f - 5f / (5f + 5f * 10f));
            Assert.AreEqual(expected, remaining, 1e-4f);
            Assert.Greater(remaining, 5f);
        }

        [Test]
        public void Vulnerability_AfterArmour_TakesMorePhysical()
        {
            _def.Armor = 5;
            _enemy.Init(_def, new List<Vector3> { Vector3.zero, Vector3.right });
            _statuses.Apply(_enemy, StatusId.CurseVulnerability, 10f, 27f);
            var spec = Mix(DamageType.Physical, 100);
            var afterArmour = 10f * (1f - 5f / (5f + 5f * 10f));
            var remaining = IncomingHit.Mitigate(10f, spec, _enemy, _statuses);
            Assert.AreEqual(afterArmour * 1.27f, remaining, 1e-4f);
        }

        [Test]
        public void Chaos_TimesOnePointTwo_WhileShieldLive()
        {
            _def.ShieldMax = 20f;
            _enemy.Init(_def, new List<Vector3> { Vector3.zero, Vector3.right });
            var spec = Mix(DamageType.Chaos, 100);
            var remaining = IncomingHit.Mitigate(10f, spec, _enemy, null);
            Assert.AreEqual(12f, remaining, 1e-4f);
        }

        [Test]
        public void Chaos_NoShieldMultiplier_WhenShieldEmpty()
        {
            var spec = Mix(DamageType.Chaos, 100);
            var remaining = IncomingHit.Mitigate(10f, spec, _enemy, null);
            Assert.AreEqual(10f, remaining, 1e-4f);
        }

        [Test]
        public void ApplyDamage_TypedFire_PaysMitigatedAmount()
        {
            _def.MaxHealth = 40f;
            _enemy.Init(_def, new List<Vector3> { Vector3.zero, Vector3.right });
            _statuses.Apply(_enemy, StatusId.CurseFlammability, 10f, -30f);
            _enemy.ApplyDamage(10f, Mix(DamageType.Fire, 100), _statuses);
            Assert.AreEqual(27f, _enemy.Hp, 1e-4f);
        }

        static SkillSpec Mix(DamageType type, int percent)
        {
            var shares = new[] { new DamageTypeShare { Type = type, Percent = percent } };
            var spec = SkillSpec.FromBase(10f);
            DamageMix.ToFractions(
                shares,
                out spec.MixPhysical,
                out spec.MixFire,
                out spec.MixCold,
                out spec.MixLightning,
                out spec.MixChaos);
            return spec;
        }
    }
}
