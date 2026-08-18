using UnityEngine;
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

        void Awake()
        {
            if (root == null)
                root = GameCompositionRoot.Instance;
            if (popup == null)
                popup = GetComponentInChildren<PopupManager>(true);
            if (buildBar == null)
                buildBar = GetComponentInChildren<BuildBarController>(true);
            if (towerDetails == null)
                towerDetails = GetComponentInChildren<TowerDetailsController>(true);
            if (inventory == null)
                inventory = GetComponentInChildren<InventoryController>(true);
            if (draft == null)
                draft = GetComponentInChildren<DraftController>(true);
            if (topHud == null)
                topHud = GetComponentInChildren<TopHudController>(true);
            if (speedPanel == null)
                speedPanel = GetComponentInChildren<SpeedPanelController>(true);
            if (codex == null)
                codex = GetComponentInChildren<CodexController>(true);

            if (root == null)
            {
                Debug.LogError("RunHudBinder: assign GameCompositionRoot.", this);
                return;
            }

            if (popup == null)
                Debug.LogError("RunHudBinder: assign PopupManager.", this);
            else
                popup.Init(root.Speed);

            buildBar?.Bind(root);
            towerDetails?.Bind(root, popup);
            inventory?.Bind(root, popup);
            draft?.Bind(root, popup);
            topHud?.Bind(root, popup);
            speedPanel?.Bind(root);
            codex?.Bind(root);
        }
    }
}
