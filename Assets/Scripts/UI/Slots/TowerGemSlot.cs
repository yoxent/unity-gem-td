using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GemTD.Gameplay;
using GemTD.Gameplay.Gems;

namespace GemTD.UI
{
    /// <summary>Tower Details socketed-gem slot. Hover shows X; X click = instant unsocket. Prefab-based.</summary>
    public sealed class TowerGemSlot : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] Button xButton;
        [SerializeField] Button slotButton;
        [SerializeField] HoverPointerRelay slotHover;
        [SerializeField] HoverPointerRelay xHover;

        GameCompositionRoot _root;
        int _socketIndex = -1;
        GemDefinition _gem;

        public Button SlotButton => slotButton;

        void Awake()
        {
            if (xButton != null)
                xButton.onClick.AddListener(OnXClicked);

            if (slotHover == null)
            {
                Debug.LogError("TowerGemSlot: assign Slot Hover (HoverPointerRelay) on the prefab.", this);
                return;
            }

            HoverAffordance.BindXHover(
                slotHover,
                xHover,
                xButton != null ? xButton.gameObject : null,
                () => _root != null && _root.SelectedSocketLockRemaining <= 0f && _gem != null);
        }

        public void Configure(GameCompositionRoot root, int socketIndex, GemDefinition gem)
        {
            _root = root;
            _socketIndex = socketIndex;
            _gem = gem;
            if (icon != null) icon.color = gem != null ? Color.white : new Color(0.18f, 0.18f, 0.22f, 1f);
            if (nameLabel != null) nameLabel.text = gem != null ? gem.DisplayName : "—";
        }

        void OnXClicked()
        {
            if (_root == null)
                return;
            _root.RequestUnsocket(_socketIndex);
        }
    }
}