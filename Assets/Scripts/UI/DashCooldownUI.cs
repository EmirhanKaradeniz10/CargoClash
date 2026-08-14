using CargoClash.Movement;
using UnityEngine;
using UnityEngine.UI;

namespace CargoClash.UI
{
    public sealed class DashCooldownUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GridDashController dashController;

        [SerializeField]
        private Image fillImage;

        [Header("Blocked State")]
        [SerializeField, Range(0f, 1f)]
        private float blockedAlpha = 0.35f;

        private void Update()
        {
            if (dashController == null ||
                fillImage == null)
            {
                return;
            }

            UpdateFill();
            UpdateAvailabilityVisual();
        }

        private void UpdateFill()
        {
            float duration =
                dashController.CooldownDuration;

            if (duration <= 0f)
            {
                fillImage.fillAmount = 1f;
                return;
            }

            float remaining =
                dashController.RemainingCooldown;

            fillImage.fillAmount =
                1f - Mathf.Clamp01(
                    remaining / duration);
        }

        private void UpdateAvailabilityVisual()
        {
            Color color =
                fillImage.color;

            color.a =
                dashController.CanDash
                    ? 1f
                    : blockedAlpha;

            fillImage.color = color;
        }
    }
}