using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Plays per-prefab idle + 1..N fire states when the bound tower fires.
    /// FireInterval is the whole action. Clips stretch to fill their window. Idle stays at speed 1.
    /// Spawn and bow Draw→Release both use strikeNormalized of that interval.
    /// </summary>
    public sealed class TowerAnimatorView : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [Tooltip("Character to yaw toward the target. Tower root stays fixed.")]
        [SerializeField] Transform facingRoot;
        [SerializeField] string idleState;
        [SerializeField] string[] fireStates;
        [SerializeField] float crossFade = 0.1f;
        [Tooltip("0–1 contact pose in the current FireInterval. Combat spawn and the Draw→Release switch use this.")]
        [SerializeField] [Range(0f, 1f)] float strikeNormalized = 1f;

        TowerInstance _runtime;
        int _seenGeneration;
        TowerAttackPlayback _playback;
        string _playingState;

        public void Bind(TowerInstance runtime)
        {
            _runtime = runtime;
            if (_runtime != null)
                _runtime.StrikeNormalized = strikeNormalized;
            _seenGeneration = runtime != null ? runtime.FireGeneration : 0;
            _playback.Stop();
            PlayIdle();
        }

        void LateUpdate()
        {
            if (_runtime == null || animator == null || !animator.isActiveAndEnabled)
                return;

            if (_runtime.FireGeneration != _seenGeneration)
            {
                _seenGeneration = _runtime.FireGeneration;
                FaceAim(_runtime.LastAimPoint);
                PlayAttack();
            }

            TickSequence();
        }

        void PlayAttack()
        {
            var count = FireStepCount();
            if (!_playback.TryStart(count, out var playIndex))
                return;

            PlayFireStep(playIndex);
        }

        void TickSequence()
        {
            if (!_playback.IsPlaying)
                return;

            var count = FireStepCount();
            var strikeDelay = TowerAttackPlayback.ContactDelay(
                _runtime.CurrentFireInterval,
                strikeNormalized);
            if (_playback.TryTickWindup(Time.deltaTime, strikeDelay, count, out var windupIndex))
            {
                PlayFireStep(windupIndex);
                return;
            }

            if (strikeDelay > 0f && _playback.StepIndex == 0 && count > 1)
                return;

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
            SetClipSpeed(SpeedForStep(playIndex));
            PlayState(fireStates[playIndex]);
        }

        void PlayIdle()
        {
            if (string.IsNullOrEmpty(idleState))
                return;

            SetClipSpeed(1f);
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

        void SetClipSpeed(float speed)
        {
            if (animator == null)
                return;
            animator.speed = speed;
        }

        float SpeedForStep(int stepIndex)
        {
            if (_runtime == null || fireStates == null || stepIndex < 0 || stepIndex >= fireStates.Length)
                return 1f;

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
