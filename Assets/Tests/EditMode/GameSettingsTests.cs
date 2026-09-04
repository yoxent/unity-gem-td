using System.IO;
using NUnit.Framework;
using UnityEngine;
using GemTD.Core;

namespace GemTD.Tests.EditMode
{
    public sealed class GameSettingsTests
    {
        string _tempPath;

        [SetUp]
        public void SetUp()
        {
            PlayerProfile.ResetForTests();
            _tempPath = Path.Combine(Path.GetTempPath(), "gemtd-settings-test-" + Path.GetRandomFileName() + ".json");
            PlayerPrefs.DeleteKey(GameSettings.MasterVolumeKey);
            PlayerPrefs.DeleteKey(GameSettings.BgmVolumeKey);
            PlayerPrefs.DeleteKey(GameSettings.SfxVolumeKey);
            GameSettings.IsPanelOpen = false;
            PlayerProfile.Initialize(new JsonFileGemTdSaveStore(_tempPath));
        }

        [TearDown]
        public void TearDown()
        {
            PlayerProfile.ResetForTests();
            PlayerPrefs.DeleteKey(GameSettings.MasterVolumeKey);
            PlayerPrefs.DeleteKey(GameSettings.BgmVolumeKey);
            PlayerPrefs.DeleteKey(GameSettings.SfxVolumeKey);
            GameSettings.IsPanelOpen = false;
            AudioListener.volume = 1f;
            if (!string.IsNullOrEmpty(_tempPath) && File.Exists(_tempPath))
                File.Delete(_tempPath);
        }

        [Test]
        public void SceneNames_MatchBuildSceneNames()
        {
            Assert.AreEqual("MainMenu", SceneNames.MainMenu);
            Assert.AreEqual("Run", SceneNames.Run);
        }

        [Test]
        public void GetMasterVolume_MissingKey_IsDefault()
        {
            Assert.AreEqual(GameSettings.DefaultMasterVolume, GameSettings.GetMasterVolume());
        }

        [Test]
        public void GetBgmVolume_MissingKey_IsDefault()
        {
            Assert.AreEqual(GameSettings.DefaultBgmVolume, GameSettings.GetBgmVolume());
        }

        [Test]
        public void GetSfxVolume_MissingKey_IsDefault()
        {
            Assert.AreEqual(GameSettings.DefaultSfxVolume, GameSettings.GetSfxVolume());
        }

        [Test]
        public void SetMasterVolume_ClampsAndRoundTrips()
        {
            GameSettings.SetMasterVolume(-0.5f);
            Assert.AreEqual(0f, GameSettings.GetMasterVolume());
            GameSettings.SetMasterVolume(2f);
            Assert.AreEqual(1f, GameSettings.GetMasterVolume());
            GameSettings.SetMasterVolume(0.25f);
            Assert.AreEqual(0.25f, GameSettings.GetMasterVolume(), 0.0001f);
        }

        [Test]
        public void SetBgmVolume_ClampsAndRoundTrips()
        {
            GameSettings.SetBgmVolume(-0.5f);
            Assert.AreEqual(0f, GameSettings.GetBgmVolume());
            GameSettings.SetBgmVolume(2f);
            Assert.AreEqual(1f, GameSettings.GetBgmVolume());
            GameSettings.SetBgmVolume(0.25f);
            Assert.AreEqual(0.25f, GameSettings.GetBgmVolume(), 0.0001f);
        }

        [Test]
        public void SetSfxVolume_ClampsAndRoundTrips()
        {
            GameSettings.SetSfxVolume(-0.5f);
            Assert.AreEqual(0f, GameSettings.GetSfxVolume());
            GameSettings.SetSfxVolume(2f);
            Assert.AreEqual(1f, GameSettings.GetSfxVolume());
            GameSettings.SetSfxVolume(0.25f);
            Assert.AreEqual(0.25f, GameSettings.GetSfxVolume(), 0.0001f);
        }

