using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    public sealed class ToggleHandler : MonoBehaviour
    {
        [SerializeField] TMP_Text toggleLabel;
        [SerializeField] Toggle toggle;

        Action<bool> _onValueChanged;

        void Awake()
        {
            if (toggle == null)
            {
                Debug.LogError("ToggleHandler: toggle is not assigned.", this);
                return;
            }

            toggle.onValueChanged.AddListener(HandleToggleValueChanged);
        }

        void HandleToggleValueChanged(bool value)
        {
            _onValueChanged?.Invoke(value);
        }

        public void SetLabel(string text)
        {
            if (toggleLabel == null)
                return;
            toggleLabel.text = text;
        }

        public bool GetIsOn()
        {
            return toggle != null && toggle.isOn;
        }

        public void SetIsOn(bool isOn)
        {
            if (toggle == null)
                return;
            toggle.SetIsOnWithoutNotify(isOn);
        }

        public void BindOnValueChanged(Action<bool> onValueChanged)
        {
            _onValueChanged = onValueChanged;
        }

        public void SetInteractable(bool interactable)
        {
            if (toggle == null)
                return;
            toggle.interactable = interactable;
        }
    }
}
