using System.IO;
using NUnit.Framework;
using UnityEngine;
using GemTD.Core;

namespace GemTD.Tests.EditMode
{
    public sealed class PlayerProfileTests
    {
        string _tempPath;

        [SetUp]
        public void SetUp()
        {
            PlayerProfile.ResetForTests();
            _tempPath = Path.Combine(Path.GetTempPath(), "gemtd-profile-test-" + Path.GetRandomFileName() + ".json");
        }

        [TearDown]
        public void TearDown()
        {
            PlayerProfile.ResetForTests();
            if (!string.IsNullOrEmpty(_tempPath) && File.Exists(_tempPath))
                File.Delete(_tempPath);
        }

        [Test]
        public void RoundTrip_HighestWave_ViaTempStore()
        {
            PlayerProfile.Initialize(new JsonFileGemTdSaveStore(_tempPath));
            Assert.AreEqual(0, PlayerProfile.GetHighestWaveCleared());
            Assert.IsTrue(PlayerProfile.TryUpdateHighestWave(18));
            Assert.AreEqual(18, PlayerProfile.GetHighestWaveCleared());

            PlayerProfile.ResetForTests();
            PlayerProfile.Initialize(new JsonFileGemTdSaveStore(_tempPath));
            Assert.AreEqual(18, PlayerProfile.GetHighestWaveCleared());
        }

        [Test]
        public void TryUpdateHighestWave_IsMonotonic()
        {
            PlayerProfile.Initialize(new JsonFileGemTdSaveStore(_tempPath));
            Assert.IsTrue(PlayerProfile.TryUpdateHighestWave(18));
            Assert.IsTrue(PlayerProfile.LastUpdateWasNewBest);

            Assert.IsFalse(PlayerProfile.TryUpdateHighestWave(12));
            Assert.AreEqual(18, PlayerProfile.GetHighestWaveCleared());
            Assert.IsFalse(PlayerProfile.LastUpdateWasNewBest);

            Assert.IsTrue(PlayerProfile.TryUpdateHighestWave(20));
            Assert.AreEqual(20, PlayerProfile.GetHighestWaveCleared());
            Assert.IsTrue(PlayerProfile.LastUpdateWasNewBest);
        }

        [Test]
        public void TryUpdateHighestWave_PreservesExistingVolumeFields()
        {
            var seed = new GemTdSaveDto
            {
                MasterVolume = 0.4f,
                BgmVolume = 0.6f,
                SfxVolume = 0.8f,
                HighestWaveCleared = 0,
            };
            var store = new JsonFileGemTdSaveStore(_tempPath);
            store.Save(seed);

            PlayerProfile.Initialize(store);
            Assert.IsTrue(PlayerProfile.TryUpdateHighestWave(7));

            var reloaded = store.Load();
            Assert.AreEqual(7, reloaded.HighestWaveCleared);
            Assert.AreEqual(0.4f, reloaded.MasterVolume, 0.0001f);
            Assert.AreEqual(0.6f, reloaded.BgmVolume, 0.0001f);
            Assert.AreEqual(0.8f, reloaded.SfxVolume, 0.0001f);
        }

        [Test]
        public void TryUpdateHighestWave_WithoutLoad_DoesNotTouchDisk()
        {
            Assert.IsFalse(PlayerProfile.TryUpdateHighestWave(5));
            Assert.IsFalse(File.Exists(_tempPath));
        }

        [Test]
        public void SetMasterVolume_PreservesHighestWave()
        {
            PlayerProfile.Initialize(new JsonFileGemTdSaveStore(_tempPath));
            Assert.IsTrue(PlayerProfile.TryUpdateHighestWave(4));
            PlayerProfile.SetMasterVolume(0.2f);
            var reloaded = new JsonFileGemTdSaveStore(_tempPath).Load();
            Assert.AreEqual(4, reloaded.HighestWaveCleared);
            Assert.AreEqual(0.2f, reloaded.MasterVolume, 0.0001f);
        }
    }
}
