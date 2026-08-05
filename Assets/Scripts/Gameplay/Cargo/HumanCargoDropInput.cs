using CargoClash.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CargoClash.Gameplay.Cargo
{
    [RequireComponent(typeof(CargoCarrier))]
    [RequireComponent(typeof(GridMovementController))]
    public sealed class HumanCargoDropInput : MonoBehaviour
    {
        [Header("Drop")]
        [SerializeField]
        private Key dropKey = Key.Q;

        [SerializeField, Min(0f)]
        private float dropHeight = 0.35f;

        private CargoCarrier cargoCarrier;
        private GridMovementController movementController;

        private void Awake()
        {
            cargoCarrier =
                GetComponent<CargoCarrier>();

            movementController =
                GetComponent<GridMovementController>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null ||
                !keyboard[dropKey].wasPressedThisFrame ||
                !cargoCarrier.IsCarrying)
            {
                return;
            }

            Vector2Int currentCell =
                movementController.CurrentCell;

            Vector3 dropPosition = new(
                currentCell.x,
                dropHeight,
                currentCell.y);

            cargoCarrier.DropCargo(dropPosition);
        }
    }
}