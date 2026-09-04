using System;
using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Plays per-prefab idle + 1..N fire states when the bound tower fires.
    /// FireInterval is the whole action. Non-event clips stretch to normalized windows;
    /// event clips preserve authored sequence order and proportions.
    /// Animator speed also follows RunClock (SpeedControl 1/2/4, 0 while paused).
    /// Event-enabled views play fire clips in authored order and release their pending combat action
    /// from the imported OnCombatAction("execute") marker.
    /// strikeNormalized is used only when event timing is disabled.
    /// </summary>
    public sealed class TowerAnimatorView : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [Tooltip("Character to yaw toward the target. Tower root stays fixed.")]
        [SerializeField] Transform facingRoot;
        [SerializeField] string idleState;
        [SerializeField] string[] fireStates;
        [SerializeField] float crossFade = 0.1f;
        [Tooltip("0–1 normalized action point used when imported event timing is disabled.")]
        [SerializeField] [Range(0f, 1f)] float strikeNormalized = 1f;
        [Tooltip("Wait for an imported OnCombatAction(\"execute\") event before resolving the pending combat action.")]
        [SerializeField] bool useAnimationActionEvent;

        TowerInstance _runtime;
        int _seenGeneration;
        TowerAttackPlayback _playback;
        string _playingState;
        float _simSpeed = 1f;
        Action<TowerInstance, int, string> _combatActionHandler;

        public Transform OccupantRoot => facingRoot;

        void OnEnable()
        {
            TowerAnimationEventRelay.ActionRaised += OnAnimationActionRaised;
        }

        void OnDisable()
        {
            TowerAnimationEventRelay.ActionRaised -= OnAnimationActionRaised;
        }

        public void SetOccupantVisible(bool visible)
        {
            if (facingRoot == null)
                return;
            if (facingRoot.gameObject.activeSelf != visible)
                facingRoot.gameObject.SetActive(visible);
        }

        public void Bind(TowerInstance runtime)
        {
            _runtime = runtime;
            if (_runtime != null)
            {
                _runtime.StrikeNormalized = strikeNormalized;
                _runtime.UsesAnimationActionEvent = useAnimationActionEvent;
            }
            _seenGeneration = runtime != null ? runtime.FireGeneration : 0;
            _playback.Stop();
            PlayIdle();
        }

        public void SetCombatActionHandler(Action<TowerInstance, int, string> handler)
        {
            _combatActionHandler = handler;
        }

        public void Tick(float dt, float simSpeed)
        {
            if (_runtime == null || animator == null || !animator.isActiveAndEnabled)
                return;

            _simSpeed = simSpeed < 0f ? 0f : simSpeed;
            if (_runtime.FireGeneration != _seenGeneration)
            {
                _seenGeneration = _runtime.FireGeneration;
                FaceAim(_runtime.LastAimPoint);
                PlayAttack();
            }

            TickSequence(dt);
            ApplyAnimatorSpeed();
        }

        void PlayAttack()
        {
            var count = FireStepCount();
            if (!_playback.TryStart(count, out var playIndex))
                return;

            PlayFireStep(playIndex);
        }

        void TickSequence(float dt)
        {
            if (!_playback.IsPlaying)
                return;

            var count = FireStepCount();
            if (!_runtime.UsesAnimationActionEvent)
            {
                var strikeDelay = TowerAttackPlayback.ContactDelay(
                    _runtime.CurrentFireInterval,
                    strikeNormalized);
                if (_playback.TryTickWindup(dt, strikeDelay, count, out var windupIndex))
                {
                    PlayFireStep(windupIndex);
                    return;
                }

                if (strikeDelay > 0f && _playback.StepIndex == 0 && count > 1)
                    return;
            }

            if (animator.IsInTransition(0))
                return;

            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (string.IsNullOrEmpty(_playingState) || !info.IsName(_playingState))
                return;

            if (info.normalizedTime < 1f)
                return;

            if (_playback.TryAdvance(count, out var playIndex))
                PlayFireStep(playIndex);
            else
                PlayIdle();
        }

        void PlayFireStep(int playIndex)
        {
            PlayState(fireStates[playIndex]);
        }

        void PlayIdle()
        {
            if (string.IsNullOrEmpty(idleState))
                return;

            PlayState(idleState);
        }

        void PlayState(string stateName)
        {
            if (animator == null || string.IsNullOrEmpty(stateName))
                return;

            _playingState = stateName;
            if (crossFade > 0f)
                animator.CrossFadeInFixedTime(stateName, crossFade, 0, 0f);
            else
                animator.Play(stateName, 0, 0f);
        }

        void ApplyAnimatorSpeed()
        {
            if (animator == null)
                return;

            var clipSpeed = _playback.IsPlaying ? SpeedForStep(_playback.StepIndex) : 1f;
            animator.speed = TowerAttackPlayback.SimAnimatorSpeed(clipSpeed, _simSpeed);
        }

        void OnAnimationActionRaised(TowerAnimationEventRelay source, string action)
        {
            if (_runtime == null
                || !_runtime.UsesAnimationActionEvent
                || !_playback.IsPlaying
                || source == null
                || animator == null
                || source.gameObject != animator.gameObject
                || action != TowerAnimationEventRelay.ExecuteAction)
                return;

            _combatActionHandler?.Invoke(_runtime, _runtime.FireGeneration, action);
        }

        float SpeedForStep(int stepIndex)
        {
            if (_runtime == null || fireStates == null || stepIndex < 0 || stepIndex >= fireStates.Length)
                return 1f;

            if (_runtime.UsesAnimationActionEvent)
            {
                var totalLength = 0f;
                for (var i = 0; i < fireStates.Length; i++)
                    totalLength += AuthoredClipLength(fireStates[i]);
                return TowerAttackPlayback.ClipSpeed(
                    totalLength,
                    _runtime.CurrentFireInterval);
            }

            var length = AuthoredClipLength(fireStates[stepIndex]);
            var window = TowerAttackPlayback.ClipWindow(
                _runtime.CurrentFireInterval,
                strikeNormalized,
                stepIndex,
                FireStepCount());
            return TowerAttackPlayback.ClipSpeed(length, window);
        }

        float AuthoredClipLength(string stateName)
        {
            if (animator == null || string.IsNullOrEmpty(stateName))
                return 0f;

            var controller = animator.runtimeAnimatorController;
            if (controller == null)
                return 0f;

            var clips = controller.animationClips;
            if (clips == null)
                return 0f;

            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                if (clip != null && clip.name == stateName)
                    return clip.length;
            }

            return 0f;
        }

        void FaceAim(Vector3 worldPoint)
        {
            if (facingRoot == null)
                return;

            var dir = worldPoint - facingRoot.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                return;

            facingRoot.rotation = Quaternion.LookRotation(dir);
        }

        int FireStepCount()
        {
            return fireStates != null ? fireStates.Length : 0;
        }
    }
}