        [Test]
        public void EffectiveBgm_IsBgmTimesMaster()
        {
            GameSettings.SetMasterVolume(0.5f);
            GameSettings.SetBgmVolume(0.7f);
            Assert.AreEqual(0.35f, GameSettings.GetEffectiveBgmVolume(), 0.0001f);
        }

        [Test]
        public void EffectiveSfx_IsSfxTimesMaster()
        {
            GameSettings.SetMasterVolume(0.5f);
            GameSettings.SetSfxVolume(0.7f);
            Assert.AreEqual(0.35f, GameSettings.GetEffectiveSfxVolume(), 0.0001f);
        }

        [Test]
        public void ApplyAudio_SetsAudioListenerVolume()
        {
            GameSettings.SetMasterVolume(0.4f);
            AudioListener.volume = 1f;
            GameSettings.ApplyAudio();
            Assert.AreEqual(0.4f, AudioListener.volume, 0.0001f);
        }

        [Test]
        public void SetMasterVolume_PersistsThroughStoreReload()
        {
            GameSettings.SetMasterVolume(0.33f);
            GameSettings.SetBgmVolume(0.44f);
            GameSettings.SetSfxVolume(0.55f);
            PlayerProfile.TryUpdateHighestWave(9);

            PlayerProfile.ResetForTests();
            PlayerProfile.Initialize(new JsonFileGemTdSaveStore(_tempPath));
            Assert.AreEqual(0.33f, GameSettings.GetMasterVolume(), 0.0001f);
            Assert.AreEqual(0.44f, GameSettings.GetBgmVolume(), 0.0001f);
            Assert.AreEqual(0.55f, GameSettings.GetSfxVolume(), 0.0001f);
            Assert.AreEqual(9, PlayerProfile.GetHighestWaveCleared());
        }

        [Test]
        public void SetVolume_DoesNotWritePlayerPrefs()
        {
            GameSettings.SetMasterVolume(0.2f);
            Assert.IsFalse(PlayerPrefs.HasKey(GameSettings.MasterVolumeKey));
        }

        [Test]
        public void Load_MigratesPlayerPrefsThenDeletesKeys()
        {
            PlayerProfile.ResetForTests();
            PlayerPrefs.SetFloat(GameSettings.MasterVolumeKey, 0.3f);
            PlayerPrefs.SetFloat(GameSettings.BgmVolumeKey, 0.4f);
            PlayerPrefs.SetFloat(GameSettings.SfxVolumeKey, 0.5f);
            PlayerPrefs.Save();

            var store = new JsonFileGemTdSaveStore(_tempPath);
            store.Save(new GemTdSaveDto { HighestWaveCleared = 12, MasterVolume = 1f, BgmVolume = 1f, SfxVolume = 1f });

            PlayerProfile.Initialize(store);
            Assert.AreEqual(0.3f, GameSettings.GetMasterVolume(), 0.0001f);
            Assert.AreEqual(0.4f, GameSettings.GetBgmVolume(), 0.0001f);
            Assert.AreEqual(0.5f, GameSettings.GetSfxVolume(), 0.0001f);
            Assert.AreEqual(12, PlayerProfile.GetHighestWaveCleared());
            Assert.IsFalse(PlayerPrefs.HasKey(GameSettings.MasterVolumeKey));
            Assert.IsFalse(PlayerPrefs.HasKey(GameSettings.BgmVolumeKey));
            Assert.IsFalse(PlayerPrefs.HasKey(GameSettings.SfxVolumeKey));

            PlayerProfile.ResetForTests();
            PlayerProfile.Initialize(new JsonFileGemTdSaveStore(_tempPath));
            Assert.AreEqual(0.3f, GameSettings.GetMasterVolume(), 0.0001f);
            Assert.IsFalse(PlayerPrefs.HasKey(GameSettings.MasterVolumeKey));
        }
    }
}
