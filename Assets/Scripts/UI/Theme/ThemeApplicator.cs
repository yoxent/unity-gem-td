using TMPro;
using UnityEngine;

namespace GemTD.UI
{
    /// <summary>Labels that share one theme style.</summary>
    [System.Serializable]
    public struct ThemeTextTarget
    {
        public TMP_Text[] texts;
        [ThemeStyleId]
        public string styleId;
    }

    /// <summary>Applies a theme asset to the text components assigned in the inspector.</summary>
    public sealed class ThemeApplicator : MonoBehaviour
    {
        [SerializeField] ThemeManager theme;
        [SerializeField] ThemeTextTarget[] targets;

        void OnValidate()
        {
            if (targets == null)
                return;

            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                var lower = ThemeManager.NormalizeId(target.styleId);
                if (lower == target.styleId)
                    continue;

                target.styleId = lower;
                targets[i] = target;
            }
        }

        void Awake() => Apply();

        public void Apply()
        {
            if (targets == null || targets.Length == 0)
                return;

            if (theme == null)
            {
                Debug.LogError("ThemeApplicator: assign a Theme asset.", this);
                return;
            }

            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                var texts = target.texts;
                if (texts == null || texts.Length == 0)
                    continue;

                for (var t = 0; t < texts.Length; t++)
                {
                    if (texts[t] == null)
                    {
                        Debug.LogError($"ThemeApplicator: Targets element {i} text {t} is not assigned.", this);
                        continue;
                    }

                    theme.Apply(texts[t], target.styleId, gameObject);
                }
            }
        }
    }
}
