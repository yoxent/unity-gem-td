using NUnit.Framework;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;

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
            var mod = GemModifierFactory.Create(GemId.Lmp);
            Assert.IsInstanceOf<LmpModifier>(mod);
        }

        [Test]
        public void Chain_AddsChainsAndReducesDamage()
        {
            var pipeline = new GemModifierPipeline();
            var baseline = AttackSpec.FromBase(damage: 10f);
            var result = pipeline.Apply(baseline, new IAttackModifier[] { new ChainModifier(0.7f, 2) });

            Assert.AreEqual(7f, result.Damage, 0.001f);
            Assert.AreEqual(2, result.ChainCount);
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

            Assert.AreEqual(1.25f, result.FireRateMultiplier, 0.001f);
        }

        [Test]
        public void IncreasedAccuracy_BoostsRange()
        {
            var pipeline = new GemModifierPipeline();
            var baseline = AttackSpec.FromBase(damage: 10f);
            var result = pipeline.Apply(baseline, new IAttackModifier[] { new IncreasedAccuracyModifier() });

            Assert.AreEqual(1.2f, result.RangeMultiplier, 0.001f);
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
        public void AttackEcho_SetsTwoVolleysAtSixtyPercent()
        {
            var pipeline = new GemModifierPipeline();
            var baseline = AttackSpec.FromBase(damage: 10f);
            var result = pipeline.Apply(baseline, new IAttackModifier[] { new AttackEchoModifier() });

            Assert.AreEqual(2, result.EchoVolleyCount);
            Assert.AreEqual(0.6f, result.EchoDamageFactor, 0.001f);
        }

        [Test]
        public void Factory_CreatesNewDraftGems()
        {
            Assert.IsInstanceOf<FasterAttacksModifier>(GemModifierFactory.Create(GemId.FasterAttacks));
            Assert.IsInstanceOf<IncreasedAccuracyModifier>(GemModifierFactory.Create(GemId.IncreasedAccuracy));
            Assert.IsInstanceOf<SlowerProjectilesModifier>(GemModifierFactory.Create(GemId.SlowerProjectiles));
            Assert.IsInstanceOf<AttackEchoModifier>(GemModifierFactory.Create(GemId.AttackEcho));
        }

        [Test]
        public void FromBase_DefaultsNewMultiplierFields()
        {
            var baseline = AttackSpec.FromBase(damage: 10f);
            Assert.AreEqual(1f, baseline.RangeMultiplier, 0.001f);
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
        public void StatusGems_SetFlags_AndTradeoffs()
        {
            var pipeline = new GemModifierPipeline();
            var ignite = pipeline.Apply(AttackSpec.FromBase(10f), new IAttackModifier[] { new IgniteModifier() });
            Assert.IsTrue(ignite.Ignite);

            var chill = pipeline.Apply(AttackSpec.FromBase(10f), new IAttackModifier[] { new ChillModifier() });
            Assert.IsTrue(chill.Chill);

            var shock = pipeline.Apply(AttackSpec.FromBase(10f), new IAttackModifier[] { new ShockModifier() });
            Assert.IsTrue(shock.Shock);

            var pierce = pipeline.Apply(AttackSpec.FromBase(10f), new IAttackModifier[] { new PierceModifier() });
            Assert.IsTrue(pierce.Pierce);
            Assert.AreEqual(8.5f, pierce.Damage, 0.001f);

            var prolif = pipeline.Apply(AttackSpec.FromBase(10f), new IAttackModifier[] { new ElementalProliferationModifier() });
            Assert.IsTrue(prolif.Proliferate);
            Assert.AreEqual(7.5f, prolif.Damage, 0.001f);
        }

        [Test]
        public void Factory_CreatesPr5Gems()
        {
            Assert.IsInstanceOf<IncreasedAreaModifier>(GemModifierFactory.Create(GemId.IncreasedArea));
            Assert.IsInstanceOf<IgniteModifier>(GemModifierFactory.Create(GemId.Ignite));
            Assert.IsInstanceOf<ChillModifier>(GemModifierFactory.Create(GemId.Chill));
            Assert.IsInstanceOf<ShockModifier>(GemModifierFactory.Create(GemId.Shock));
            Assert.IsInstanceOf<PierceModifier>(GemModifierFactory.Create(GemId.Pierce));
            Assert.IsInstanceOf<ElementalProliferationModifier>(GemModifierFactory.Create(GemId.ElementalProliferation));
            Assert.IsInstanceOf<ForkModifier>(GemModifierFactory.Create(GemId.Fork));
        }
    }
}
