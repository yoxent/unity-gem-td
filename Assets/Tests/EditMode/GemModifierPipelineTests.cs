using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class GemModifierPipelineTests
    {
        [Test]
        public void EmptyPipeline_ReturnsBaseline()
        {
            var pipeline = new GemModifierPipeline();
            var baseline = SkillSpec.FromBase(damage: 10f);
            var result = pipeline.Apply(baseline, System.Array.Empty<ISkillModifier>());

            Assert.AreEqual(10f, result.Damage);
            Assert.AreEqual(1, result.ProjectileCount);
        }

        [Test]
        public void MultipleProjectiles_AddsProjectilesAndReducesDamage()
        {
            var result = ApplyCatalog(GemId.MultipleProjectiles, SkillSpec.FromBase(damage: 10f));

            Assert.AreEqual(8f, result.Damage, 0.001f);
            Assert.AreEqual(3, result.ProjectileCount);
        }

        [Test]
        public void Chain_AddsChainsAndReducesDamage()
        {
            var result = ApplyCatalog(GemId.Chain, SkillSpec.FromBase(damage: 10f));

            Assert.AreEqual(7f, result.Damage, 0.001f);
            Assert.AreEqual(1, result.ChainCount);
            Assert.AreEqual(0.6f, result.ChainHopFalloff, 0.001f);
        }

        [Test]
        public void FasterAttacks_BoostsFireRate()
        {
            var result = ApplyCatalog(GemId.FasterAttacks, SkillSpec.FromBase(damage: 10f));

            Assert.AreEqual(1.25f, result.AttackSpeedMultiplier, 0.001f);
        }

        [Test]
        public void SlowerProjectiles_BoostsDamageAndSlowsProjectiles()
        {
            var result = ApplyCatalog(GemId.SlowerProjectiles, SkillSpec.FromBase(damage: 10f));

            Assert.AreEqual(13f, result.Damage, 0.001f);
            Assert.AreEqual(0.6f, result.ProjectileSpeedMultiplier, 0.001f);
        }

        [Test]
        public void FromBase_DefaultsNewMultiplierFields()
        {
            var baseline = SkillSpec.FromBase(damage: 10f);
            Assert.AreEqual(1f, baseline.RangeMultiplier, 0.001f);
            Assert.AreEqual(1f, baseline.AttackSpeedMultiplier, 0.001f);
            Assert.AreEqual(1f, baseline.CastSpeedMultiplier, 0.001f);
            Assert.AreEqual(1f, baseline.ProjectileSpeedMultiplier, 0.001f);
            Assert.AreEqual(1, baseline.EchoVolleyCount);
            Assert.AreEqual(1f, baseline.EchoDamageFactor, 0.001f);
            Assert.AreEqual(PierceMode.Finite, baseline.PierceBehavior);
            Assert.AreEqual(0, baseline.PierceCount);
            Assert.IsFalse(baseline.Pierce);
            Assert.AreEqual(AimMode.Direct, baseline.AimMode);
            Assert.AreEqual(DeliveryPattern.Straight, baseline.DeliveryPattern);
        }

        [Test]
        public void ResolveBaseline_CopiesRoleChainCount()
        {
            var role = ScriptableObject.CreateInstance<SpellRoleDefinition>();
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            def.Roles = new[] { role };
            role.Modifiers = new[]
            {
                RoleStatModifier.Single(RoleStat.CastTime, RoleModifierOperation.Set, 0.6f),
                RoleStatModifier.Single(RoleStat.CastSpeed, RoleModifierOperation.Set, 100f)
            };
            role.Levels = new[]
            {
                new RoleLevelDefinition
                {
                    SourceLevel = 1,
                    Modifiers = new[]
                    {
                        RoleStatModifier.Single(RoleStat.ChainCount, RoleModifierOperation.Set, 4f)
                    }
                }
            };

            try
            {
                var tower = new TowerInstance(Vector2Int.zero, def);
                var spec = new GemModifierPipeline().ResolveBaseline(tower);
                Assert.AreEqual(4, spec.ChainCount);
            }
            finally
            {
                Object.DestroyImmediate(def);
                Object.DestroyImmediate(role);
            }
        }

        [Test]
        public void IncreasedArea_BoostsAoeAndCutsFireRate()
        {
            var result = ApplyCatalog(GemId.IncreasedArea, SkillSpec.FromBase(10f, 1, aoe: 1f));
            Assert.AreEqual(1.35f, result.AoeRadius, 0.001f);
            Assert.AreEqual(1.35f, result.AoeRadiusMultiplier, 0.001f);
            Assert.AreEqual(0.9f, result.FireRateMultiplier, 0.001f);
        }

        [Test]
        public void PierceAndProlif_SetFlags_AndTradeoffs()
        {
            var pierce = ApplyCatalog(GemId.Pierce, SkillSpec.FromBase(10f));
            Assert.IsTrue(pierce.Pierce);
            Assert.AreEqual(PierceMode.Finite, pierce.PierceBehavior);
            Assert.AreEqual(1, pierce.PierceCount);
            Assert.AreEqual(8.5f, pierce.Damage, 0.001f);

            var prolif = ApplyCatalog(GemId.ElementalProliferation, SkillSpec.FromBase(10f));
            Assert.IsTrue(prolif.Proliferate);
            Assert.AreEqual(7.5f, prolif.Damage, 0.001f);
        }

        [Test]
        public void Combustion_MoreDamageAndIgnite()
        {
            var result = ApplyCatalog(GemId.Combustion, SkillSpec.FromBase(10f));
            Assert.AreEqual(11.4f, result.Damage, 0.001f);
            Assert.IsTrue(result.Ignite);
        }

        [Test]
        public void AddedElemental_ExtraDamageAndAilments()
        {
            var fire = ApplyCatalog(GemId.AddedFireDamage, SkillSpec.FromBase(10f));
            Assert.AreEqual(13.1f, fire.Damage, 0.001f);
            Assert.IsFalse(fire.Ignite);

            var cold = ApplyCatalog(GemId.AddedColdDamage, SkillSpec.FromBase(10f));
            Assert.AreEqual(14f, cold.Damage, 0.001f);
            Assert.IsTrue(cold.Chill);

            var lightning = ApplyCatalog(GemId.AddedLightningDamage, SkillSpec.FromBase(10f));
            Assert.AreEqual(14f, lightning.Damage, 0.001f);
            Assert.IsTrue(lightning.Shock);
        }

        [Test]
        public void Knockback_SetsChanceAndDistance()
        {
            var result = ApplyCatalog(GemId.Knockback, SkillSpec.FromBase(10f));
            Assert.AreEqual(0.34f, result.KnockbackChance, 0.001f);
            Assert.AreEqual(1f, result.KnockbackDistance, 0.001f);
        }

        [Test]
        public void Resolve_ChainThenFork_StacksDamageMultipliers()
        {
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            def.Damage = 10f;
            def.SocketCount = 2;
            def.Tags = GemTag.Attack | GemTag.Projectile;
            var tower = new TowerInstance(Vector2Int.zero, def);
            var chain = ScriptableObject.CreateInstance<GemDefinition>();
            chain.Id = GemId.Chain;
            CatalogGemModifiers.Bind(chain);
            var fork = ScriptableObject.CreateInstance<GemDefinition>();
            fork.Id = GemId.Fork;
            CatalogGemModifiers.Bind(fork);
            Assert.IsTrue(tower.TrySocket(chain, 0, allowSocket: true));
            Assert.IsTrue(tower.TrySocket(fork, 1, allowSocket: true));

            try
            {
                var scratch = new System.Collections.Generic.List<ISkillModifier>(2);
                var spec = new GemModifierPipeline().Resolve(tower, scratch);
                Assert.AreEqual(10f * 0.7f * 0.85f, spec.Damage, 0.001f);
                Assert.AreEqual(1, spec.ChainCount);
                Assert.AreEqual(1, spec.ForkCount);
            }
            finally
            {
                Object.DestroyImmediate(def);
                Object.DestroyImmediate(chain);
                Object.DestroyImmediate(fork);
            }
        }

        [Test]
        public void Resolve_NoneIdGem_StillAppliesModifiers()
        {
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            def.Damage = 10f;
            def.SocketCount = 1;
            var tower = new TowerInstance(Vector2Int.zero, def);
            var gem = ScriptableObject.CreateInstance<GemDefinition>();
            gem.Id = GemId.None;
            gem.Modifiers = new[]
            {
                GemStatModifier.Single(GemStat.ChainCount, RoleModifierOperation.Add, 10f)
            };
            Assert.IsTrue(tower.TrySocket(gem, 0, allowSocket: true));

            try
            {
                var scratch = new System.Collections.Generic.List<ISkillModifier>(1);
                var spec = new GemModifierPipeline().Resolve(tower, scratch);
                Assert.AreEqual(10, spec.ChainCount);
            }
            finally
            {
                Object.DestroyImmediate(def);
                Object.DestroyImmediate(gem);
            }
        }

        [Test]
        public void GemDefinitionModifier_GreaterInstance_UsesGreaterScalar()
        {
            var gem = ScriptableObject.CreateInstance<GemDefinition>();
            gem.Modifiers = new[]
            {
                GemStatModifier.TieredSingle(
                    GemStat.Damage,
                    RoleModifierOperation.Multiply,
                    lesser: 0.8f,
                    normal: 1f,
                    greater: 1.3f)
            };

            try
            {
                var baseline = SkillSpec.FromBase(10f, 10f, projectiles: 1, aoe: 0f);
                var greater = new GemModifierPipeline().Apply(
                    baseline,
                    new ISkillModifier[]
                    {
                        new GemDefinitionModifier(new GemInstance(gem, GemRarity.Greater))
                    });
                var normal = new GemModifierPipeline().Apply(
                    baseline,
                    new ISkillModifier[] { new GemDefinitionModifier(gem) });

                Assert.AreEqual(13f, greater.Damage, 1e-4f);
                Assert.AreEqual(10f, normal.Damage, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(gem);
            }
        }

        static SkillSpec ApplyCatalog(GemId id, SkillSpec baseline)
        {
            var gem = ScriptableObject.CreateInstance<GemDefinition>();
            gem.Id = id;
            CatalogGemModifiers.Bind(gem);
            try
            {
                return new GemModifierPipeline().Apply(
                    baseline,
                    new ISkillModifier[] { new GemDefinitionModifier(gem) });
            }
            finally
            {
                Object.DestroyImmediate(gem);
            }
        }
    }
}
