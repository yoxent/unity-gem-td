using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    public class BuildTowerButton : MonoBehaviour
    {
        [SerializeField] Button buildButton;
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

        public Button GetButton()
        {
            return buildButton;
        }

        public void BindEmpty()
        {
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
                buildButtonCost.text = cost.ToString();
            if (towerLevelLabel != null)
                towerLevelLabel.text = $"Lv. {level}";
            ApplySlot(hasTower: true);
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
