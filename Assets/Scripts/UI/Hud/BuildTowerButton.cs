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

        public void UpdateTowerButton(string label, int cost)
        {
            buildButtonLabel.text = label;
            buildButtonCost.text = cost.ToString();
        }
    }
}
