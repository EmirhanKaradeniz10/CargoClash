using CargoClash.Gameplay;
using CargoClash.Gameplay.Cargo;
using CargoClash.Map;
using UnityEngine;

using CargoClash.Gameplay.PowerUps;

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
        [SerializeField, Min(0f)]
        private float cooldownDuration = 2f;

        [SerializeField, Min(1f)]
        private float dashSpeedMultiplier = 3f;

        [SerializeField, Min(0f)]
        private float recoveryDuration = 0.15f;

        [Header("Cargo Drop")]
        [SerializeField, Min(0f)]
        private float droppedCargoHeight = 0.35f;

        [SerializeField, Min(1)]
        private int maximumDropSearchDistance = 2;

        [SerializeField]
        private GridMapGenerator mapGenerator;

        private CharacterHeatController heatController;

        [SerializeField]
        private GridOccupancyManager occupancyManager;

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

        private void Awake()
        {
            movementController =
                GetComponent<GridMovementController>();

            cargoCarrier =
                GetComponent<CargoCarrier>();

            playerIdentity =
                GetComponent<PlayerIdentity>();

            heatController =
                GetComponent<CharacterHeatController>();

            if (mapGenerator == null)
            {
                mapGenerator =
                    FindAnyObjectByType<GridMapGenerator>();
            }

            if (occupancyManager == null)
            {
                occupancyManager =
                    FindAnyObjectByType<GridOccupancyManager>();
            }

            if (mapGenerator == null)
            {
                Debug.LogError(
                    "GridMapGenerator was not found.",
                    this);
            }

            if (occupancyManager == null)
            {
                Debug.LogError(
                    "GridOccupancyManager was not found.",
                    this);
            }
        }

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

        public bool TryDash()
        {
            return TryDash(
                movementController.FacingDirection);
        }

        public bool TryDash(Vector2Int direction)
        {
            if (IsOnCooldown ||
                dashQueued ||
                movementController.IsDashing ||
                (heatController != null &&
                 heatController.IsOverheated))
            {
                return false;
            }

            if (!IsCardinalDirection(direction))
            {
                return false;
            }

            int dashDistance =
                GetCurrentDashDistance();

            if (dashDistance <= 0)
            {
                return false;
            }

            movementController.ClearBufferedMovement();

            if (movementController.IsMoving)
            {
                dashQueued = true;
                queuedDashDirection = direction;

                return true;
            }

            return ExecuteDash(direction);
        }

        private bool ExecuteDash(
            Vector2Int direction)
        {
            if (IsOnCooldown ||
                movementController.IsMoving ||
                movementController.IsDashing ||
                (heatController != null &&
                 heatController.IsOverheated))
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
                HandleDashHit(
                    hitTarget,
                    direction);
            }

            heatController?.RegisterDash();

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

        private bool HandleDashHit(
    GridMovementController targetMovement,
    Vector2Int dashDirection)
        {
            PlayerIdentity targetIdentity =
                targetMovement.GetComponent<PlayerIdentity>();

            if (targetIdentity == null ||
                targetIdentity.Side ==
                playerIdentity.Side)
            {
                return false;
            }

            CharacterShieldController targetShield =
                targetMovement.GetComponent<
                    CharacterShieldController>();

            if (targetShield != null &&
                targetShield.TryConsumeShield())
            {
                Debug.Log(
                    $"{targetIdentity.Side} blocked the dash " +
                    "with a shield.",
                    this);

                return false;
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

                return false;
            }

            bool hitApplied =
                targetStatus.TryApplyDashHit();

            if (!hitApplied)
            {
                Debug.Log(
                    $"{targetIdentity.Side} blocked the dash " +
                    "because it is invulnerable.",
                    this);

                return false;
            }

            CargoCarrier targetCarrier =
                targetMovement.GetComponent<CargoCarrier>();

            bool droppedCargo =
                targetCarrier != null &&
                targetCarrier.IsCarrying;

            if (droppedCargo)
            {
                Vector2Int targetCell =
                    targetMovement.EffectiveCell;

                Vector2Int dropCell =
                    FindDropCell(
                        targetCell,
                        dashDirection);

                Vector3 dropPosition =
                    mapGenerator != null
                        ? mapGenerator.GridToWorld(dropCell)
                        : new Vector3(
                            dropCell.x,
                            0f,
                            dropCell.y);

                dropPosition.y =
                    droppedCargoHeight;

                targetCarrier.DropCargo(
                    dropPosition);

                Debug.Log(
                    $"Cargo dropped from {targetCell} " +
                    $"to {dropCell}.",
                    this);
            }

            Debug.Log(
                $"{playerIdentity.Side} hit " +
                $"{targetIdentity.Side}. " +
                $"Cargo dropped: {droppedCargo}.",
                this);

            return true;
        }

        private Vector2Int FindDropCell(
            Vector2Int targetCell,
            Vector2Int dashDirection)
        {
            Vector2Int rightDirection =
                new(
                    dashDirection.y,
                    -dashDirection.x);

            Vector2Int leftDirection =
                new(
                    -dashDirection.y,
                    dashDirection.x);

            Vector2Int[] preferredDirections =
            {
                dashDirection,
                rightDirection,
                leftDirection,
                -dashDirection
            };

            foreach (Vector2Int direction
                     in preferredDirections)
            {
                Vector2Int candidate =
                    targetCell + direction;

                if (IsValidDropCell(candidate))
                {
                    return candidate;
                }
            }

            for (int distance = 2;
                 distance <= maximumDropSearchDistance;
                 distance++)
            {
                for (int xOffset = -distance;
                     xOffset <= distance;
                     xOffset++)
                {
                    int yOffset =
                        distance -
                        Mathf.Abs(xOffset);

                    Vector2Int firstCandidate =
                        targetCell +
                        new Vector2Int(
                            xOffset,
                            yOffset);

                    if (IsValidDropCell(firstCandidate))
                    {
                        return firstCandidate;
                    }

                    if (yOffset == 0)
                    {
                        continue;
                    }

                    Vector2Int secondCandidate =
                        targetCell +
                        new Vector2Int(
                            xOffset,
                            -yOffset);

                    if (IsValidDropCell(secondCandidate))
                    {
                        return secondCandidate;
                    }
                }
            }

            Debug.LogWarning(
                $"No free drop cell was found near " +
                $"{targetCell}. Cargo will use the target cell.",
                this);

            return targetCell;
        }

        private bool IsValidDropCell(
            Vector2Int cell)
        {
            if (mapGenerator == null ||
                occupancyManager == null)
            {
                return false;
            }

            if (!mapGenerator.IsWalkable(cell))
            {
                return false;
            }

            if (occupancyManager.IsOccupied(
                    cell,
                    null))
            {
                return false;
            }

            return true;
        }

        private static bool IsCardinalDirection(
            Vector2Int direction)
        {
            return direction == Vector2Int.up ||
                   direction == Vector2Int.down ||
                   direction == Vector2Int.left ||
                   direction == Vector2Int.right;
        }

        private void OnValidate()
        {
            emptyDashDistance =
                Mathf.Max(0, emptyDashDistance);

            carriedDashDistance =
                Mathf.Max(0, carriedDashDistance);

            heavyCargoDashDistance =
                Mathf.Max(0, heavyCargoDashDistance);

            maximumDropSearchDistance =
                Mathf.Max(
                    1,
                    maximumDropSearchDistance);
        }
    }
}