using UnityEngine;
using UnityEngine.InputSystem;

namespace CargoClash.Movement
{
    [RequireComponent(typeof(GridDashController))]
    public sealed class HumanDashInput : MonoBehaviour
    {
        [SerializeField]
        private Key dashKey = Key.E;

        private GridDashController dashController;

        private void Awake()
        {
            dashController =
                GetComponent<GridDashController>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null ||
                !keyboard[dashKey].wasPressedThisFrame)
            {
                return;
            }

            dashController.TryDash();
        }
    }
}