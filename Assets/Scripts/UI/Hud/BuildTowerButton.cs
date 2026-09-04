using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    public class BuildTowerButton : MonoBehaviour
    {
        [SerializeField] Button buildButton;
        [SerializeField] TMP_Text buildButtonLabel;
        [SerializeField] TMP_Text buildButtonCost;

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
            if (buildButton != null)
                buildButton.interactable = false;
        }

        public void UpdateTowerButton(string label, int cost)
        {
            if (buildButtonLabel != null)
                buildButtonLabel.text = label;
            if (buildButtonCost != null)
                buildButtonCost.text = cost.ToString();
        }
    }
}
