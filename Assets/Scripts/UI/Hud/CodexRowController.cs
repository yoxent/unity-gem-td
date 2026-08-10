using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GemTD.Gameplay.Meta;

namespace GemTD.UI
{
    /// <summary>One Codex row (pre-placed per catalog entry). Flips locked↔unlocked display. Prefab-based.</summary>
    public sealed class CodexRowController : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] Image stateChip;
        [SerializeField] TMP_Text stateChipText;
        [SerializeField] TMP_Text bodyText;

        static readonly Color DiscoveredChip = new Color(0.14f, 0.45f, 0.20f, 1f);
        static readonly Color LockedChip = new Color(0.45f, 0.14f, 0.14f, 1f);
        static readonly Color Dim = new Color(0.7f, 0.7f, 0.72f, 0.65f);

        public void Configure(CodexEntry entry, bool unlocked)
        {
            if (entry == null) return;
            if (icon != null)
            {
                if (unlocked && entry.Icon != null) { icon.sprite = entry.Icon; icon.color = Color.white; icon.preserveAspect = true; }
                else icon.color = new Color(0.18f, 0.18f, 0.22f, 1f);
            }
            if (nameLabel != null) nameLabel.text = unlocked ? entry.DisplayName : "???";
            if (stateChip != null) stateChip.color = unlocked ? DiscoveredChip : LockedChip;
            if (stateChipText != null) stateChipText.text = unlocked ? "DISCOVERED" : "LOCKED";
            if (bodyText != null)
            {
                bodyText.text = unlocked ? (entry.UnlockedText ?? "") : (entry.LockedHint ?? "");
                bodyText.color = unlocked ? Color.white : Dim;
            }
        }
    }
}