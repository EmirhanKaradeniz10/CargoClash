using UnityEngine;
using UnityEngine.InputSystem;

namespace CargoClash.Movement
{
    [RequireComponent(typeof(GridMovementController))]
    public sealed class HumanGridInput : MonoBehaviour
    {
        private GridMovementController movementController;

        private void Awake()
        {
            movementController =
                GetComponent<GridMovementController>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.wKey.wasPressedThisFrame ||
                keyboard.upArrowKey.wasPressedThisFrame)
            {
                movementController.TryMove(Vector2Int.up);
            }
            else if (keyboard.sKey.wasPressedThisFrame ||
                     keyboard.downArrowKey.wasPressedThisFrame)
            {
                movementController.TryMove(Vector2Int.down);
            }
            else if (keyboard.aKey.wasPressedThisFrame ||
                     keyboard.leftArrowKey.wasPressedThisFrame)
            {
                movementController.TryMove(Vector2Int.left);
            }
            else if (keyboard.dKey.wasPressedThisFrame ||
                     keyboard.rightArrowKey.wasPressedThisFrame)
            {
                movementController.TryMove(Vector2Int.right);
            }
        }
    }
}