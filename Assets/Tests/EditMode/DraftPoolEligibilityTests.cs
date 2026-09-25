using NUnit.Framework;
using GemTD.Gameplay.Gems;

namespace GemTD.Tests.EditMode
{
    public sealed class DraftPoolEligibilityTests
    {
        [Test]
        public void EmptyModifiers_AreNotEligible()
        {
            Assert.IsFalse(
                DraftPoolEligibility.HasResolvedCombatModifier(
                    System.Array.Empty<GemStatModifier>()));
        }

        [Test]
        public void PausedOnlyModifiers_AreNotEligible()
        {
            var modifiers = new[]
            {
                new GemStatModifier { Stat = GemStat.SpreadDegrees },
                new GemStatModifier { Stat = GemStat.Proliferate },
                new GemStatModifier { Stat = GemStat.KnockbackDistance }
            };

            Assert.IsFalse(DraftPoolEligibility.HasResolvedCombatModifier(modifiers));
        }

        [Test]
        public void LiveModifier_IsEligible()
        {
            var modifiers = new[]
            {
                new GemStatModifier { Stat = GemStat.Damage }
            };

            Assert.IsTrue(DraftPoolEligibility.HasResolvedCombatModifier(modifiers));
        }

        [Test]
        public void LiveAndPausedModifiers_AreEligible()
        {
            var modifiers = new[]
            {
                new GemStatModifier { Stat = GemStat.Damage },
                new GemStatModifier { Stat = GemStat.SpreadDegrees },
                new GemStatModifier { Stat = GemStat.Proliferate },
                new GemStatModifier { Stat = GemStat.KnockbackDistance }
            };

            Assert.IsTrue(DraftPoolEligibility.HasResolvedCombatModifier(modifiers));
        }
    }
}
