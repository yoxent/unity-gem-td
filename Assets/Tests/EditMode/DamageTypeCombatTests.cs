using NUnit.Framework;
using GemTD.Gameplay.Combat;

namespace GemTD.Tests.EditMode
{
    public sealed class DamageTypeCombatTests
    {
        [Test]
        public void Constants_MatchSpec()
        {
            Assert.AreEqual(1.2f, DamageTypeCombat.ChaosVsShieldMultiplier);
            Assert.AreEqual(1.25f, DamageTypeCombat.PackAuraAllyMultiplier);
            Assert.AreEqual(1.5f, DamageTypeCombat.PackAuraSelfMultiplier);
            Assert.AreEqual(2f, DamageTypeCombat.PackAuraRadius);
            Assert.AreEqual(0.6f, DamageTypeCombat.UnhallowedCurseEffectiveness);
            Assert.AreEqual(0.2f, DamageTypeCombat.PackAuraHealthFraction);
            Assert.AreEqual(0.2f, DamageTypeCombat.PackAuraSpeedFraction);
            Assert.AreEqual(5f, DamageTypeCombat.PackAuraArmor);
            Assert.AreEqual(0.2f, DamageTypeCombat.PackAuraShieldFraction);
        }
    }
}
