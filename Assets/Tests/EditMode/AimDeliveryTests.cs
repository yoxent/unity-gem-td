using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class AimDeliveryTests
    {
        [Test]
        public void Resolve_EmptySockets_CopiesRoleAimAndDelivery()
        {
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            var role = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            role.AimMode = AimMode.Ground;
            role.DeliveryPattern = DeliveryPattern.PayloadNova;
            def.Roles = new TowerRoleDefinition[] { role };
            def.Tags = GemTag.Attack | GemTag.Projectile;
            def.Damage = 10f;
            var tower = new TowerInstance(Vector2Int.zero, def);
            try
            {
                Assert.AreEqual(AimMode.Ground, def.GetAimMode());
                Assert.AreEqual(DeliveryPattern.PayloadNova, def.GetDeliveryPattern());
                var spec = new GemModifierPipeline().Resolve(tower, new List<ISkillModifier>(0));
                Assert.AreEqual(AimMode.Ground, spec.AimMode);
                Assert.AreEqual(DeliveryPattern.PayloadNova, spec.DeliveryPattern);
            }
            finally
            {
                Object.DestroyImmediate(def);
                Object.DestroyImmediate(role);
            }
        }

        [Test]
        public void GetAimMode_WithoutDamageRole_IsDirectStraight()
        {
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            try
            {
                Assert.AreEqual(AimMode.Direct, def.GetAimMode());
                Assert.AreEqual(DeliveryPattern.Straight, def.GetDeliveryPattern());
            }
            finally
            {
                Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void GemSet_RewritesAimAndDelivery_LastSetWins()
        {
            var spec = SkillSpec.FromBase(10f);
            spec = GemStatResolver.Apply(
                spec,
                new[]
                {
                    GemStatModifier.Single(GemStat.AimMode, RoleModifierOperation.Set, (float)AimMode.Ground),
                    GemStatModifier.Single(GemStat.DeliveryPattern, RoleModifierOperation.Set, (float)DeliveryPattern.PayloadNova),
                    GemStatModifier.Single(GemStat.AimMode, RoleModifierOperation.Set, (float)AimMode.Direct)
                });
            Assert.AreEqual(AimMode.Direct, spec.AimMode);
            Assert.AreEqual(DeliveryPattern.PayloadNova, spec.DeliveryPattern);
        }

        [Test]
        public void GemAddMultiply_AndInvalidOrdinal_LeaveAimAndDelivery()
        {
            var spec = SkillSpec.FromBase(10f);
            spec.AimMode = AimMode.Ground;
            spec.DeliveryPattern = DeliveryPattern.PayloadNova;
            spec = GemStatResolver.Apply(
                spec,
                new[]
                {
                    GemStatModifier.Single(GemStat.AimMode, RoleModifierOperation.Add, 1f),
                    GemStatModifier.Single(GemStat.DeliveryPattern, RoleModifierOperation.Multiply, 2f),
                    GemStatModifier.Single(GemStat.AimMode, RoleModifierOperation.Set, 99f),
                    GemStatModifier.Single(GemStat.DeliveryPattern, RoleModifierOperation.Set, -1f)
                });
            Assert.AreEqual(AimMode.Ground, spec.AimMode);
            Assert.AreEqual(DeliveryPattern.PayloadNova, spec.DeliveryPattern);
        }

        [Test]
        public void GemSet_WarpStrikeOrdinal_IsAccepted()
        {
            var spec = SkillSpec.FromBase(10f);
            spec = GemStatResolver.Apply(
                spec,
                new[]
                {
                    GemStatModifier.Single(
                        GemStat.DeliveryPattern,
                        RoleModifierOperation.Set,
                        (float)DeliveryPattern.WarpStrike)
                });
            Assert.AreEqual(DeliveryPattern.WarpStrike, spec.DeliveryPattern);
        }

        [Test]
        public void Resolve_SocketedGem_OverridesRoleAim()
        {
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            var role = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            role.AimMode = AimMode.Direct;
            role.DeliveryPattern = DeliveryPattern.Straight;
            def.Roles = new TowerRoleDefinition[] { role };
            def.Tags = GemTag.Attack | GemTag.Projectile;
            def.Damage = 10f;
            def.SocketCount = 1;
            var gem = ScriptableObject.CreateInstance<GemDefinition>();
            gem.Id = GemId.None;
            gem.Modifiers = new[]
            {
                GemStatModifier.Single(GemStat.AimMode, RoleModifierOperation.Set, (float)AimMode.Ground),
                GemStatModifier.Single(GemStat.DeliveryPattern, RoleModifierOperation.Set, (float)DeliveryPattern.PayloadNova)
            };
            var tower = new TowerInstance(Vector2Int.zero, def);
            Assert.IsTrue(tower.TrySocket(gem, 0, allowSocket: true));
            try
            {
                var spec = new GemModifierPipeline().Resolve(tower, new List<ISkillModifier>(1));
                Assert.AreEqual(AimMode.Ground, spec.AimMode);
                Assert.AreEqual(DeliveryPattern.PayloadNova, spec.DeliveryPattern);
            }
            finally
            {
                Object.DestroyImmediate(def);
                Object.DestroyImmediate(role);
                Object.DestroyImmediate(gem);
            }
        }
    }
}
