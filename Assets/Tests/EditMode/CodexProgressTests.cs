using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Meta;

namespace GemTD.Tests.EditMode
{
    public sealed class CodexProgressTests
    {
        static CodexEntry MakeEntry(string id)
        {
            var e = ScriptableObject.CreateInstance<CodexEntry>();
            e.Id = id;
            return e;
        }

        [Test]
        public void Unlock_Once_PersistsViaStore()
        {
            var store = new MemoryCodexStore();
            var a = new CodexProgress(store);
            var entry = MakeEntry("hydra-ballista");
            Assert.IsFalse(a.IsUnlocked(entry));
            Assert.IsFalse(a.IsUnlocked("hydra-ballista"));

            a.Unlock(entry);
            Assert.IsTrue(a.IsUnlocked(entry));
            Assert.IsTrue(a.IsUnlocked("hydra-ballista"));

            // New instance over same store: unlock survived (persisted across runs).
            var b = new CodexProgress(store);
            Assert.IsTrue(b.IsUnlocked("hydra-ballista"));
        }

        [Test]
        public void Unlock_IsIdempotent()
        {
            var store = new MemoryCodexStore();
            var progress = new CodexProgress(store);
            var entry = MakeEntry("hydra-ballista");
            progress.Unlock(entry);
            progress.Unlock(entry);
            Assert.IsTrue(progress.IsUnlocked("hydra-ballista"));
        }

        [Test]
        public void IsUnlocked_UnknownId_ReturnsFalse()
        {
            var store = new MemoryCodexStore();
            var progress = new CodexProgress(store);
            Assert.IsFalse(progress.IsUnlocked("does-not-exist"));
            Assert.IsFalse(progress.IsUnlocked((CodexEntry)null));
        }

        [Test]
        public void Save_RoundTrip_PreservesMultipleIds()
        {
            var store = new MemoryCodexStore();
            var progress = new CodexProgress(store);
            progress.Unlock(MakeEntry("hydra-ballista"));
            progress.Unlock(MakeEntry("future-evolution"));

            var reloaded = new CodexProgress(store);
            Assert.IsTrue(reloaded.IsUnlocked("hydra-ballista"));
            Assert.IsTrue(reloaded.IsUnlocked("future-evolution"));
        }
    }
}