using System.Collections;
using CargoClash.Movement;
using UnityEngine;

using UnityEngine.InputSystem;



namespace CargoClash.Gameplay.PowerUps
{
    public sealed class CharacterSpeedBoostController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private GridMovementController movementController;

        [Header("Speed Boost")]
        [SerializeField, Min(1f)]
        private float speedMultiplier = 1.35f;

        [SerializeField, Min(0.1f)]
        private float duration = 5f;

        private Coroutine boostCoroutine;

        public bool IsSpeedBoostActive =>
            boostCoroutine != null;

        private void Awake()
        {
            if (movementController == null)
            {
                movementController =
                    GetComponent<GridMovementController>();
            }

            if (movementController == null)
            {
                Debug.LogError(
                    $"{name}: GridMovementController was not found.",
                    this);

                enabled = false;
            }
        }

        public bool TryActivateSpeedBoost()
        {
            if (boostCoroutine != null)
            {
                return false;
            }

            movementController
                .SetPowerUpSpeedMultiplier(
                    speedMultiplier);

            boostCoroutine =
                StartCoroutine(
                    SpeedBoostRoutine());

            Debug.Log(
                $"{name} activated Speed Boost.",
                this);

            return true;
        }

        public void ResetSpeedBoost()
        {
            if (boostCoroutine != null)
            {
                StopCoroutine(
                    boostCoroutine);

                boostCoroutine = null;
            }

            movementController
                .SetPowerUpSpeedMultiplier(1f);
        }

        private IEnumerator SpeedBoostRoutine()
        {
            yield return new WaitForSeconds(duration);

            movementController
                .SetPowerUpSpeedMultiplier(1f);

            boostCoroutine = null;

            Debug.Log(
                $"{name} Speed Boost ended.",
                this);
        }

        private void OnDisable()
        {
            ResetSpeedBoost();
        }
    }
}