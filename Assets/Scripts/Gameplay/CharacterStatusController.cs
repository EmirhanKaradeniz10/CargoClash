using System.Collections;
using CargoClash.Movement;
using UnityEngine;

namespace CargoClash.Gameplay
{
    [RequireComponent(typeof(GridMovementController))]
    public sealed class CharacterStatusController : MonoBehaviour
    {
        [Header("Status")]
        [SerializeField, Min(0f)]
        private float defaultStunDuration = 0.6f;

        [SerializeField, Min(0f)]
        private float defaultInvulnerabilityDuration = 1f;

        private GridMovementController movementController;

        private Coroutine statusCoroutine;

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

            if (statusCoroutine != null)
            {
                StopCoroutine(statusCoroutine);
            }

            statusCoroutine =
                StartCoroutine(StatusRoutine());

            return true;
        }

        private IEnumerator StatusRoutine()
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
            statusCoroutine = null;
        }

        private void OnDisable()
        {
            if (statusCoroutine != null)
            {
                StopCoroutine(statusCoroutine);
                statusCoroutine = null;
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