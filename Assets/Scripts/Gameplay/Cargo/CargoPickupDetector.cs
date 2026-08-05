using UnityEngine;

namespace CargoClash.Gameplay.Cargo
{
    [RequireComponent(typeof(CargoCarrier))]
    public sealed class CargoPickupDetector : MonoBehaviour
    {
        private CargoCarrier cargoCarrier;

        private void Awake()
        {
            cargoCarrier =
                GetComponent<CargoCarrier>();
        }

        private void OnTriggerEnter(Collider other)
        {
            TryPickUpFromCollider(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryPickUpFromCollider(other);
        }

        private void TryPickUpFromCollider(
            Collider other)
        {
            if (cargoCarrier.IsCarrying)
            {
                return;
            }

            CargoItem cargo =
                other.GetComponent<CargoItem>();

            if (cargo == null ||
                cargo.IsCarried ||
                !cargo.CanBePickedUpBy(cargoCarrier))
            {
                return;
            }

            cargoCarrier.TryPickUp(cargo);
        }
    }
}