using UnityEngine;
using UnityEngine.UI;
using GemTD.Gameplay;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;

namespace GemTD.UI
{
    /// <summary>Inventory bar slot. Hover shows X; X click opens a discard confirm popup.</summary>
    public sealed class InventoryGemSlot : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] Text nameLabel;
        [SerializeField] Button xButton;
        [SerializeField] Button slotButton;

        GameCompositionRoot _root;
        PopupManager _popup;
        int _slotIndex = -1;
        GemDefinition _gem;

        public void Configure(GameCompositionRoot root, PopupManager popup, int slotIndex, GemDefinition gem)
        {
            _root = root;
            _popup = popup;
            _slotIndex = slotIndex;
            _gem = gem;
            if (icon != null) icon.color = gem != null ? Color.white : new Color(0.18f, 0.18f, 0.22f, 1f);
            if (nameLabel != null) nameLabel.text = gem != null ? gem.DisplayName : "—";
            HoverAffordance.BindXHover(slotButton, xButton != null ? xButton.gameObject : null, () =>
                _root != null && _root.States != null
                && _root.States.Current == RunStateId.Plan && _gem != null);
        }

        void Awake()
        {
            if (xButton != null) xButton.onClick.AddListener(OnXClicked);
        }

        void OnXClicked()
        {
            if (_root == null || _popup == null || _gem == null)
                return;
            var gemName = _gem.DisplayName;
            var idx = _slotIndex;
            _popup.ShowConfirmOnceSuppressed(
                id: "DiscardGem",
                title: "Discard gem?",
                body: $"{gemName} will be lost.",
                onConfirm: () => _root.RequestDiscardAt(idx),
                pauseForFairness: true,
                yesText: "Yes", noText: "No");
        }
    }
}