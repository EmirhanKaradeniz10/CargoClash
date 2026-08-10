using CargoClash.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CargoClash.UI
{
    public sealed class HeatBarUI : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField]
        private CharacterHeatController heatController;

        [Header("UI")]
        [SerializeField]
        private Image fillImage;

        [SerializeField]
        private TMP_Text heatText;

        [Header("Display")]
        [SerializeField]
        private string label = "HEAT";

        [Header("Colors")]
        [SerializeField]
        private Color normalColor = new(
            0.15f,
            0.65f,
            1f,
            1f);

        [SerializeField]
        private Color warningColor = new(
            1f,
            0.55f,
            0.1f,
            1f);

        [SerializeField]
        private Color dangerColor = new(
            1f,
            0.1f,
            0.1f,
            1f);

        [SerializeField, Range(0f, 1f)]
        private float warningThreshold = 0.6f;

        private void Update()
        {
            if (heatController == null)
            {
                return;
            }

            UpdateFill();
            UpdateColor();
            UpdateText();
        }

        private void UpdateFill()
        {
            if (fillImage == null)
            {
                return;
            }

            fillImage.fillAmount =
                Mathf.Clamp01(
                    heatController.NormalizedHeat);
        }

        private void UpdateColor()
        {
            if (fillImage == null)
            {
                return;
            }

            if (heatController.IsOverheated)
            {
                fillImage.color = dangerColor;
                return;
            }

            float normalizedHeat =
                heatController.NormalizedHeat;

            if (normalizedHeat <= warningThreshold)
            {
                fillImage.color = normalColor;
                return;
            }

            float warningProgress =
                Mathf.InverseLerp(
                    warningThreshold,
                    1f,
                    normalizedHeat);

            fillImage.color =
                Color.Lerp(
                    warningColor,
                    dangerColor,
                    warningProgress);
        }

        private void UpdateText()
        {
            if (heatText == null)
            {
                return;
            }

            int currentHeat =
                Mathf.RoundToInt(
                    heatController.CurrentHeat);

            int maximumHeat =
                Mathf.RoundToInt(
                    heatController.MaximumHeat);

            heatText.text =
                $"{label} {currentHeat} / {maximumHeat}";
        }
    }
}