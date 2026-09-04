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

        public static float GetMasterVolume() => PlayerProfile.GetMasterVolume();

        public static void SetMasterVolume(float volume)
        {
            PlayerProfile.SetMasterVolume(volume);
            ApplyAudio();
        }

        public static float GetBgmVolume() => PlayerProfile.GetBgmVolume();

        public static void SetBgmVolume(float volume)
        {
            PlayerProfile.SetBgmVolume(volume);
            ApplyAudio();
        }

        public static float GetSfxVolume() => PlayerProfile.GetSfxVolume();

        public static void SetSfxVolume(float volume)
        {
            PlayerProfile.SetSfxVolume(volume);
            ApplyAudio();
        }

        public static float GetEffectiveBgmVolume() => GetBgmVolume() * GetMasterVolume();
        public static float GetEffectiveSfxVolume() => GetSfxVolume() * GetMasterVolume();

        public static void ApplyAudio()
        {
            AudioListener.volume = GetMasterVolume();
            AudioPlayer.Instance?.RefreshBusVolumes();
        }
    }
}
