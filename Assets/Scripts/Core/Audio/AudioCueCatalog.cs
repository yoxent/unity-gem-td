using System;
using UnityEngine;

namespace GemTD.Core
{
    [CreateAssetMenu(menuName = "Gem TD/Audio/Cue Catalog", fileName = "AudioCueCatalog")]
    public sealed class AudioCueCatalog : ScriptableObject
    {
        [SerializeField, Tooltip("The BGM cue started by AudioPlayer on boot.")]
        AudioCue activeBgmCue;

        [SerializeField, Tooltip("All SFX cues available to GameEvents.RaisePlaySfx(string).")]
        AudioCue[] sfxCues;

        public AudioCue ActiveBgmCue => activeBgmCue;

        public bool TryGetSfx(string eventKey, out AudioCue cue)
        {
            cue = null;
            if (string.IsNullOrWhiteSpace(eventKey) || sfxCues == null)
                return false;

            for (var i = 0; i < sfxCues.Length; i++)
            {
                var candidate = sfxCues[i];
                if (candidate == null
                    || candidate.bus != AudioBus.Sfx
                    || !string.Equals(candidate.EventKey, eventKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (cue != null)
                {
                    cue = null;
                    return false;
                }

                cue = candidate;
            }

            return cue != null;
        }

        void OnValidate()
        {
            if (activeBgmCue != null && activeBgmCue.bus != AudioBus.Bgm)
                Debug.LogWarning("AudioCueCatalog: activeBgmCue is not on the Bgm bus.", activeBgmCue);

            if (sfxCues == null)
                return;

            for (var i = 0; i < sfxCues.Length; i++)
            {
                var cue = sfxCues[i];
                if (cue == null)
                {
                    Debug.LogWarning("AudioCueCatalog: SFX cue list contains an empty entry.", this);
                    continue;
                }

                if (cue.bus != AudioBus.Sfx)
                    Debug.LogWarning("AudioCueCatalog: SFX list contains a non-Sfx cue.", cue);
                if (string.IsNullOrWhiteSpace(cue.EventKey))
                    Debug.LogWarning("AudioCueCatalog: SFX cue has an empty eventKey.", cue);

                for (var j = i + 1; j < sfxCues.Length; j++)
                {
                    var other = sfxCues[j];
                    if (other != null
                        && !string.IsNullOrWhiteSpace(cue.EventKey)
                        && string.Equals(cue.EventKey, other.EventKey, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.LogWarning(
                            "AudioCueCatalog: duplicate SFX eventKey '" + cue.EventKey + "'.",
                            this);
                    }
                }
            }
        }
    }
}
