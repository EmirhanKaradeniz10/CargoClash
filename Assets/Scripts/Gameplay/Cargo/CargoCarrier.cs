using UnityEngine;

namespace CargoClash.Gameplay.Cargo
{
    public sealed class CargoCarrier : MonoBehaviour
    {
        [Header("Carry")]
        [SerializeField]
        private Transform carryPoint;

        private CargoItem carriedCargo;

        public CargoItem CarriedCargo => carriedCargo;

        public bool IsCarrying => carriedCargo != null;

        public bool TryPickUp(CargoItem cargo)
        {
            if (cargo == null || IsCarrying)
            {
                return false;
            }

            carriedCargo = cargo;

            cargo.RemoveFromSlot();
            cargo.SetCarried(this, carryPoint);

            return true;
        }

        public CargoItem DropCargo(Vector3 worldPosition)
        {
            if (!IsCarrying)
            {
                return null;
            }

            CargoItem droppedCargo = carriedCargo;
            carriedCargo = null;

            droppedCargo.SetDropped(worldPosition);

            return droppedCargo;
        }

        public CargoItem RemoveCarriedCargo()
        {
            if (!IsCarrying)
            {
                return null;
            }

            CargoItem removedCargo = carriedCargo;
            carriedCargo = null;

            return removedCargo;
        }
    }
}