using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class AuraInfluenceRuntimeTests
    {
        AttackRoleDefinition _attackRole;
        SpellRoleDefinition _spellRole;
        AuraRoleDefinition _hasteRole;
        AuraRoleDefinition _angerRole;
        AuraRoleDefinition _precisionRole;
        AuraRoleDefinition _hasteStrongRole;
        TowerDefinition _attack;
        TowerDefinition _spell;
        TowerDefinition _haste;
        TowerDefinition _anger;
        TowerDefinition _precision;
        TowerDefinition _hasteStrong;
        TowerDefinition _smite;

        [SetUp]
        public void SetUp()
        {
            _attackRole = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            _attackRole.Modifiers = new[]
            {
                Stat(RoleStat.AttackTime, 1f),
                Stat(RoleStat.AttackSpeed, 100f),
                Stat(RoleStat.TowerRadius, 8f),
                Stat(RoleStat.Damage, 10f)
            };
            _attack = Tower("Attack", _attackRole);

            _spellRole = ScriptableObject.CreateInstance<SpellRoleDefinition>();
            _spellRole.Modifiers = new[]
            {
                Stat(RoleStat.CastTime, 1f),
                Stat(RoleStat.CastSpeed, 100f),
                Stat(RoleStat.TowerRadius, 8f),
                Stat(RoleStat.Damage, 10f)
            };
            _spell = Tower("Spell", _spellRole);

            _hasteRole = HasteRole(20f);
            _haste = Tower("Haste", _hasteRole);
            _haste.Tags = GemTag.Aura;

            _hasteStrongRole = HasteRole(40f);
            _hasteStrong = Tower("HasteStrong", _hasteStrongRole);
            _hasteStrong.Tags = GemTag.Aura;

            _angerRole = ScriptableObject.CreateInstance<AuraRoleDefinition>();
            _angerRole.Modifiers = new[] { Stat(RoleStat.TowerRadius, 1.5f) };
            _angerRole.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 1,
                    Modifiers = System.Array.Empty<RoleStatModifier>(),
                    Effects = new[]
                    {
                        RoleEffectModifier.Range(
                            RoleEffectKind.AllyAddedAttackFireDamage,
                            RoleModifierOperation.Set,
                            25f,
                            36f),
                        RoleEffectModifier.Range(
                            RoleEffectKind.AllyAddedSpellFireDamage,
                            RoleModifierOperation.Set,
                            25f,
                            36f)
                    }
                }
            };
            _anger = Tower("Anger", _angerRole);
            _anger.Tags = GemTag.Aura;

            _precisionRole = ScriptableObject.CreateInstance<AuraRoleDefinition>();
            _precisionRole.Modifiers = new[] { Stat(RoleStat.TowerRadius, 1.5f) };
            _precisionRole.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 1,
                    Modifiers = System.Array.Empty<RoleStatModifier>(),
                    Effects = new[]
                    {
                        RoleEffectModifier.Single(
                            RoleEffectKind.AllyAccuracyRating,
                            RoleModifierOperation.Set,
                            193f),
                        RoleEffectModifier.Single(
                            RoleEffectKind.AllyCriticalStrikeChanceIncreased,
                            RoleModifierOperation.Set,
                            26f)
                    }
                }
            };
            _precision = Tower("Precision", _precisionRole);
            _precision.Tags = GemTag.Aura;

            var smiteAttack = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            smiteAttack.Modifiers = new[]
            {
                Stat(RoleStat.AttackTime, 1f),
                Stat(RoleStat.AttackSpeed, 100f),
                Stat(RoleStat.TowerRadius, 8f),
                Stat(RoleStat.Damage, 10f)
            };
            var smiteAura = HasteRole(20f);
            _smite = ScriptableObject.CreateInstance<TowerDefinition>();
            _smite.DisplayName = "Smite";
            _smite.Roles = new TowerRoleDefinition[] { smiteAttack, smiteAura };
            _smite.Tags = GemTag.Attack | GemTag.Aura;
            _smite.SocketCount = 3;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_attackRole);
            Object.DestroyImmediate(_spellRole);
            Object.DestroyImmediate(_hasteRole);
            Object.DestroyImmediate(_angerRole);
            Object.DestroyImmediate(_precisionRole);
            Object.DestroyImmediate(_hasteStrongRole);
            Object.DestroyImmediate(_attack);
            Object.DestroyImmediate(_spell);
            Object.DestroyImmediate(_haste);
            Object.DestroyImmediate(_anger);
            Object.DestroyImmediate(_precision);
            Object.DestroyImmediate(_hasteStrong);
            if (_smite != null)
            {
                for (var i = 0; i < _smite.Roles.Length; i++)
                    Object.DestroyImmediate(_smite.Roles[i]);
                Object.DestroyImmediate(_smite);
            }
        }

        [Test]
        public void InRadius_HasteShortensAttackInterval()
        {
            var ally = new TowerInstance(Vector2Int.zero, _attack);
            var aura = new TowerInstance(new Vector2Int(1, 0), _haste);
            var spec = SkillSpec.FromBase(10f);
            AuraInfluenceRuntime.Apply(ally, new[] { ally, aura }, ref spec, 1f);
            Assert.AreEqual(1f / 1.2f, _attack.FireInterval(spec, 1), 1e-4f);
        }

        [Test]
        public void OutOfRadius_DoesNotBuff()
        {
            var ally = new TowerInstance(Vector2Int.zero, _attack);
            var aura = new TowerInstance(new Vector2Int(3, 0), _haste);
            var spec = SkillSpec.FromBase(10f);
            var interval = _attack.FireInterval(spec, 1);
            AuraInfluenceRuntime.Apply(ally, new[] { ally, aura }, ref spec, 1f);
            Assert.AreEqual(interval, _attack.FireInterval(spec, 1), 1e-4f);
        }

        [Test]
        public void AuraOnly_DoesNotBuffSelf()
        {
            var aura = new TowerInstance(Vector2Int.zero, _haste);
            var spec = SkillSpec.FromBase(10f);
            var before = spec.AttackSpeedMultiplier;
            AuraInfluenceRuntime.Apply(aura, new[] { aura }, ref spec, 1f);
            Assert.AreEqual(before, spec.AttackSpeedMultiplier, 1e-4f);
        }

        [Test]
        public void DamagingAura_BuffsSelf()
        {
            var smite = new TowerInstance(Vector2Int.zero, _smite);
            var spec = SkillSpec.FromBase(10f);
            AuraInfluenceRuntime.Apply(smite, new[] { smite }, ref spec, 1f);
            Assert.AreEqual(1.2f, spec.AttackSpeedMultiplier, 1e-4f);
        }

        [Test]
        public void TwoHastes_KeepStrongerOnly()
        {
            var ally = new TowerInstance(Vector2Int.zero, _attack);
            var weak = new TowerInstance(new Vector2Int(1, 0), _haste);
            var strong = new TowerInstance(new Vector2Int(0, 1), _hasteStrong);
            var spec = SkillSpec.FromBase(10f);
            AuraInfluenceRuntime.Apply(ally, new[] { ally, weak, strong }, ref spec, 1f);
            Assert.AreEqual(1.4f, spec.AttackSpeedMultiplier, 1e-4f);
        }

        [Test]
        public void HasteAndAnger_BothApply()
        {
            var ally = new TowerInstance(Vector2Int.zero, _attack);
            var haste = new TowerInstance(new Vector2Int(1, 0), _haste);
            var anger = new TowerInstance(new Vector2Int(0, 1), _anger);
            var spec = SkillSpec.FromBase(10f, 10f);
            AuraInfluenceRuntime.Apply(ally, new[] { ally, haste, anger }, ref spec, 1f);
            Assert.AreEqual(1.2f, spec.AttackSpeedMultiplier, 1e-4f);
            Assert.AreEqual(35f, spec.DamageMin, 1e-4f);
            Assert.AreEqual(46f, spec.DamageMax, 1e-4f);
        }

        [Test]
        public void Anger_AddsSpellFireOnSpellAlly()
        {
            var ally = new TowerInstance(Vector2Int.zero, _spell);
            var anger = new TowerInstance(new Vector2Int(1, 0), _anger);
            var spec = SkillSpec.FromBase(10f, 10f);
            AuraInfluenceRuntime.Apply(ally, new[] { ally, anger }, ref spec, 1f);
            Assert.AreEqual(35f, spec.DamageMin, 1e-4f);
            Assert.AreEqual(46f, spec.DamageMax, 1e-4f);
        }

        [Test]
        public void Precision_AddsCritChance_IgnoresAccuracy()
        {
            var ally = new TowerInstance(Vector2Int.zero, _attack);
            var precision = new TowerInstance(new Vector2Int(1, 0), _precision);
            var spec = SkillSpec.FromBase(10f);
            AuraInfluenceRuntime.Apply(ally, new[] { ally, precision }, ref spec, 1f);
            Assert.AreEqual(0.31f, spec.CritChance, 1e-4f);
        }

        static AuraRoleDefinition HasteRole(float attackSpeedPercent)
        {
            var role = ScriptableObject.CreateInstance<AuraRoleDefinition>();
            role.Modifiers = new[] { Stat(RoleStat.TowerRadius, 1.5f) };
            role.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 1,
                    Modifiers = System.Array.Empty<RoleStatModifier>(),
                    Effects = new[]
                    {
                        RoleEffectModifier.Single(
                            RoleEffectKind.AllyAttackSpeedIncreased,
                            RoleModifierOperation.Set,
                            attackSpeedPercent),
                        RoleEffectModifier.Single(
                            RoleEffectKind.AllyCastSpeedIncreased,
                            RoleModifierOperation.Set,
                            attackSpeedPercent),
                        RoleEffectModifier.Single(
                            RoleEffectKind.AllyMovementSpeedIncreased,
                            RoleModifierOperation.Set,
                            11f)
                    }
                }
            };
            return role;
        }

        static RoleStatModifier Stat(RoleStat stat, float value)
        {
            return new RoleStatModifier
            {
                Stat = stat,
                Operation = RoleModifierOperation.Set,
                Value = value
            };
        }

        static TowerDefinition Tower(string name, TowerRoleDefinition role)
        {
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            def.DisplayName = name;
            def.Roles = new[] { role };
            return def;
        }
    }
}
