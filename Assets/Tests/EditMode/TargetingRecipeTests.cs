using NUnit.Framework;
using GemTD.Gameplay.Combat;

namespace GemTD.Tests.EditMode
{
    public sealed class TargetingRecipeTests
    {
        [Test]
        public void Default_IsThreeFirst()
        {
            var r = TargetingRecipe.Default;
            Assert.AreEqual(TargetingKey.First, r.Priority1);
            Assert.AreEqual(TargetingKey.First, r.Priority2);
            Assert.AreEqual(TargetingKey.First, r.Priority3);
        }

        [Test]
        public void WithCycled_WrapsP1ThroughEightKeys()
        {
            var r = TargetingRecipe.Default;
            r = r.WithCycled(0);
            Assert.AreEqual(TargetingKey.LeastHpPct, r.Priority1);
            Assert.AreEqual(TargetingKey.First, r.Priority2);
            for (var i = 0; i < 7; i++)
                r = r.WithCycled(0);
            Assert.AreEqual(TargetingKey.First, r.Priority1);
        }

        [Test]
        public void WithCycled_InvalidSlot_ReturnsUnchanged()
        {
            var r = TargetingRecipe.Default;
            Assert.AreEqual(r, r.WithCycled(-1));
            Assert.AreEqual(r, r.WithCycled(3));
        }

        [Test]
        public void Clipboard_CopyPaste_AndEmptyPasteFails()
        {
            var clip = new TargetingClipboard();
            Assert.IsFalse(clip.TryGet(out _));
            var recipe = new TargetingRecipe
            {
                Priority1 = TargetingKey.MostArmor,
                Priority2 = TargetingKey.MostHpPct,
                Priority3 = TargetingKey.First
            };
            clip.Copy(recipe);
            Assert.IsTrue(clip.Has);
            Assert.IsTrue(clip.TryGet(out var pasted));
            Assert.AreEqual(recipe, pasted);
            clip.Clear();
            Assert.IsFalse(clip.TryGet(out _));
        }

        [Test]
        public void Scope_Next_AndNeedsAllConfirm()
        {
            Assert.AreEqual(TargetingApplyScope.ThisType,
                TargetingScopeRequests.Next(TargetingApplyScope.ThisTower));
            Assert.AreEqual(TargetingApplyScope.AllTowers,
                TargetingScopeRequests.Next(TargetingApplyScope.ThisType));
            Assert.AreEqual(TargetingApplyScope.ThisTower,
                TargetingScopeRequests.Next(TargetingApplyScope.AllTowers));
            Assert.IsTrue(TargetingScopeRequests.NeedsAllConfirm(
                TargetingApplyScope.ThisType, TargetingApplyScope.AllTowers));
            Assert.IsFalse(TargetingScopeRequests.NeedsAllConfirm(
                TargetingApplyScope.AllTowers, TargetingApplyScope.AllTowers));
            Assert.IsFalse(TargetingScopeRequests.NeedsAllConfirm(
                TargetingApplyScope.ThisTower, TargetingApplyScope.ThisType));
        }

        [Test]
        public void Labels_CoverAllKeys()
        {
            Assert.AreEqual("First", TargetingKeyLabels.For(TargetingKey.First));
            Assert.AreEqual("Least HP%", TargetingKeyLabels.For(TargetingKey.LeastHpPct));
            Assert.AreEqual("Most HP%", TargetingKeyLabels.For(TargetingKey.MostHpPct));
            Assert.AreEqual("Most Armor", TargetingKeyLabels.For(TargetingKey.MostArmor));
            Assert.AreEqual("Most Shield", TargetingKeyLabels.For(TargetingKey.MostShield));
            Assert.AreEqual("Fastest", TargetingKeyLabels.For(TargetingKey.Fastest));
            Assert.AreEqual("Slowest", TargetingKeyLabels.For(TargetingKey.Slowest));
            Assert.AreEqual("Last", TargetingKeyLabels.For(TargetingKey.Last));
        }
    }
}
