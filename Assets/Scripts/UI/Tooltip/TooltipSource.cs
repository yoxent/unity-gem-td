using UnityEngine;

namespace GemTD.UI
{
    /// <summary>Carries tooltip text for <see cref="TooltipController"/>. Drop on any icon; set text in inspector.</summary>
    [AddComponentMenu("Gem TD/UI/Tooltip Source")]
    public sealed class TooltipSource : MonoBehaviour, ITooltipable
    {
        [SerializeField] [TextArea] string text;
        public string TooltipText => text;
    }
}