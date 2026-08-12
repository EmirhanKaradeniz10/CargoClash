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

        private void Update()
        {
            if (dashController == null ||
                fillImage == null)
            {
                return;
            }

            float duration =
                dashController.CooldownDuration;

            if (duration <= 0f)
            {
                fillImage.fillAmount = 1f;
                return;
            }

            float remaining =
                dashController.RemainingCooldown;

            float normalized =
                1f - Mathf.Clamp01(
                    remaining / duration);

            fillImage.fillAmount =
                normalized;
        }
    }
}