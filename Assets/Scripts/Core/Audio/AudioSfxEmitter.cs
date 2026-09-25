using UnityEngine;

namespace GemTD.Core
{
    [AddComponentMenu("Gem TD/Audio/SFX Emitter")]
    public sealed class AudioSfxEmitter : MonoBehaviour
    {
        [SerializeField, Tooltip("The AudioCue eventKey raised when Play is called.")]
        string eventKey;

        public void Play()
        {
            GameEvents.RaisePlaySfx(eventKey);
        }
    }
}
