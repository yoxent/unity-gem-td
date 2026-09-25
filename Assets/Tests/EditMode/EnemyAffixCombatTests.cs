using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;

namespace GemTD.Tests.EditMode
{
    public sealed class EnemyAffixCombatTests
    {
        EnemyDefinition _def;

        [SetUp]
        public void SetUp()
        {
            _def = ScriptableObject.CreateInstance<EnemyDefinition>();
            _def.MaxHealth = 20f;
            _def.MoveSpeed = 2f;
            _def.Armor = 0;
            _def.ShieldMax = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_def);
        }

        [Test]
        public void ScaleMagnitude_Hexproof_IsZero()
        {
            var enemy = Create(EnemyAffix.Hexproof);
            Assert.AreEqual(0f, CurseHex.ScaleMagnitude(enemy, -30f), 1e-4f);
        }

        [Test]
        public void ScaleMagnitude_Unhallowed_IsPointSix()
        {
            var enemy = Create(EnemyAffix.Unhallowed);
            Assert.AreEqual(-18f, CurseHex.ScaleMagnitude(enemy, -30f), 1e-4f);
        }

        [Test]
        public void ScaleMagnitude_HexproofWinsOverUnhallowed()
        {
            var enemy = Create(EnemyAffix.Unhallowed, EnemyAffix.Hexproof);
            Assert.AreEqual(0f, CurseHex.ScaleMagnitude(enemy, 27f), 1e-4f);
        }

        [Test]
        public void Bloody_Self_AddsOnePointFiveTimesM()
        {
            var enemy = Create(EnemyAffix.Bloody);
            PackAuraRuntime.Apply(new List<EnemyRuntime> { enemy });
            var expected = 20f * DamageTypeCombat.PackAuraHealthFraction * DamageTypeCombat.PackAuraSelfMultiplier;
            Assert.AreEqual(20f + expected, enemy.MaxHealth, 1e-4f);
            Assert.AreEqual(20f + expected, enemy.Hp, 1e-4f);
        }

        [Test]
        public void Bloody_AllyInRadius_AddsOnePointTwoFiveTimesM()
        {
            var source = Create(EnemyAffix.Bloody);
            var ally = Create();
            ally.SetWorldPosition(new Vector3(1f, 0f, 0f));
            PackAuraRuntime.Apply(new List<EnemyRuntime> { source, ally });
            var expected = 20f * DamageTypeCombat.PackAuraHealthFraction * DamageTypeCombat.PackAuraAllyMultiplier;
            Assert.AreEqual(20f + expected, ally.MaxHealth, 1e-4f);
        }

        [Test]
        public void Bloody_AllyOutsideRadius_Unchanged()
        {
            var source = Create(EnemyAffix.Bloody);
            var ally = Create();
            ally.SetWorldPosition(new Vector3(3f, 0f, 0f));
            PackAuraRuntime.Apply(new List<EnemyRuntime> { source, ally });
            Assert.AreEqual(20f, ally.MaxHealth, 1e-4f);
        }

        [Test]
        public void Hunting_Self_BoostsMoveSpeed()
        {
            var enemy = Create(EnemyAffix.Hunting);
            PackAuraRuntime.Apply(new List<EnemyRuntime> { enemy });
            var expected = 2f * (1f + DamageTypeCombat.PackAuraSpeedFraction * DamageTypeCombat.PackAuraSelfMultiplier);
            Assert.AreEqual(expected, enemy.CurrentMoveSpeed, 1e-4f);
        }

        [Test]
        public void Ironclad_Self_AddsArmor()
        {
            var enemy = Create(EnemyAffix.Ironclad);
            PackAuraRuntime.Apply(new List<EnemyRuntime> { enemy });
            var expected = Mathf.RoundToInt(
                DamageTypeCombat.PackAuraArmor * DamageTypeCombat.PackAuraSelfMultiplier);
            Assert.AreEqual(expected, enemy.Armor);
        }

        [Test]
        public void Shaded_Self_AddsShield()
        {
            var enemy = Create(EnemyAffix.Shaded);
            PackAuraRuntime.Apply(new List<EnemyRuntime> { enemy });
            var expected = 20f * DamageTypeCombat.PackAuraShieldFraction * DamageTypeCombat.PackAuraSelfMultiplier;
            Assert.AreEqual(expected, enemy.ShieldHp, 1e-4f);
        }

        [Test]
        public void Hexproof_StillReceivesPackAura()
        {
            var enemy = Create(EnemyAffix.Hexproof, EnemyAffix.Bloody);
            PackAuraRuntime.Apply(new List<EnemyRuntime> { enemy });
            Assert.Greater(enemy.MaxHealth, 20f);
        }

        [Test]
        public void TwoBloodySources_DoNotStack()
        {
            var a = Create(EnemyAffix.Bloody);
            var b = Create(EnemyAffix.Bloody);
            b.SetWorldPosition(new Vector3(1f, 0f, 0f));
            PackAuraRuntime.Apply(new List<EnemyRuntime> { a, b });
            var self = 20f * DamageTypeCombat.PackAuraHealthFraction * DamageTypeCombat.PackAuraSelfMultiplier;
            Assert.AreEqual(20f + self, a.MaxHealth, 1e-4f);
        }

        EnemyRuntime Create(params EnemyAffix[] affixes)
        {
            _def.Affixes = affixes != null && affixes.Length > 0 ? affixes : null;
            var enemy = new EnemyRuntime();
            enemy.Init(_def, new List<Vector3> { Vector3.zero, Vector3.right });
            return enemy;
        }
    }
}
