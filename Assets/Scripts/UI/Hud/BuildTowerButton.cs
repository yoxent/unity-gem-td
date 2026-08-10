using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    public class BuildTowerButton : MonoBehaviour
    {
        [SerializeField] Button buildButton;
        [SerializeField] TMP_Text buildButtonLabel;

        public Button GetButton()
        {
            return buildButton;
        }

        public void UpdateLabel(string label)
        {
            buildButtonLabel.text = label;
        }
    }
}
