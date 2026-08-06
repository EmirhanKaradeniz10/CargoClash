using CargoClash.Gameplay;
using CargoClash.Gameplay.Cargo;
using UnityEngine;

namespace CargoClash.Movement
{
    [RequireComponent(typeof(GridMovementController))]
    [RequireComponent(typeof(CargoCarrier))]
    [RequireComponent(typeof(PlayerIdentity))]
    public sealed class GridDashController : MonoBehaviour
    {
        [Header("Dash Distance")]
        [SerializeField, Min(0)]
        private int emptyDashDistance = 2;

        [SerializeField, Min(0)]
        private int carriedDashDistance = 1;

        [SerializeField, Min(0)]
        private int heavyCargoDashDistance = 0;

        [Header("Dash Timing")]
        [SerializeField, Min(0.1f)]
        private float cooldownDuration = 2f;

        [SerializeField, Min(1f)]
        private float dashSpeedMultiplier = 3f;

        [SerializeField, Min(0f)]
        private float recoveryDuration = 0.15f;

        [Header("Cargo Drop")]
        [SerializeField, Min(0f)]
        private float droppedCargoHeight = 0.35f;

        private GridMovementController movementController;
        private CargoCarrier cargoCarrier;
        private PlayerIdentity playerIdentity;

        private float nextAllowedDashTime;

        private bool dashQueued;
        private Vector2Int queuedDashDirection;

        public bool IsDashQueued => dashQueued;

        public bool IsOnCooldown =>
            Time.time < nextAllowedDashTime;

        public float RemainingCooldown =>
            Mathf.Max(
                0f,
                nextAllowedDashTime - Time.time);

        private void Update()
        {
            if (!dashQueued ||
                movementController.IsMoving)
            {
                return;
            }

            dashQueued = false;

            ExecuteDash(queuedDashDirection);
        }

        private void Awake()
        {
            movementController =
                GetComponent<GridMovementController>();

            cargoCarrier =
                GetComponent<CargoCarrier>();

            playerIdentity =
                GetComponent<PlayerIdentity>();
        }

        public bool TryDash()
        {
            if (IsOnCooldown ||
                dashQueued ||
                movementController.IsDashing)
            {
                return false;
            }

            int dashDistance =
                GetCurrentDashDistance();

            if (dashDistance <= 0)
            {
                return false;
            }

            Vector2Int requestedDirection =
                movementController.FacingDirection;

            movementController.ClearBufferedMovement();

            if (movementController.IsMoving)
            {
                dashQueued = true;
                queuedDashDirection = requestedDirection;

                return true;
            }

            return ExecuteDash(requestedDirection);
        }

        private bool ExecuteDash(
    Vector2Int direction)
        {
            if (IsOnCooldown ||
                movementController.IsMoving ||
                movementController.IsDashing)
            {
                return false;
            }

            int dashDistance =
                GetCurrentDashDistance();

            if (dashDistance <= 0)
            {
                return false;
            }

            bool dashStarted =
                movementController.TryDash(
                    direction,
                    dashDistance,
                    dashSpeedMultiplier,
                    recoveryDuration,
                    out GridMovementController hitTarget);

            if (!dashStarted)
            {
                return false;
            }

            nextAllowedDashTime =
                Time.time + cooldownDuration;

            if (hitTarget != null)
            {
                HandleDashHit(hitTarget);
            }

            return true;
        }

        private int GetCurrentDashDistance()
        {
            if (!cargoCarrier.IsCarrying)
            {
                return emptyDashDistance;
            }

            CargoItem cargo =
                cargoCarrier.CarriedCargo;

            if (cargo == null)
            {
                return emptyDashDistance;
            }

            return cargo.CargoType == CargoType.Heavy
                ? heavyCargoDashDistance
                : carriedDashDistance;
        }

        private void HandleDashHit(
    GridMovementController targetMovement)
        {
            PlayerIdentity targetIdentity =
                targetMovement.GetComponent<PlayerIdentity>();

            if (targetIdentity == null ||
                targetIdentity.Side ==
                playerIdentity.Side)
            {
                return;
            }

            CharacterStatusController targetStatus =
                targetMovement.GetComponent<
                    CharacterStatusController>();

            if (targetStatus == null)
            {
                Debug.LogError(
                    $"{targetIdentity.Side} does not have a " +
                    "CharacterStatusController.",
                    targetMovement);

                return;
            }

            bool hitApplied =
                targetStatus.TryApplyDashHit();

            if (!hitApplied)
            {
                Debug.Log(
                    $"{targetIdentity.Side} blocked the dash " +
                    "because it is invulnerable.",
                    this);

                return;
            }

            CargoCarrier targetCarrier =
                targetMovement.GetComponent<CargoCarrier>();

            bool droppedCargo =
                targetCarrier != null &&
                targetCarrier.IsCarrying;

            if (droppedCargo)
            {
                Vector2Int targetCell =
                    targetMovement.CurrentCell;

                Vector3 dropPosition = new(
                    targetCell.x,
                    droppedCargoHeight,
                    targetCell.y);

                targetCarrier.DropCargo(
                    dropPosition);
            }

            Debug.Log(
                $"{playerIdentity.Side} hit " +
                $"{targetIdentity.Side}. " +
                $"Cargo dropped: {droppedCargo}.",
                this);
        }

        private void OnValidate()
        {
            emptyDashDistance =
                Mathf.Max(0, emptyDashDistance);

            carriedDashDistance =
                Mathf.Max(0, carriedDashDistance);

            heavyCargoDashDistance =
                Mathf.Max(0, heavyCargoDashDistance);
        }
    }
}