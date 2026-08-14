using CargoClash.Gameplay;
using UnityEngine;

namespace CargoClash.Gameplay.PowerUps
{
    public sealed class CoolingPowerUp : PowerUpBase
    {
        [Header("Cooling")]
        [SerializeField, Min(0f)]
        private float heatReduction = 35f;

        private bool isConsumed;

        private void OnTriggerEnter(Collider other)
        {
            if (isConsumed)
            {
                return;
            }

            CharacterHeatController heatController =
                other.GetComponent<CharacterHeatController>();

            if (heatController == null)
            {
                return;
            }

            if (heatController.CurrentHeat <= 0f)
            {
                return;
            }

            isConsumed = true;

            heatController.ReduceHeat(
                heatReduction);

            Debug.Log(
                $"{heatController.name} picked up Cooling Power-Up. " +
                $"Heat reduced by {heatReduction}.",
                this);

            ConsumePowerUp();
        }
    }
}