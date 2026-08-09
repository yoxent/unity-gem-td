using NUnit.Framework;
using GemTD.Gameplay.Meta;

namespace GemTD.Tests.EditMode
{
    public sealed class CodexProgressTests
    {
        [Test]
        public void Unlock_Once_PersistsViaStore()
        {
            var store = new MemoryCodexStore();
            var a = new CodexProgress(store);
            Assert.IsFalse(a.IsHydraUnlocked);
            Assert.AreEqual(CodexProgress.CrypticHydraHint, a.HydraHintOrReveal);
            a.NotifyHydraFormed();
            Assert.IsTrue(a.IsHydraUnlocked);
            Assert.AreEqual(CodexProgress.RevealedHydraText, a.HydraHintOrReveal);

            var b = new CodexProgress(store);
            Assert.IsTrue(b.IsHydraUnlocked);
            Assert.AreEqual(CodexProgress.RevealedHydraText, b.HydraHintOrReveal);
        }

        [Test]
        public void NotifyHydraFormed_IsIdempotent()
        {
            var store = new MemoryCodexStore();
            var progress = new CodexProgress(store);
            progress.NotifyHydraFormed();
            progress.NotifyHydraFormed();
            Assert.IsTrue(progress.IsHydraUnlocked);
        }
    }
}
