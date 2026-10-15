using LitMotion;
using UnityEngine;
using GemTD.Core;

namespace GemTD.UI
{
    /// <summary>
    /// Fades the wave-clear panel in while the hold is showing, then fades it out.
    /// The panel stays active so the next clear can show it again. Text stays authored on the prefab.
    /// </summary>
    public sealed class ResultMessagePanelController : MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] float fadeSeconds = 0.25f;

        MotionHandle _fade;

        void Awake()
        {
            GameEvents.WaveClearHoldChanged += OnWaveClearHoldChanged;
            if (canvasGroup == null)
            {
                Debug.LogError(
                    "ResultMessagePanelController: assign Canvas Group on the prefab.",
                    this);
                return;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        void OnDestroy()
        {
            GameEvents.WaveClearHoldChanged -= OnWaveClearHoldChanged;
            CancelFade();
        }

        void OnWaveClearHoldChanged(bool showing)
        {
            if (canvasGroup == null)
                return;

            if (showing && !canvasGroup.gameObject.activeSelf)
                canvasGroup.gameObject.SetActive(true);

            FadeTo(showing ? 1f : 0f);
        }

        void FadeTo(float target)
        {
            CancelFade();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (fadeSeconds <= 0f || Mathf.Approximately(canvasGroup.alpha, target))
            {
                canvasGroup.alpha = target;
                return;
            }

            var group = canvasGroup;
            _fade = LMotion.Create(group.alpha, target, fadeSeconds)
                .WithEase(Ease.OutQuad)
                .Bind(group, static (alpha, g) =>
                {
                    if (g != null)
                        g.alpha = alpha;
                });
        }

        void CancelFade()
        {
            if (_fade.IsActive())
                _fade.Cancel();
        }
    }
}
