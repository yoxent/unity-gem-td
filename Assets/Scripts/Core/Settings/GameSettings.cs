using UnityEngine;

namespace GemTD.Core
{
    public static class GameSettings
    {
        public const string MasterVolumeKey = "GemTD.Settings.MasterVolume";
        public const string BgmVolumeKey = "GemTD.Settings.BgmVolume";
        public const string SfxVolumeKey = "GemTD.Settings.SfxVolume";

        public const float DefaultMasterVolume = 1f;
        public const float DefaultBgmVolume = 1f;
        public const float DefaultSfxVolume = 1f;

        public static bool IsPanelOpen { get; set; }

        public static float GetMasterVolume()
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume));
        }

        public static void SetMasterVolume(float volume)
        {
            var v = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MasterVolumeKey, v);
            PlayerPrefs.Save();
            ApplyAudio();
        }

        public static float GetBgmVolume()
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, DefaultBgmVolume));
        }

        public static void SetBgmVolume(float volume)
        {
            var v = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(BgmVolumeKey, v);
            PlayerPrefs.Save();
            ApplyAudio();
        }

        public static float GetSfxVolume()
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume));
        }

        public static void SetSfxVolume(float volume)
        {
            var v = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SfxVolumeKey, v);
            PlayerPrefs.Save();
            ApplyAudio();
        }

        // Master multiplier affects BGM/SFX effective levels.
        public static float GetEffectiveBgmVolume() => GetBgmVolume() * GetMasterVolume();
        public static float GetEffectiveSfxVolume() => GetSfxVolume() * GetMasterVolume();

        public static void ApplyAudio()
        {
            var master = GetMasterVolume();
            AudioListener.volume = master;

            // Until we have a full bus system / AudioMixer, best-effort: update any AudioSources
            // whose GameObject name includes "BGM" or "SFX". This keeps master as a true multiplier.
            var bgmBase = GetBgmVolume();
            var sfxBase = GetSfxVolume();

            var sources = Object.FindObjectsOfType<AudioSource>();
            for (var i = 0; i < sources.Length; i++)
            {
                var goName = sources[i].name;
                if (goName == null) continue;
                var lower = goName.ToLowerInvariant();
                if (lower.Contains("bgm"))
                    sources[i].volume = bgmBase;
                else if (lower.Contains("sfx"))
                    sources[i].volume = sfxBase;
            }
        }
    }
}
