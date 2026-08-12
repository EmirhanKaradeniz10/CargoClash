using UnityEngine;
using UnityEngine.InputSystem;

namespace CargoClash.Movement
{
    [RequireComponent(typeof(GridMovementController))]
    public sealed class HumanGridInput : MonoBehaviour
    {
        private GridMovementController movementController;
        private GridDashController dashController;

        private Vector2Int lastHeldDirection;

        private void Awake()
        {
            movementController =
                GetComponent<GridMovementController>();

            dashController =
                GetComponent<GridDashController>();
        }

        private void Update()
        {
            Keyboard keyboard =
                Keyboard.current;

            if (keyboard == null)
            {
                lastHeldDirection =
                    Vector2Int.zero;

                return;
            }

            if (dashController != null &&
                (dashController.IsDashQueued ||
                 movementController.IsDashing))
            {
                lastHeldDirection =
                    Vector2Int.zero;

                return;
            }

            Vector2Int currentDirection =
                ReadHeldDirection(keyboard);

            if (currentDirection == Vector2Int.zero)
            {
                lastHeldDirection =
                    Vector2Int.zero;

                return;
            }

            bool directionChanged =
                currentDirection !=
                lastHeldDirection;

            lastHeldDirection =
                currentDirection;

            if (directionChanged)
            {
                movementController.TryMove(
                    currentDirection);

                return;
            }

            if (!movementController.IsMoving)
            {
                movementController.TryMove(
                    currentDirection);
            }
        }

        private static Vector2Int ReadHeldDirection(
            Keyboard keyboard)
        {
            if (keyboard.wKey.isPressed ||
                keyboard.upArrowKey.isPressed)
            {
                return Vector2Int.up;
            }

            if (keyboard.sKey.isPressed ||
                keyboard.downArrowKey.isPressed)
            {
                return Vector2Int.down;
            }

            if (keyboard.aKey.isPressed ||
                keyboard.leftArrowKey.isPressed)
            {
                return Vector2Int.left;
            }

            if (keyboard.dKey.isPressed ||
                keyboard.rightArrowKey.isPressed)
            {
                return Vector2Int.right;
            }

            return Vector2Int.zero;
        }
    }
}