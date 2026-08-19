using UnityEngine;
using GemTD.Core;
using GemTD.Gameplay;

namespace GemTD.UI
{
    /// <summary>Inspector-wires Run HUD controllers to GameCompositionRoot and PopupManager.</summary>
    public sealed class RunHudBinder : MonoBehaviour
    {
        [SerializeField] GameCompositionRoot root;
        [SerializeField] PopupManager popup;
        [SerializeField] BuildBarController buildBar;
        [SerializeField] TowerDetailsController towerDetails;
        [SerializeField] InventoryController inventory;
        [SerializeField] DraftController draft;
        [SerializeField] TopHudController topHud;
        [SerializeField] SpeedPanelController speedPanel;
        [SerializeField] CodexController codex;
        [SerializeField] SettingsController settings;

        void Awake()
        {
            if (root == null)
            {
                Debug.LogError("RunHudBinder: root is not assigned.", this);
                return;
            }

            if (popup == null) Debug.LogError("RunHudBinder: popup is not assigned.", this);
            if (buildBar == null) Debug.LogError("RunHudBinder: buildBar is not assigned.", this);
            if (towerDetails == null) Debug.LogError("RunHudBinder: towerDetails is not assigned.", this);
            if (inventory == null) Debug.LogError("RunHudBinder: inventory is not assigned.", this);
            if (draft == null) Debug.LogError("RunHudBinder: draft is not assigned.", this);
            if (topHud == null) Debug.LogError("RunHudBinder: topHud is not assigned.", this);
            if (speedPanel == null) Debug.LogError("RunHudBinder: speedPanel is not assigned.", this);
            if (codex == null) Debug.LogError("RunHudBinder: codex is not assigned.", this);
            if (settings == null) Debug.LogError("RunHudBinder: settings is not assigned.", this);

            if (popup != null) popup.Init(root.Speed);

            buildBar?.Bind(root);
            towerDetails?.Bind(root, popup);
            inventory?.Bind(root, popup);
            draft?.Bind(root, popup);
            topHud?.Bind(root, popup);
            speedPanel?.Bind(root);
            codex?.Bind(root);

            GameSettings.ApplyAudio();
            if (settings != null)
            {
                settings.BindSpeed(root.Speed);
                topHud?.BindSettings(settings);
            }
        }
    }
}
