using UnityEngine;
using UnityEngine.UI;
using GemTD.Gameplay;
using GemTD.Gameplay.Gems;

namespace GemTD.UI
{
    /// <summary>Tower Details socketed-gem slot. Hover shows X; X click = instant unsocket.</summary>
    public sealed class TowerGemSlot : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] Text nameLabel;
        [SerializeField] Button xButton;
        [SerializeField] Button slotButton;

        GameCompositionRoot _root;
        int _socketIndex = -1;
        GemDefinition _gem;

        public void Configure(GameCompositionRoot root, int socketIndex, GemDefinition gem)
        {
            _root = root;
            _socketIndex = socketIndex;
            _gem = gem;
            if (icon != null) icon.color = gem != null ? Color.white : new Color(0.18f, 0.18f, 0.22f, 1f);
            if (nameLabel != null) nameLabel.text = gem != null ? gem.DisplayName : "—";
            HoverAffordance.BindXHover(slotButton, xButton != null ? xButton.gameObject : null, () =>
                _root != null && _root.SelectedSocketLockRemaining <= 0f && _gem != null);
        }

        void Awake()
        {
            if (xButton != null) xButton.onClick.AddListener(OnXClicked);
        }

        void OnXClicked()
        {
            if (_root == null)
                return;
            _root.RequestUnsocket(_socketIndex);
        }
    }
}