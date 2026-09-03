using UnityEngine;

namespace GemTD.Core
{
    /// <summary>Process-lifetime profile facade over <see cref="IGemTdSaveStore"/>.</summary>
    public static class PlayerProfile
    {
        static IGemTdSaveStore _store;
        static GemTdSaveDto _cache;
        static bool _loaded;

        /// <summary>True when the last <see cref="TryUpdateHighestWave"/> raised the saved high.</summary>
        public static bool LastUpdateWasNewBest { get; private set; }

        public static void Initialize(IGemTdSaveStore store)
        {
            _store = store;
            _loaded = false;
            LastUpdateWasNewBest = false;
            _cache = null;
            Load();
        }

        /// <summary>Idempotent. Uses <see cref="JsonFileGemTdSaveStore"/> when no store was injected.</summary>
        public static void Load()
        {
            if (_loaded)
                return;

            if (_store == null)
                _store = new JsonFileGemTdSaveStore();

            _cache = _store.Load() ?? new GemTdSaveDto();
            MigrateLegacyVolumePrefsIfPresent();
            _loaded = true;
        }

        public static int GetHighestWaveCleared()
        {
            Load();
            return _cache.HighestWaveCleared;
        }

        public static float GetMasterVolume()
        {
            Load();
            return Mathf.Clamp01(_cache.MasterVolume);
        }

        public static float GetBgmVolume()
        {
            Load();
            return Mathf.Clamp01(_cache.BgmVolume);
        }

        public static float GetSfxVolume()
        {
            Load();
            return Mathf.Clamp01(_cache.SfxVolume);
        }

        public static void SetMasterVolume(float volume)
        {
            Load();
            _cache.MasterVolume = Mathf.Clamp01(volume);
            _store.Save(_cache);
        }

        public static void SetBgmVolume(float volume)
        {
            Load();
            _cache.BgmVolume = Mathf.Clamp01(volume);
            _store.Save(_cache);
        }

        public static void SetSfxVolume(float volume)
        {
            Load();
            _cache.SfxVolume = Mathf.Clamp01(volume);
            _store.Save(_cache);
        }

        /// <summary>Monotonic. Returns true when <paramref name="wave"/> becomes the new high.</summary>
        public static bool TryUpdateHighestWave(int wave)
        {
            if (!_loaded && _store == null)
                return false;

            Load();
            LastUpdateWasNewBest = wave > _cache.HighestWaveCleared;
            if (!LastUpdateWasNewBest)
                return false;

            _cache.HighestWaveCleared = wave;
            _store.Save(_cache);
            return true;
        }

        /// <summary>EditMode isolation — does not touch the default save file.</summary>
        public static void ResetForTests()
        {
            _store = null;
            _cache = null;
            _loaded = false;
            LastUpdateWasNewBest = false;
        }

        static void MigrateLegacyVolumePrefsIfPresent()
        {
            var hasMaster = PlayerPrefs.HasKey(GameSettings.MasterVolumeKey);
            var hasBgm = PlayerPrefs.HasKey(GameSettings.BgmVolumeKey);
            var hasSfx = PlayerPrefs.HasKey(GameSettings.SfxVolumeKey);
            if (!hasMaster && !hasBgm && !hasSfx)
                return;

            if (hasMaster)
                _cache.MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(GameSettings.MasterVolumeKey));
            if (hasBgm)
                _cache.BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(GameSettings.BgmVolumeKey));
            if (hasSfx)
                _cache.SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(GameSettings.SfxVolumeKey));

            _store.Save(_cache);
            PlayerPrefs.DeleteKey(GameSettings.MasterVolumeKey);
            PlayerPrefs.DeleteKey(GameSettings.BgmVolumeKey);
            PlayerPrefs.DeleteKey(GameSettings.SfxVolumeKey);
            PlayerPrefs.Save();
        }
    }
}
