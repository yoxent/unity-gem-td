using NUnit.Framework;
using UnityEngine;
using GemTD.Core;

namespace GemTD.Tests.EditMode
{
    public sealed class GameSettingsTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(GameSettings.MasterVolumeKey);
            PlayerPrefs.DeleteKey(GameSettings.BgmVolumeKey);
            PlayerPrefs.DeleteKey(GameSettings.SfxVolumeKey);
            GameSettings.IsPanelOpen = false;
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(GameSettings.MasterVolumeKey);
            PlayerPrefs.DeleteKey(GameSettings.BgmVolumeKey);
            PlayerPrefs.DeleteKey(GameSettings.SfxVolumeKey);
            GameSettings.IsPanelOpen = false;
            AudioListener.volume = 1f;
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
    }
}
