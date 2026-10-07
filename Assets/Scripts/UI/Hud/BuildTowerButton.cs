using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GemTD.UI
{
    public class BuildTowerButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Button buildButton;
        [SerializeField] Image towerIcon;
        [SerializeField] TMP_Text towerLevelLabel;
        [SerializeField] TMP_Text buildButtonLabel;
        [SerializeField] TMP_Text buildButtonCost;

        [SerializeField] GameObject towerInfoPanel;
        [SerializeField] GameObject emptySlotPanel;

        [SerializeField] GameObject buttonBGPanel;
        [SerializeField] Sprite towerSlotSprite;
        [SerializeField] Sprite emptySlotSprite;
        [SerializeField] float towerPixelsPerUnitMultiplier = 9f;
        [SerializeField] float emptyPixelsPerUnitMultiplier = 4f;

        BuildTowerTooltip _tooltip;
        BuildTowerTooltipData _tooltipData;
        bool _hasTooltipData;
        bool _pointerOver;

        public Button GetButton()
        {
            return buildButton;
        }

        public Sprite GetTowerIconSprite()
        {
            return towerIcon != null ? towerIcon.sprite : null;
        }

        public void SetTooltip(BuildTowerTooltip tooltip)
        {
            if (_tooltip == tooltip)
                return;

            if (_pointerOver)
                _tooltip?.Hide();
            _tooltip = tooltip;
            RefreshTooltip();
        }

        public void SetTooltipData(BuildTowerTooltipData data)
        {
            _tooltipData = data;
            _hasTooltipData = true;
            RefreshTooltip();
        }

        public void BindEmpty()
        {
            if (_pointerOver)
                _tooltip?.Hide();
            _hasTooltipData = false;
            if (buildButtonLabel != null)
                buildButtonLabel.text = "—";
            if (buildButtonCost != null)
                buildButtonCost.text = "";
            if (towerLevelLabel != null)
                towerLevelLabel.text = "";
            ApplySlot(hasTower: false);
            if (buildButton != null)
                buildButton.interactable = false;
        }

        public void UpdateTowerButton(string label, int cost, int level)
        {
            if (buildButtonLabel != null)
                buildButtonLabel.text = label;
            if (buildButtonCost != null)
                buildButtonCost.text = $"{cost}g";
            if (towerLevelLabel != null)
                towerLevelLabel.text = $"Lv. {level}";
            ApplySlot(hasTower: true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _pointerOver = true;
            RefreshTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _pointerOver = false;
            _tooltip?.Hide();
        }

        void OnDisable()
        {
            if (_pointerOver)
                _tooltip?.Hide();
            _pointerOver = false;
            _hasTooltipData = false;
        }

        void RefreshTooltip()
        {
            if (_pointerOver && _hasTooltipData)
                _tooltip?.Show(_tooltipData);
        }

        void ApplySlot(bool hasTower)
        {
            if (towerInfoPanel != null)
                towerInfoPanel.SetActive(hasTower);
            if (emptySlotPanel != null)
                emptySlotPanel.SetActive(!hasTower);
            if (buttonBGPanel != null)
                buttonBGPanel.SetActive(hasTower);

            if (buildButton == null || buildButton.image == null)
                return;
            var image = buildButton.image;
            var sprite = hasTower ? towerSlotSprite : emptySlotSprite;
            if (sprite != null)
                image.sprite = sprite;
            image.pixelsPerUnitMultiplier = hasTower
                ? towerPixelsPerUnitMultiplier
                : emptyPixelsPerUnitMultiplier;
        }
    }
}
