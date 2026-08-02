using UnityEngine;

namespace CargoClash.Gameplay.Cargo
{
    [RequireComponent(typeof(CargoCarrier))]
    public sealed class CargoPickupDetector : MonoBehaviour
    {
        private CargoCarrier cargoCarrier;

        private void Awake()
        {
            cargoCarrier = GetComponent<CargoCarrier>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (cargoCarrier.IsCarrying)
            {
                return;
            }

            CargoItem cargo =
                other.GetComponent<CargoItem>();

            if (cargo == null || cargo.IsCarried)
            {
                return;
            }

            cargoCarrier.TryPickUp(cargo);
        }
    }
}