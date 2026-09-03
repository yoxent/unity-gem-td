using UnityEngine;

namespace GemTD.Core
{
    public sealed class AudioPlayer : MonoBehaviour
    {
        const int SfxPoolSize = 8;

        public static AudioPlayer Instance { get; private set; }

        AudioSource _bgm;
        AudioSource[] _sfx;
        AudioCue _currentBgmCue;
        bool _paused;
        float _bgmDisplayed;
        float _bgmFrom;
        float _bgmTarget;
        float _bgmFadeT = 1f;

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

            _bgm = CreateChildSource("BGM");
            _bgm.loop = true;
            _bgm.pitch = 1f;
            _sfx = new AudioSource[SfxPoolSize];
            for (var i = 0; i < SfxPoolSize; i++)
                _sfx[i] = CreateChildSource("SFX_" + i);
        }

        void OnEnable()
        {
            GameEvents.PlaySfx += OnPlaySfx;
            GameEvents.PlayBgm += OnPlayBgm;
            GameEvents.StopBgm += OnStopBgm;
            GameEvents.PauseChanged += OnPauseChanged;
        }

        void OnDisable()
        {
            GameEvents.PlaySfx -= OnPlaySfx;
            GameEvents.PlayBgm -= OnPlayBgm;
            GameEvents.StopBgm -= OnStopBgm;
            GameEvents.PauseChanged -= OnPauseChanged;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            if (_bgm == null || _bgmFadeT >= 1f)
                return;

            _bgmFadeT += Time.unscaledDeltaTime / AudioMix.DuckFadeSeconds;
            if (_bgmFadeT > 1f)
                _bgmFadeT = 1f;
            _bgmDisplayed = Mathf.Lerp(_bgmFrom, _bgmTarget, _bgmFadeT);
            _bgm.volume = _bgmDisplayed;
        }

        public void RefreshBusVolumes()
        {
            if (_currentBgmCue != null)
                SetBgmTarget(BgmTargetVolume(_currentBgmCue), snap: false);
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

        void OnPlaySfx(AudioCue cue)
        {
            if (cue == null || cue.bus != AudioBus.Sfx || cue.sfx.clip == null)
            {
#if UNITY_EDITOR
                if (cue != null && (cue.bus != AudioBus.Sfx || cue.sfx.clip == null))
                    Debug.LogWarning("AudioPlayer: PlaySfx ignored (need Sfx bus + clip).", cue);
#endif
                return;
            }

            var src = NextSfxSource();
            src.clip = cue.sfx.clip;
            src.loop = false;
            src.pitch = AudioPitch.Resolve(cue.sfx, Random.value);
            src.volume = AudioMix.SfxSourceVolume(cue.volume, GameSettings.GetSfxVolume());
            src.Play();
        }

        void OnPlayBgm(AudioCue cue)
        {
            if (cue == null || cue.bus != AudioBus.Bgm || cue.bgmClip == null)
            {
#if UNITY_EDITOR
                if (cue != null)
                    Debug.LogWarning("AudioPlayer: PlayBgm ignored (need Bgm bus + clip).", cue);
#endif
                return;
            }

            if (_currentBgmCue == cue && _bgm.isPlaying)
                return;

            _currentBgmCue = cue;
            _bgm.clip = cue.bgmClip;
            _bgm.loop = cue.loop;
            _bgm.pitch = 1f;
            SetBgmTarget(BgmTargetVolume(cue), snap: true);
            _bgm.Play();
        }

        void OnStopBgm()
        {
            _currentBgmCue = null;
            if (_bgm != null)
                _bgm.Stop();
        }

        void OnPauseChanged(bool paused)
        {
            _paused = paused;
            if (_currentBgmCue != null)
                SetBgmTarget(BgmTargetVolume(_currentBgmCue), snap: false);
        }

        float BgmTargetVolume(AudioCue cue)
        {
            return AudioMix.BgmSourceVolume(cue.volume, GameSettings.GetBgmVolume(), _paused);
        }

        void SetBgmTarget(float target, bool snap)
        {
            _bgmTarget = target;
            if (snap || _bgm == null)
            {
                _bgmDisplayed = target;
                _bgmFrom = target;
                _bgmFadeT = 1f;
                if (_bgm != null)
                    _bgm.volume = target;
                return;
            }

            _bgmFrom = _bgmDisplayed;
            _bgmFadeT = 0f;
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
