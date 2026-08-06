using System.Collections.Generic;
using CargoClash.Gameplay;
using CargoClash.Gameplay.Cargo;
using UnityEngine;

namespace CargoClash.Movement
{
    [RequireComponent(typeof(GridMovementController))]
    [RequireComponent(typeof(CargoCarrier))]
    [RequireComponent(typeof(PlayerIdentity))]

    public sealed class ScriptedGridBot : MonoBehaviour
    {

        private enum BotGoal
        {
            None,
            Cargo,
            HomeDelivery
        }

        private static readonly Vector2Int[] CardinalDirections =
        {
            Vector2Int.zero,
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        [Header("Decision Timing")]
        [SerializeField, Min(0.05f)]
        private float decisionDelay = 0.15f;

        [Header("Dependencies")]
        [SerializeField]
        private GridPathfinder pathfinder;

        [SerializeField]
        private DeliveryZone homeDeliveryZone;

        private GridMovementController movementController;
        private CargoCarrier cargoCarrier;
        private PlayerIdentity playerIdentity;

        private CargoSpawnSlot targetCargoSlot;

        private CharacterStatusController statusController;

        private GridDashController dashController;

        private PlayerIdentity opposingPlayer;
        private GridMovementController opposingMovement;
        private CargoCarrier opposingCargoCarrier;
        private CharacterStatusController opposingStatus;

        private List<Vector2Int> currentPath = new();
        private int pathIndex;

        private BotGoal currentGoal = BotGoal.None;
        private float nextDecisionTime;

        private void Awake()
        {
            statusController =
                GetComponent<CharacterStatusController>();

            movementController =
                GetComponent<GridMovementController>();

            cargoCarrier =
                GetComponent<CargoCarrier>();

            playerIdentity =
                GetComponent<PlayerIdentity>();

            dashController =
                GetComponent<GridDashController>();

            if (pathfinder == null)
            {
                pathfinder =
                    FindAnyObjectByType<GridPathfinder>();
            }

            if (homeDeliveryZone == null)
            {
                FindHomeDeliveryZone();
            }

            FindOpposingPlayer();
        }

        private void Start()
        {
            if (!ValidateDependencies())
            {
                enabled = false;
                return;
            }

            ScheduleNextDecision();
        }

        private void Update()
        {
            if (statusController != null &&
                statusController.IsStunned)
            {
                return;
            }

            if (Time.time < nextDecisionTime ||
                movementController.IsMoving ||
                movementController.IsDashing)
            {
                return;
            }

            if (TryDashAtOpposingPlayer())
            {
                ClearCurrentPath();
                ScheduleNextDecision();
                return;
            }

            BotGoal desiredGoal =
                cargoCarrier.IsCarrying
                    ? BotGoal.HomeDelivery
                    : BotGoal.Cargo;

            bool goalChanged =
                currentGoal != desiredGoal;

            bool goalInvalid =
                !IsCurrentGoalValid();

            bool pathFinished =
                pathIndex >= currentPath.Count;

            if (goalChanged ||
                goalInvalid ||
                pathFinished)
            {
                SelectGoal(desiredGoal);
            }

            FollowCurrentPath();
            ScheduleNextDecision();
        }

        private void SelectGoal(BotGoal desiredGoal)
        {
            ClearCurrentPath();

            switch (desiredGoal)
            {
                case BotGoal.Cargo:
                    SelectCargoTarget();
                    break;

                case BotGoal.HomeDelivery:
                    SelectHomeDeliveryTarget();
                    break;

                default:
                    currentGoal = BotGoal.None;
                    break;
            }
        }

        private void SelectCargoTarget()
        {
            CargoSpawnSlot[] cargoSlots =
                FindObjectsByType<CargoSpawnSlot>();

            List<Vector2Int> bestPath = null;
            CargoSpawnSlot bestSlot = null;

            foreach (CargoSpawnSlot cargoSlot in cargoSlots)
            {
                if (cargoSlot == null ||
                    !cargoSlot.IsOccupied ||
                    cargoSlot.CurrentCargo == null ||
                    cargoSlot.CurrentCargo.IsCarried)
                {
                    continue;
                }

                List<Vector2Int> candidatePath =
                    pathfinder.FindPath(
                        movementController.CurrentCell,
                        cargoSlot.Cell,
                        movementController);

                if (candidatePath.Count == 0)
                {
                    continue;
                }

                if (bestPath == null ||
                    candidatePath.Count < bestPath.Count)
                {
                    bestPath = candidatePath;
                    bestSlot = cargoSlot;
                }
            }

            if (bestPath == null || bestSlot == null)
            {
                currentGoal = BotGoal.None;
                targetCargoSlot = null;
                return;
            }

            currentGoal = BotGoal.Cargo;
            targetCargoSlot = bestSlot;
            currentPath = bestPath;
            pathIndex = 0;
        }

        private void SelectHomeDeliveryTarget()
        {
            if (homeDeliveryZone == null)
            {
                currentGoal = BotGoal.None;
                return;
            }

            Vector2Int currentCell =
                movementController.CurrentCell;

            if (homeDeliveryZone.IsInDeliveryRange(currentCell))
            {
                currentGoal = BotGoal.HomeDelivery;
                targetCargoSlot = null;
                return;
            }

            List<Vector2Int> bestPath = null;

            foreach (Vector2Int offset in CardinalDirections)
            {
                Vector2Int candidateCell =
                    homeDeliveryZone.CenterCell + offset;

                if (!homeDeliveryZone.IsInDeliveryRange(
                        candidateCell))
                {
                    continue;
                }

                List<Vector2Int> candidatePath =
                    pathfinder.FindPath(
                        currentCell,
                        candidateCell,
                        movementController);

                if (candidatePath.Count == 0)
                {
                    continue;
                }

                if (bestPath == null ||
                    candidatePath.Count < bestPath.Count)
                {
                    bestPath = candidatePath;
                }
            }

            if (bestPath == null)
            {
                currentGoal = BotGoal.None;
                return;
            }

            currentGoal = BotGoal.HomeDelivery;
            targetCargoSlot = null;
            currentPath = bestPath;
            pathIndex = 0;
        }

        private bool IsCurrentGoalValid()
        {
            switch (currentGoal)
            {
                case BotGoal.Cargo:
                    return !cargoCarrier.IsCarrying &&
                           targetCargoSlot != null &&
                           targetCargoSlot.IsOccupied &&
                           targetCargoSlot.CurrentCargo != null &&
                           !targetCargoSlot.CurrentCargo.IsCarried;

                case BotGoal.HomeDelivery:
                    return cargoCarrier.IsCarrying &&
                           homeDeliveryZone != null;

                default:
                    return false;
            }
        }

        private void FollowCurrentPath()
        {
            if (pathIndex >= currentPath.Count)
            {
                return;
            }

            Vector2Int nextCell =
                currentPath[pathIndex];

            Vector2Int direction =
                nextCell -
                movementController.CurrentCell;

            if (movementController.TryMove(direction))
            {
                pathIndex++;
                return;
            }

            // Yol geçici olarak kapandı.
            // Sonraki karar döngüsünde aynı hedef için
            // yeniden rota hesaplanır.
            ClearCurrentPath();
        }

        private void FindHomeDeliveryZone()
        {
            DeliveryZone[] deliveryZones =
                FindObjectsByType<DeliveryZone>();

            foreach (DeliveryZone zone in deliveryZones)
            {
                if (zone.Owner != playerIdentity.Side ||
                    zone.ZoneType != DeliveryZoneType.Home)
                {
                    continue;
                }

                homeDeliveryZone = zone;
                return;
            }
        }

        private void FindOpposingPlayer()
        {
            PlayerIdentity[] players =
                FindObjectsByType<PlayerIdentity>();

            foreach (PlayerIdentity candidate in players)
            {
                if (candidate == null ||
                    candidate == playerIdentity ||
                    candidate.Side == playerIdentity.Side)
                {
                    continue;
                }

                opposingPlayer = candidate;

                opposingMovement =
                    candidate.GetComponent<GridMovementController>();

                opposingCargoCarrier =
                    candidate.GetComponent<CargoCarrier>();

                opposingStatus =
                    candidate.GetComponent<CharacterStatusController>();

                return;
            }
        }

        private bool TryDashAtOpposingPlayer()
        {
            if (dashController == null ||
                opposingPlayer == null ||
                opposingMovement == null ||
                opposingCargoCarrier == null)
            {
                return false;
            }

            if (dashController.IsOnCooldown ||
                !opposingCargoCarrier.IsCarrying)
            {
                return false;
            }

            if (opposingStatus != null &&
                opposingStatus.IsInvulnerable)
            {
                return false;
            }

            Vector2Int botCell =
                movementController.EffectiveCell;

            Vector2Int playerCell =
                opposingMovement.EffectiveCell;

            Vector2Int difference =
                playerCell - botCell;

            if (!TryGetDashDirection(
                    difference,
                    out Vector2Int dashDirection))
            {
                return false;
            }

            return dashController.TryDash(
                dashDirection);
        }

        private static bool TryGetDashDirection(
            Vector2Int difference,
            out Vector2Int direction)
        {
            direction = Vector2Int.zero;

            bool sameColumn =
                difference.x == 0 &&
                difference.y != 0;

            bool sameRow =
                difference.y == 0 &&
                difference.x != 0;

            if (!sameColumn && !sameRow)
            {
                return false;
            }

            int distance =
                Mathf.Abs(difference.x) +
                Mathf.Abs(difference.y);

            if (distance < 1 ||
                distance > 2)
            {
                return false;
            }

            if (sameColumn)
            {
                direction =
                    difference.y > 0
                        ? Vector2Int.up
                        : Vector2Int.down;

                return true;
            }

            direction =
                difference.x > 0
                    ? Vector2Int.right
                    : Vector2Int.left;

            return true;
        }

        private bool ValidateDependencies()
        {
            if (pathfinder == null)
            {
                Debug.LogError(
                    "GridPathfinder was not found.",
                    this);

                return false;
            }

            if (homeDeliveryZone == null)
            {
                Debug.LogError(
                    $"Home DeliveryZone was not found for " +
                    $"{playerIdentity.Side}.",
                    this);

                return false;
            }

            if (dashController == null)
            {
                Debug.LogError(
                    "GridDashController was not found on the bot.",
                    this);

                return false;
            }

            if (opposingPlayer == null ||
                opposingMovement == null ||
                opposingCargoCarrier == null)
            {
                Debug.LogError(
                    "Opposing player dependencies were not found.",
                    this);

                return false;
            }

            return true;
        }

        private void ClearCurrentPath()
        {
            currentPath.Clear();
            pathIndex = 0;
        }

        private void ScheduleNextDecision()
        {
            nextDecisionTime =
                Time.time + decisionDelay;
        }
    }
}