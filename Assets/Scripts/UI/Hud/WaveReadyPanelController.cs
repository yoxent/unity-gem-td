using LitMotion;
using UnityEngine;
using GemTD.Core;
using GemTD.Gameplay.Run;

namespace GemTD.UI
{
    /// <summary>
    /// Fades and elastically scales the ready call while <see cref="WaveController"/> holds the first spawn.
    /// Combat progress drives the motion, so pause and 2× / 4× stay locked to the spawn.
    /// Copy stays authored on the prefab.
    /// </summary>
    public sealed class WaveReadyPanelController : MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] RectTransform scaleTarget;
        [Tooltip("Share of the ready hold spent fading in.")]
        [SerializeField] float fadeInPortion = 0.08f;
        [Tooltip("Share of the ready hold spent scaling from small, past normal, and back to the authored size.")]
        [SerializeField] float scalePortion = 0.55f;
        [Tooltip("Share of the ready hold spent fading out. Scale stays at the authored size.")]
        [SerializeField] float fadeOutPortion = 0.22f;
        [SerializeField] float startScale = 0.35f;

        readonly ManualMotionDispatcher _clock = new ManualMotionDispatcher();
        MotionHandle _motion;

        void OnEnable()
        {
            if (canvasGroup == null || scaleTarget == null)
            {
                Debug.LogError(
                    "WaveReadyPanelController: assign Canvas Group and the scale RectTransform on ReadyForBattle.",
                    this);
                return;
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            Apply(0f);
            StartMotion();
            GameEvents.WaveReadyProgress += OnWaveReadyProgress;
        }

        void OnDisable()
        {
            GameEvents.WaveReadyProgress -= OnWaveReadyProgress;
            if (_motion.IsActive())
                _motion.Cancel();
            _clock.Reset();
        }

        void OnWaveReadyProgress(float progress)
        {
            if (!_motion.IsActive())
                StartMotion();

            var duration = WaveController.ReadyHoldSeconds;
            _clock.Time = Mathf.Clamp01(progress) * duration;
        }

        void StartMotion()
        {
            if (_motion.IsActive())
                _motion.Cancel();
            _clock.Reset();

            var duration = WaveController.ReadyHoldSeconds;
            if (duration <= 0f)
                return;

            _motion = LMotion.Create(0f, 1f, duration)
                .WithEase(Ease.Linear)
                .WithScheduler(_clock.Scheduler)
                .Bind(this, static (t, panel) =>
                {
                    if (panel != null)
                        panel.Apply(t);
                })
                .Preserve();
        }

        void Apply(float progress)
        {
            if (canvasGroup == null || scaleTarget == null)
                return;

            var t = Mathf.Clamp01(progress);
            canvasGroup.alpha = Alpha(t);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            var scale = Scale(t);
            scaleTarget.localScale = new Vector3(scale, scale, 1f);
        }

        float Alpha(float progress)
        {
            ResolveWindows(out var fadeIn, out var fadeOut, out _);
            if (progress <= 0f || progress >= 1f)
                return 0f;
            if (progress < fadeIn)
                return EaseUtility.Evaluate(progress / fadeIn, Ease.OutQuad);
            var fadeOutStart = 1f - fadeOut;
            if (fadeOut > 0f && progress >= fadeOutStart)
                return 1f - EaseUtility.Evaluate((progress - fadeOutStart) / fadeOut, Ease.InQuad);
            return 1f;
        }

        float Scale(float progress)
        {
            ResolveWindows(out _, out _, out var scaleWindow);
            if (progress >= scaleWindow)
                return 1f;

            var eased = EaseUtility.Evaluate(progress / scaleWindow, Ease.OutElastic);
            var scale = Mathf.LerpUnclamped(startScale, 1f, eased);
            return scale < 0.01f ? 0.01f : scale;
        }

        void ResolveWindows(out float fadeIn, out float fadeOut, out float scaleWindow)
        {
            fadeIn = Mathf.Max(0.0001f, fadeInPortion);
            fadeOut = Mathf.Max(0f, fadeOutPortion);
            var sum = fadeIn + fadeOut;
            if (sum > 1f)
            {
                fadeIn /= sum;
                fadeOut /= sum;
            }

            scaleWindow = Mathf.Max(fadeIn, scalePortion);
            var settleBy = 1f - fadeOut;
            if (scaleWindow > settleBy)
                scaleWindow = settleBy;
        }
    }
}
