using NUnit.Framework;
using GemTD.Gameplay.Enemies;

namespace GemTD.Tests.EditMode
{
    public sealed class EnemyHealthBarMathTests
    {
        [Test]
        public void ShouldShow_FullHpAndShield_NonBoss_False()
        {
            Assert.IsFalse(EnemyHealthBarMath.ShouldShow(20f, 20f, 10f, 10f, false));
        }

        [Test]
        public void ShouldShow_DamagedHp_True()
        {
            Assert.IsTrue(EnemyHealthBarMath.ShouldShow(19f, 20f, 10f, 10f, false));
        }

        [Test]
        public void ShouldShow_DamagedShield_True()
        {
            Assert.IsTrue(EnemyHealthBarMath.ShouldShow(20f, 20f, 9f, 10f, false));
        }

        [Test]
        public void ShouldShow_BossAtFull_True()
        {
            Assert.IsTrue(EnemyHealthBarMath.ShouldShow(400f, 400f, 0f, 0f, true));
        }

        [Test]
        public void ComputeFills_HpAndShieldShareMaxHealthScale()
        {
            EnemyHealthBarMath.ComputeFills(10f, 20f, 5f, out var hpFill, out var shieldFill);
            Assert.AreEqual(0.5f, hpFill, 1e-4f);
            Assert.AreEqual(0.25f, shieldFill, 1e-4f);
        }

        [Test]
        public void ComputeFills_ShieldOverlapsFullBarWhenEqualToMaxHealth()
        {
            EnemyHealthBarMath.ComputeFills(20f, 20f, 20f, out var hpFill, out var shieldFill);
            Assert.AreEqual(1f, hpFill, 1e-4f);
            Assert.AreEqual(1f, shieldFill, 1e-4f);
        }

        [Test]
        public void ComputeFills_ShieldLargerThanMaxHealth_ClampsToOne()
        {
            EnemyHealthBarMath.ComputeFills(20f, 20f, 40f, out _, out var shieldFill);
            Assert.AreEqual(1f, shieldFill, 1e-4f);
        }

        [Test]
        public void ComputeFills_ZeroMaxHealth_BothZero()
        {
            EnemyHealthBarMath.ComputeFills(0f, 0f, 10f, out var hpFill, out var shieldFill);
            Assert.AreEqual(0f, hpFill, 1e-4f);
            Assert.AreEqual(0f, shieldFill, 1e-4f);
        }
    }
}
