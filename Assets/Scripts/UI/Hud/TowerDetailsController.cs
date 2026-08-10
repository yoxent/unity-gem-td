using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GemTD.Gameplay;
using GemTD.Gameplay.Run;

namespace GemTD.UI
{
    /// <summary>Lives on TowerDetailsPanel prefab. Shows selected tower stats + 3 TowerGemSlots + Sell button.</summary>
    public sealed class TowerDetailsController : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text detailsText;
        [SerializeField] TowerGemSlot[] socketSlots = new TowerGemSlot[3];
        [SerializeField] Button sellButton;

        GameCompositionRoot _root;

        void Start()
        {
            if (panel == null) panel = gameObject;
            if (sellButton != null) sellButton.onClick.AddListener(OnSell);
        }

        void Update()
        {
            if (_root == null) _root = GameCompositionRoot.Instance;
            if (_root == null) return;

            var show = _root.HasSelectedTower && _root.States != null
                       && _root.States.Current != RunStateId.Defeat
                       && _root.States.Current != RunStateId.VictorySummary;

            panel.SetActive(show);
            if (!show) return;

            Refresh();
        }

        void Refresh()
        {
            if (detailsText != null) detailsText.text = _root.BuildSelectedTowerDetailsText();

            var plan = _root.States != null && _root.States.Current == RunStateId.Plan;
            if (sellButton != null) sellButton.interactable = plan && _root.HasSelectedTower;

            var tower = _root.Placement?.Selected;
            for (var i = 0; i < socketSlots.Length; i++)
            {
                if (socketSlots[i] == null || tower == null) continue;
                var gem = tower.Sockets != null && i < tower.Sockets.Length ? tower.Sockets[i] : null;
                socketSlots[i].Configure(_root, i, gem);
            }
        }

        void OnSell()
        {
            if (_root == null || !_root.HasSelectedTower) return;

            var popup = FindFirstObjectByType<PopupManager>(FindObjectsInactive.Include);
            if (popup == null) { _root.RequestSellSelected(); return; }

            popup.ShowConfirmOnceSuppressed(
                id: "SellConfirm",
                title: "Sell tower?",
                body: _root.SelectedHasSocketedGems
                    ? "This tower has a socketed gem; selling unsockets it back to your inventory. 50% refund."
                    : "50% refund of purchase + upgrade spend.",
                onConfirm: () => _root.RequestSellSelected(),
                pauseForFairness: false,
                yesText: "Yes", noText: "No");
        }
    }
}