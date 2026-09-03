using UnityEngine;

namespace GemTD.Core
{
    [CreateAssetMenu(menuName = "Gem TD/Audio Cue", fileName = "AudioCue")]
    public sealed class AudioCue : ScriptableObject
    {
        public AudioBus bus;
        [Range(0f, 1f)] public float volume = 1f;
        public AudioClip bgmClip;
        public bool loop = true;
        public SfxData sfx = SfxData.Default;

        void Reset()
        {
            volume = 1f;
            loop = true;
            sfx = SfxData.Default;
        }

        void OnValidate()
        {
            if (sfx.pitch == 0f && sfx.pitchMin == 0f && sfx.pitchMax == 0f)
            {
                var clip = sfx.clip;
                var random = sfx.randomPitch;
                sfx = SfxData.Default;
                sfx.clip = clip;
                sfx.randomPitch = random;
            }
        }
    }
}
