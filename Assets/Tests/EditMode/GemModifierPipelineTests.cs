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
    }
}
