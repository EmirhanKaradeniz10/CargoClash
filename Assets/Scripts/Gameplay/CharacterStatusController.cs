using System.Collections;
using CargoClash.Movement;
using UnityEngine;

namespace CargoClash.Gameplay
{
    [RequireComponent(typeof(GridMovementController))]
    public sealed class CharacterStatusController : MonoBehaviour
    {
        [Header("Dash Hit")]
        [SerializeField, Min(0f)]
        private float defaultStunDuration = 0.8f;

        [SerializeField, Min(0f)]
        private float defaultInvulnerabilityDuration = 1.5f;

        private GridMovementController movementController;

        private Coroutine dashStatusCoroutine;

        private bool isStunned;
        private bool isInvulnerable;

        public bool IsStunned => isStunned;

        public bool IsInvulnerable => isInvulnerable;

        private void Awake()
        {
            movementController =
                GetComponent<GridMovementController>();
        }

        public bool TryApplyDashHit()
        {
            if (isInvulnerable)
            {
                return false;
            }

            if (dashStatusCoroutine != null)
            {
                StopCoroutine(dashStatusCoroutine);
            }

            dashStatusCoroutine =
                StartCoroutine(DashStatusRoutine());

            return true;
        }

        private IEnumerator DashStatusRoutine()
        {
            isStunned = true;

            movementController.SetMovementLocked(true);
            movementController.ClearBufferedMovement();

            if (defaultStunDuration > 0f)
            {
                yield return new WaitForSeconds(
                    defaultStunDuration);
            }

            isStunned = false;
            isInvulnerable = true;

            movementController.SetMovementLocked(false);

            if (defaultInvulnerabilityDuration > 0f)
            {
                yield return new WaitForSeconds(
                    defaultInvulnerabilityDuration);
            }

            isInvulnerable = false;
            dashStatusCoroutine = null;
        }

        private void OnDisable()
        {
            if (dashStatusCoroutine != null)
            {
                StopCoroutine(dashStatusCoroutine);
                dashStatusCoroutine = null;
            }

            isStunned = false;
            isInvulnerable = false;

            if (movementController != null)
            {
                movementController.SetMovementLocked(false);
            }
        }
    }
}