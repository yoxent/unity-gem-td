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
            var baseline = AttackSpec.FromBase(damage: 10f);
            var result = pipeline.Apply(baseline, System.Array.Empty<IAttackModifier>());

            Assert.AreEqual(10f, result.Damage);
            Assert.AreEqual(1, result.ProjectileCount);
        }

        [Test]
        public void Lmp_AddsProjectilesAndReducesDamage()
        {
            var pipeline = new GemModifierPipeline();
            var baseline = AttackSpec.FromBase(damage: 10f);
            var result = pipeline.Apply(baseline, new IAttackModifier[] { new LmpModifier(0.8f, 2) });

            Assert.AreEqual(8f, result.Damage, 0.001f);
            Assert.AreEqual(3, result.ProjectileCount);
        }

        [Test]
        public void Factory_CreatesLmp()
        {
            var mod = GemModifierFactory.Create(GemId.MultipleProjectiles);
            Assert.IsInstanceOf<LmpModifier>(mod);
        }

        [Test]
        public void Chain_AddsChainsAndReducesDamage()
        {
            var pipeline = new GemModifierPipeline();
            var baseline = AttackSpec.FromBase(damage: 10f);
            var result = pipeline.Apply(baseline, new IAttackModifier[] { new ChainModifier() });

            Assert.AreEqual(7f, result.Damage, 0.001f);
            Assert.AreEqual(1, result.ChainCount);
        }

        [Test]
        public void Factory_CreatesChain()
        {
            var mod = GemModifierFactory.Create(GemId.Chain);
            Assert.IsInstanceOf<ChainModifier>(mod);
        }

        [Test]
        public void FasterAttacks_BoostsFireRate()
        {
            var pipeline = new GemModifierPipeline();
            var baseline = AttackSpec.FromBase(damage: 10f);
            var result = pipeline.Apply(baseline, new IAttackModifier[] { new FasterAttacksModifier() });

            Assert.AreEqual(1.25f, result.AttackSpeedMultiplier, 0.001f);
        }

        [Test]
        public void SlowerProjectiles_BoostsDamageAndSlowsProjectiles()
        {
            var pipeline = new GemModifierPipeline();
            var baseline = AttackSpec.FromBase(damage: 10f);
            var result = pipeline.Apply(baseline, new IAttackModifier[] { new SlowerProjectilesModifier() });

            Assert.AreEqual(13f, result.Damage, 0.001f);
            Assert.AreEqual(0.6f, result.ProjectileSpeedMultiplier, 0.001f);
        }

        [Test]
        public void Factory_CreatesOutOfPoolGems()
        {
            Assert.IsInstanceOf<FasterAttacksModifier>(GemModifierFactory.Create(GemId.FasterAttacks));
            Assert.IsInstanceOf<SlowerProjectilesModifier>(GemModifierFactory.Create(GemId.SlowerProjectiles));
        }

        [Test]
        public void FromBase_DefaultsNewMultiplierFields()
        {
            var baseline = AttackSpec.FromBase(damage: 10f);
            Assert.AreEqual(1f, baseline.RangeMultiplier, 0.001f);
            Assert.AreEqual(1f, baseline.AttackSpeedMultiplier, 0.001f);
            Assert.AreEqual(1f, baseline.CastSpeedMultiplier, 0.001f);
            Assert.AreEqual(1f, baseline.ProjectileSpeedMultiplier, 0.001f);
            Assert.AreEqual(1, baseline.EchoVolleyCount);
            Assert.AreEqual(1f, baseline.EchoDamageFactor, 0.001f);
        }

        [Test]
        public void IncreasedArea_BoostsAoeAndCutsFireRate()
        {
            var pipeline = new GemModifierPipeline();
            var baseline = AttackSpec.FromBase(10f, 1, aoe: 1f);
            var result = pipeline.Apply(baseline, new IAttackModifier[] { new IncreasedAreaModifier() });
            Assert.AreEqual(1.35f, result.AoeRadius, 0.001f);
            Assert.AreEqual(0.9f, result.FireRateMultiplier, 0.001f);
        }

        [Test]
        public void PierceAndProlif_SetFlags_AndTradeoffs()
        {
            var pipeline = new GemModifierPipeline();
            var pierce = pipeline.Apply(AttackSpec.FromBase(10f), new IAttackModifier[] { new PierceModifier() });
            Assert.IsTrue(pierce.Pierce);
            Assert.AreEqual(8.5f, pierce.Damage, 0.001f);

            var prolif = pipeline.Apply(AttackSpec.FromBase(10f), new IAttackModifier[] { new ElementalProliferationModifier() });
            Assert.IsTrue(prolif.Proliferate);
            Assert.AreEqual(7.5f, prolif.Damage, 0.001f);
        }

        [Test]
        public void Factory_CreatesDraftPoolGems()
        {
            Assert.IsInstanceOf<IncreasedAreaModifier>(GemModifierFactory.Create(GemId.IncreasedArea));
            Assert.IsInstanceOf<PierceModifier>(GemModifierFactory.Create(GemId.Pierce));
            Assert.IsInstanceOf<ElementalProliferationModifier>(GemModifierFactory.Create(GemId.ElementalProliferation));
            Assert.IsInstanceOf<ForkModifier>(GemModifierFactory.Create(GemId.Fork));
            Assert.IsInstanceOf<CombustionModifier>(GemModifierFactory.Create(GemId.Combustion));
            Assert.IsInstanceOf<AddedFireDamageModifier>(GemModifierFactory.Create(GemId.AddedFireDamage));
            Assert.IsInstanceOf<AddedColdDamageModifier>(GemModifierFactory.Create(GemId.AddedColdDamage));
            Assert.IsInstanceOf<AddedLightningDamageModifier>(GemModifierFactory.Create(GemId.AddedLightningDamage));
            Assert.IsInstanceOf<KnockbackModifier>(GemModifierFactory.Create(GemId.Knockback));
        }

        [Test]
        public void Combustion_MoreDamageAndIgnite()
        {
            var pipeline = new GemModifierPipeline();
            var result = pipeline.Apply(AttackSpec.FromBase(10f), new IAttackModifier[] { new CombustionModifier() });
            Assert.AreEqual(11.4f, result.Damage, 0.001f);
            Assert.IsTrue(result.Ignite);
        }

        [Test]
        public void AddedElemental_ExtraDamageAndAilments()
        {
            var pipeline = new GemModifierPipeline();
            var fire = pipeline.Apply(AttackSpec.FromBase(10f), new IAttackModifier[] { new AddedFireDamageModifier() });
            Assert.AreEqual(13.1f, fire.Damage, 0.001f);
            Assert.IsFalse(fire.Ignite);

            var cold = pipeline.Apply(AttackSpec.FromBase(10f), new IAttackModifier[] { new AddedColdDamageModifier() });
            Assert.AreEqual(14f, cold.Damage, 0.001f);
            Assert.IsTrue(cold.Chill);

            var lightning = pipeline.Apply(AttackSpec.FromBase(10f), new IAttackModifier[] { new AddedLightningDamageModifier() });
            Assert.AreEqual(14f, lightning.Damage, 0.001f);
            Assert.IsTrue(lightning.Shock);
        }

        [Test]
        public void Knockback_SetsChanceAndDistance()
        {
            var pipeline = new GemModifierPipeline();
            var result = pipeline.Apply(AttackSpec.FromBase(10f), new IAttackModifier[] { new KnockbackModifier() });
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
            var fork = ScriptableObject.CreateInstance<GemDefinition>();
            fork.Id = GemId.Fork;
            Assert.IsTrue(tower.TrySocket(chain, 0, allowSocket: true));
            Assert.IsTrue(tower.TrySocket(fork, 1, allowSocket: true));

            try
            {
                var scratch = new System.Collections.Generic.List<IAttackModifier>(2);
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
    }
}
