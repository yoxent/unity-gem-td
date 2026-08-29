using NUnit.Framework;
using GemTD.Gameplay.Combat;

namespace GemTD.Tests.EditMode
{
    public sealed class DamageMixTests
    {
        [Test]
        public void Empty_IsValidUntyped()
        {
            Assert.IsTrue(DamageMix.TryValidate(null, out _));
            Assert.IsTrue(DamageMix.TryValidate(System.Array.Empty<DamageTypeShare>(), out _));
            DamageMix.ToFractions(null, out var p, out var f, out var c, out var l, out var ch);
            Assert.AreEqual(0f, p);
            Assert.AreEqual(0f, f);
            Assert.AreEqual(0f, c);
            Assert.AreEqual(0f, l);
            Assert.AreEqual(0f, ch);
        }

        [Test]
        public void SixtyForty_PhysicalFire_Fractions()
        {
            var shares = new[]
            {
                new DamageTypeShare { Type = DamageType.Physical, Percent = 60 },
                new DamageTypeShare { Type = DamageType.Fire, Percent = 40 }
            };
            Assert.IsTrue(DamageMix.TryValidate(shares, out _));
            DamageMix.ToFractions(shares, out var p, out var f, out var c, out var l, out var ch);
            Assert.AreEqual(0.6f, p, 0.0001f);
            Assert.AreEqual(0.4f, f, 0.0001f);
            Assert.AreEqual(0f, c);
            Assert.AreEqual(0f, l);
            Assert.AreEqual(0f, ch);
        }

        [Test]
        public void DuplicateType_IsInvalid()
        {
            var shares = new[]
            {
                new DamageTypeShare { Type = DamageType.Fire, Percent = 50 },
                new DamageTypeShare { Type = DamageType.Fire, Percent = 50 }
            };
            Assert.IsFalse(DamageMix.TryValidate(shares, out var reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void SumNotOneHundred_IsInvalid()
        {
            var shares = new[]
            {
                new DamageTypeShare { Type = DamageType.Fire, Percent = 70 }
            };
            Assert.IsFalse(DamageMix.TryValidate(shares, out _));
        }

        [Test]
        public void HundredFire_IsValid()
        {
            var shares = new[]
            {
                new DamageTypeShare { Type = DamageType.Fire, Percent = 100 }
            };
            Assert.IsTrue(DamageMix.TryValidate(shares, out _));
            DamageMix.ToFractions(shares, out var p, out var f, out _, out _, out _);
            Assert.AreEqual(0f, p);
            Assert.AreEqual(1f, f, 0.0001f);
        }
    }
}
