using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GemTD.Core;
using GemTD.Gameplay;
using GemTD.Gameplay.Meta;

namespace GemTD.UI
{
    /// <summary>Lives on CodexPanel prefab. Shows/hides on CodexToggled event. Renders all CodexRow entries.</summary>
    public sealed class CodexController : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button closeButton;
        [SerializeField] CodexRowController[] rows;

        GameCompositionRoot _root;

        void Start()
        {
            if (panel == null) panel = gameObject;
            if (closeButton != null) closeButton.onClick.AddListener(() => _root?.ToggleCodexPanel());
            panel.SetActive(false);
        }

        void OnEnable() => GameEvents.CodexToggled += OnCodexToggled;
        void OnDisable() => GameEvents.CodexToggled -= OnCodexToggled;

        void OnCodexToggled()
        {
            if (_root == null) _root = GameCompositionRoot.Instance;
            if (_root == null) return;
            var open = _root.CodexPanelOpen;
            panel.SetActive(open);
            if (!open) return;
            var catalog = _root.CodexCatalog;
            var progress = _root.Codex;
            if (catalog == null || catalog.Entries == null) return;
            for (var i = 0; i < rows.Length && i < catalog.Entries.Length; i++)
            {
                if (rows[i] == null) continue;
                var entry = catalog.Entries[i];
                var unlocked = progress != null && progress.IsUnlocked(entry);
                rows[i].Configure(entry, unlocked);
            }
        }
    }
}