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

        private List<Vector2Int> currentPath = new();
        private int pathIndex;

        private BotGoal currentGoal = BotGoal.None;
        private float nextDecisionTime;

        private void Awake()
        {
            movementController =
                GetComponent<GridMovementController>();

            cargoCarrier =
                GetComponent<CargoCarrier>();

            playerIdentity =
                GetComponent<PlayerIdentity>();

            if (pathfinder == null)
            {
                pathfinder =
                    FindAnyObjectByType<GridPathfinder>();
            }

            if (homeDeliveryZone == null)
            {
                FindHomeDeliveryZone();
            }
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
            if (Time.time < nextDecisionTime ||
                movementController.IsMoving)
            {
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