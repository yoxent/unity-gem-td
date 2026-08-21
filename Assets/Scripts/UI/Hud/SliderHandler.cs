using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    public class SliderHandler : MonoBehaviour
    {
        [SerializeField] TMP_Text sliderLabel;
        [SerializeField] Slider slider;
        [SerializeField] TMP_Text valueLabel;

        Action<float> _onValueChanged;

        void Awake()
        {
            if (slider == null)
            {
                Debug.LogError("SliderHandler: slider is not assigned.", this);
                return;
            }

            slider.onValueChanged.AddListener(HandleSliderValueChanged);
            RefreshValueLabel(slider.value);
        }

        void HandleSliderValueChanged(float v)
        {
            RefreshValueLabel(v);
            _onValueChanged?.Invoke(v);
        }

        void RefreshValueLabel(float value01)
        {
            if (valueLabel == null) return;
            valueLabel.text = Mathf.RoundToInt(value01 * 100f) + "%";
        }

        public void SetLabel(string text)
        {
            if (sliderLabel == null) return;
            sliderLabel.text = text;
        }

        public float GetValue01()
        {
            return slider != null ? slider.value : 0f;
        }

        public void SetValue01(float value01)
        {
            if (slider == null) return;
            var v = Mathf.Clamp01(value01);
            slider.SetValueWithoutNotify(v);
            RefreshValueLabel(v);
        }

        public void BindOnValueChanged(Action<float> onValueChanged)
        {
            _onValueChanged = onValueChanged;
        }

        public void SetInteractable(bool interactable)
        {
            if (slider == null) return;
            slider.interactable = interactable;
        }
    }
}
