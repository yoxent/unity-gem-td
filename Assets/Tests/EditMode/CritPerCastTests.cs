using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;
using NUnit.Framework;

namespace GemTD.Tests.EditMode
{
    public sealed class CritPerCastTests
    {
        static SkillSpec Spec(float chance, float multiplier, bool resolved, bool crit)
        {
            var spec = SkillSpec.FromBase(100f);
            spec.CritChance = chance;
            spec.CritMultiplier = multiplier;
            spec.CritResolved = resolved;
            spec.Crit = crit;
            return spec;
        }

        [Test]
        public void ResolvedCrit_AppliesMultiplier_AndReportsCrit()
        {
            var spec = Spec(0.05f, 1.5f, resolved: true, crit: true);
            var dealt = IncomingHit.ApplyCrit(100f, spec, 0.99, out var isCrit);
            Assert.IsTrue(isCrit);
            Assert.AreEqual(150f, dealt, 1e-4f);
        }

        [Test]
        public void ResolvedNonCrit_LeavesDamagePlain_EvenOnALowRoll()
        {
            var spec = Spec(0.05f, 1.5f, resolved: true, crit: false);
            // A roll that would have crit per-hit must be ignored: the cast decided.
            var dealt = IncomingHit.ApplyCrit(100f, spec, 0.0, out var isCrit);
            Assert.IsFalse(isCrit);
            Assert.AreEqual(100f, dealt, 1e-4f);
        }

        [Test]
        public void ResolvedCast_DrawsNoRandomness()
        {
            var resolved = Spec(0.05f, 1.5f, resolved: true, crit: true);
            for (var i = 0; i <= 100; i++)
            {
                IncomingHit.ApplyCrit(100f, resolved, i / 100d, out var isCrit);
                Assert.IsTrue(isCrit, "A resolved cast must not depend on the roll.");
            }
        }

        [Test]
        public void UnresolvedSpec_StillRollsPerHit()
        {
            var spec = Spec(0.5f, 2f, resolved: false, crit: false);

            IncomingHit.ApplyCrit(100f, spec, 0.10, out var lowRollCrit);
            Assert.IsTrue(lowRollCrit);

            IncomingHit.ApplyCrit(100f, spec, 0.90, out var highRollCrit);
            Assert.IsFalse(highRollCrit);
        }

        [Test]
        public void AtChanceRoll_IsAMiss_NotACrit()
        {
            var spec = Spec(0.05f, 1.5f, resolved: false, crit: false);
            // double 0.05 is below float 0.05f; the float-space compare keeps it a miss.
            var dealt = IncomingHit.ApplyCrit(100f, spec, 0.05, out var isCrit);
            Assert.IsFalse(isCrit);
            Assert.AreEqual(100f, dealt, 1e-4f);
        }

        [Test]
        public void ThreeArgumentOverload_StillRolls()
        {
            var spec = Spec(0.5f, 2f, resolved: false, crit: false);
            Assert.AreEqual(200f, IncomingHit.ApplyCrit(100f, spec, 0.10), 1e-4f);
            Assert.AreEqual(100f, IncomingHit.ApplyCrit(100f, spec, 0.90), 1e-4f);
        }

        [Test]
        public void ZeroDamage_ReportsNoCrit()
        {
            var spec = Spec(1f, 2f, resolved: true, crit: true);
            var dealt = IncomingHit.ApplyCrit(0f, spec, 0.0, out var isCrit);
            Assert.IsFalse(isCrit);
            Assert.AreEqual(0f, dealt, 1e-4f);
        }

        [Test]
        public void CritChance_MultipliesTheBase()
        {
            var spec = SkillSpec.FromBase(100f);
            var modifiers = new[]
            {
                GemStatModifier.TieredSingle(
                    GemStat.CritChance,
                    RoleModifierOperation.Multiply,
                    1.93f,
                    2.17f,
                    2.41f),
            };

            var resolved = GemStatResolver.Apply(spec, modifiers, GemRarity.Greater);
            // 0.05 x 2.41, not 0.05 + 1.41.
            Assert.AreEqual(0.1205f, resolved.CritChance, 1e-4f);
        }

        [Test]
        public void CritChance_ClampsAtOne()
        {
            var spec = SkillSpec.FromBase(100f);
            var modifiers = new[]
            {
                GemStatModifier.TieredSingle(
                    GemStat.CritChance,
                    RoleModifierOperation.Multiply,
                    1000f,
                    1000f,
                    1000f),
            };

            var resolved = GemStatResolver.Apply(spec, modifiers, GemRarity.Normal);
            Assert.AreEqual(1f, resolved.CritChance, 1e-4f);
        }

        [Test]
        public void CritMultiplier_AddsToTheBase()
        {
            var spec = SkillSpec.FromBase(100f);
            var modifiers = new[]
            {
                GemStatModifier.TieredSingle(
                    GemStat.CritMultiplier,
                    RoleModifierOperation.Add,
                    1.22f,
                    1.38f,
                    1.54f),
            };

            var resolved = GemStatResolver.Apply(spec, modifiers, GemRarity.Greater);
            Assert.AreEqual(1.5f + 1.54f, resolved.CritMultiplier, 1e-4f);
        }

        [Test]
        public void CritMultiplier_NeverDropsBelowOne()
        {
            var spec = SkillSpec.FromBase(100f);
            var modifiers = new[]
            {
                GemStatModifier.Single(GemStat.CritMultiplier, RoleModifierOperation.Add, -5f),
            };

            var resolved = GemStatResolver.Apply(spec, modifiers, GemRarity.Normal);
            Assert.AreEqual(1f, resolved.CritMultiplier, 1e-4f);
        }

        [Test]
        public void CritOnlyGem_IsNotDraftEligible()
        {
            var critOnly = new[]
            {
                GemStatModifier.Single(GemStat.CritChance, RoleModifierOperation.Multiply, 2.41f),
            };
            Assert.IsFalse(DraftPoolEligibility.HasResolvedCombatModifier(critOnly));

            var critDamageOnly = new[]
            {
                GemStatModifier.Single(GemStat.CritMultiplier, RoleModifierOperation.Add, 1.54f),
            };
            Assert.IsFalse(DraftPoolEligibility.HasResolvedCombatModifier(critDamageOnly));
        }

        [Test]
        public void CritPairedWithAnotherStat_StaysDraftEligible()
        {
            var mixed = new[]
            {
                GemStatModifier.Single(GemStat.CritChance, RoleModifierOperation.Multiply, 2.41f),
                GemStatModifier.Single(GemStat.Damage, RoleModifierOperation.Multiply, 1.1f),
            };
            Assert.IsTrue(DraftPoolEligibility.HasResolvedCombatModifier(mixed));
        }
    }
}
