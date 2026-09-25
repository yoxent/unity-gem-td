using NUnit.Framework;
using GemTD.Gameplay.Combat;

namespace GemTD.Tests.EditMode
{
    public sealed class BoltElementVisualTests
    {
        [Test]
        public void Resolve_FireMixUsesFireVariant()
        {
            var spec = SkillSpec.FromBase(10f);
            spec.MixFire = 1f;

            Assert.AreEqual(BoltElement.Fire, BoltElementVisual.Resolve(spec));
        }

        [Test]
        public void Resolve_ColdMixUsesIceVariant()
        {
            var spec = SkillSpec.FromBase(10f);
            spec.MixCold = 1f;

            Assert.AreEqual(BoltElement.Ice, BoltElementVisual.Resolve(spec));
        }

        [Test]
        public void Resolve_LightningMixUsesLightningVariant()
        {
            var spec = SkillSpec.FromBase(10f);
            spec.MixLightning = 1f;

            Assert.AreEqual(BoltElement.Lightning, BoltElementVisual.Resolve(spec));
        }

        [Test]
        public void Resolve_UntypedAndPhysicalMixUsesWaterVariant()
        {
            var untyped = SkillSpec.FromBase(10f);
            var physical = SkillSpec.FromBase(10f);
            physical.MixPhysical = 1f;

            Assert.AreEqual(BoltElement.Water, BoltElementVisual.Resolve(untyped));
            Assert.AreEqual(BoltElement.Water, BoltElementVisual.Resolve(physical));
        }

        [Test]
        public void Resolve_UsesStrongestRecognizedElement()
        {
            var spec = SkillSpec.FromBase(10f);
            spec.MixFire = 0.25f;
            spec.MixCold = 0.5f;
            spec.MixLightning = 0.25f;

            Assert.AreEqual(BoltElement.Ice, BoltElementVisual.Resolve(spec));
        }
    }
}
