using UnityEngine;
using UnityEngine.InputSystem;

namespace CargoClash.Movement
{
    [RequireComponent(typeof(GridMovementController))]
    public sealed class HumanGridInput : MonoBehaviour
    {
        [Header("Held Input")]
        [SerializeField, Min(0f)]
        private float initialRepeatDelay = 0.18f;

        [SerializeField, Min(0.01f)]
        private float repeatInterval = 0.08f;

        private GridMovementController movementController;

        private Vector2Int heldDirection;
        private float nextRepeatTime;

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
                ResetHeldInput();
                return;
            }

            Vector2Int currentDirection =
                ReadHeldDirection(keyboard);

            if (currentDirection == Vector2Int.zero)
            {
                ResetHeldInput();
                return;
            }

            bool directionChanged =
                currentDirection != heldDirection;

            if (directionChanged)
            {
                heldDirection = currentDirection;

                movementController.TryMove(heldDirection);

                nextRepeatTime =
                    Time.time + initialRepeatDelay;

                return;
            }

            if (Time.time < nextRepeatTime)
            {
                return;
            }

            movementController.TryMove(heldDirection);

            nextRepeatTime =
                Time.time + repeatInterval;
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

        private void ResetHeldInput()
        {
            heldDirection = Vector2Int.zero;
            nextRepeatTime = 0f;
        }

        private void OnValidate()
        {
            if (repeatInterval > initialRepeatDelay &&
                initialRepeatDelay > 0f)
            {
                repeatInterval = initialRepeatDelay;
            }
        }
    }
}