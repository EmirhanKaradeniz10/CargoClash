using UnityEngine;

namespace CargoClash.Gameplay.PowerUps
{
    public sealed class SpeedBoostPowerUp : PowerUpBase
    {
        private bool isConsumed;

        private void OnTriggerEnter(Collider other)
        {
            if (isConsumed)
            {
                return;
            }

            CharacterSpeedBoostController speedController =
                other.GetComponent<CharacterSpeedBoostController>();

            if (speedController == null)
            {
                return;
            }

            if (!speedController.TryActivateSpeedBoost())
            {
                return;
            }

            isConsumed = true;

            Debug.Log(
                $"{speedController.name} picked up Speed Boost.",
                this);

            ConsumePowerUp();
        }
    }
}