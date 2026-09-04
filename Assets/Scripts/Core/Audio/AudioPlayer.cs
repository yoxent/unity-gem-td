using UnityEngine;

namespace GemTD.Core
{
    [DefaultExecutionOrder(-1000)]
    public sealed class AudioPlayer : MonoBehaviour
    {
        const int SfxPoolSize = 8;

        [SerializeField, Tooltip("The central BGM and SFX configuration for this audio player.")]
        AudioCueCatalog catalog;
        public static AudioPlayer Instance { get; private set; }

        AudioSource _bgm;
        AudioSource[] _sfx;
        AudioCue _currentBgmCue;

        public static AudioPlayer EnsureExists()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject("AudioPlayer");
            DontDestroyOnLoad(go);
            return go.AddComponent<AudioPlayer>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bgm = CreateChildSource("BGM");
            _bgm.loop = true;
            _bgm.pitch = 1f;
            _sfx = new AudioSource[SfxPoolSize];
            for (var i = 0; i < SfxPoolSize; i++)
                _sfx[i] = CreateChildSource("SFX_" + i);
        }

        void Start()
        {
            PlayConfiguredBgm();
        }

        public void PlayConfiguredBgm()
        {
            if (catalog != null && catalog.ActiveBgmCue != null)
                GameEvents.RaisePlayBgm(catalog.ActiveBgmCue);
        }

        void OnEnable()
        {
            GameEvents.PlaySfx += OnPlaySfx;
            GameEvents.PlayBgm += OnPlayBgm;
            GameEvents.StopBgm += OnStopBgm;
        }

        void OnDisable()
        {
            GameEvents.PlaySfx -= OnPlaySfx;
            GameEvents.PlayBgm -= OnPlayBgm;
            GameEvents.StopBgm -= OnStopBgm;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void RefreshBusVolumes()
        {
            if (_currentBgmCue != null)
                SetBgmVolume(BgmTargetVolume(_currentBgmCue));
        }

        AudioSource CreateChildSource(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var src = child.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            return src;
        }

        void OnValidate()
        {
            if (catalog == null)
                Debug.LogWarning("AudioPlayer: no AudioCueCatalog is assigned.", this);
        }

        void OnPlaySfx(string eventKey)
        {
            if (catalog == null || !catalog.TryGetSfx(eventKey, out var cue))
            {
#if UNITY_EDITOR
                Debug.LogWarning("AudioPlayer: no SFX cue is configured for eventKey '" + eventKey + "'.", this);
#endif
                return;
            }

            if (cue.sfx.clip == null)
                return;

            if (cue.bus != AudioBus.Sfx)
            {
#if UNITY_EDITOR
                Debug.LogWarning("AudioPlayer: PlaySfx ignored because the cue is not on the Sfx bus.", cue);
#endif
                return;
            }

            var src = NextSfxSource();
            src.clip = cue.sfx.clip;
            src.loop = false;
            src.pitch = AudioPitch.Resolve(cue.sfx, UnityEngine.Random.value);
            src.volume = AudioMix.SfxSourceVolume(cue.volume, GameSettings.GetSfxVolume());
            src.Play();
        }

        void OnPlayBgm(AudioCue cue)
        {
            if (cue == null || cue.bgmClip == null)
                return;

            if (cue.bus != AudioBus.Bgm)
            {
#if UNITY_EDITOR
                Debug.LogWarning("AudioPlayer: PlayBgm ignored because the cue is not on the Bgm bus.", cue);
#endif
                return;
            }

            if (_currentBgmCue == cue && _bgm.isPlaying)
                return;

            _currentBgmCue = cue;
            _bgm.clip = cue.bgmClip;
            _bgm.loop = cue.loop;
            _bgm.pitch = 1f;
            SetBgmVolume(BgmTargetVolume(cue));
            _bgm.Play();
        }

        void OnStopBgm()
        {
            _currentBgmCue = null;
            if (_bgm != null)
                _bgm.Stop();
        }

        float BgmTargetVolume(AudioCue cue)
        {
            return AudioMix.BgmSourceVolume(cue.volume, GameSettings.GetBgmVolume());
        }

        void SetBgmVolume(float volume)
        {
            if (_bgm != null)
                _bgm.volume = volume;
        }

        AudioSource NextSfxSource()
        {
            for (var i = 0; i < _sfx.Length; i++)
            {
                if (!_sfx[i].isPlaying)
                    return _sfx[i];
            }

            var oldest = _sfx[0];
            var oldestTime = oldest.time;
            for (var i = 1; i < _sfx.Length; i++)
            {
                if (_sfx[i].time > oldestTime)
                {
                    oldest = _sfx[i];
                    oldestTime = _sfx[i].time;
                }
            }

            oldest.Stop();
            return oldest;
        }
    }
}
