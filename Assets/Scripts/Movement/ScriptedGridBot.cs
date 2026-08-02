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
            OwnBase
        }

        [Header("Decision Timing")]
        [SerializeField, Min(0.05f)]
        private float decisionDelay = 0.15f;

        [Header("Dependencies")]
        [SerializeField]
        private GridPathfinder pathfinder;

        [SerializeField]
        private BaseZone ownBase;

        [Header("Debug")]
        [SerializeField]
        private GameObject targetMarkerPrefab;

        [SerializeField, Min(0f)]
        private float targetMarkerHeight = 0.05f;

        private GridMovementController movementController;
        private CargoCarrier cargoCarrier;
        private PlayerIdentity playerIdentity;

        private CargoSpawnSlot targetCargoSlot;

        private List<Vector2Int> currentPath = new();
        private int pathIndex;

        private BotGoal currentGoal = BotGoal.None;
        private float nextDecisionTime;

        private GameObject targetMarkerInstance;

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

            if (ownBase == null)
            {
                FindOwnBase();
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
                    ? BotGoal.OwnBase
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

                case BotGoal.OwnBase:
                    SelectOwnBaseTarget();
                    break;

                default:
                    HideTargetMarker();
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
                HideTargetMarker();
                return;
            }

            currentGoal = BotGoal.Cargo;
            targetCargoSlot = bestSlot;
            currentPath = bestPath;
            pathIndex = 0;

            UpdateTargetMarker(bestSlot.Cell);
        }

        private void SelectOwnBaseTarget()
        {
            if (ownBase == null)
            {
                currentGoal = BotGoal.None;
                HideTargetMarker();
                return;
            }

            Vector2Int baseCell =
                ownBase.CenterCell;

            if (ownBase.Contains(
                    movementController.CurrentCell))
            {
                currentGoal = BotGoal.OwnBase;
                targetCargoSlot = null;
                HideTargetMarker();
                return;
            }

            List<Vector2Int> path =
                pathfinder.FindPath(
                    movementController.CurrentCell,
                    baseCell,
                    movementController);

            if (path.Count == 0)
            {
                currentGoal = BotGoal.None;
                HideTargetMarker();
                return;
            }

            currentGoal = BotGoal.OwnBase;
            targetCargoSlot = null;
            currentPath = path;
            pathIndex = 0;

            UpdateTargetMarker(baseCell);
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

                case BotGoal.OwnBase:
                    return cargoCarrier.IsCarrying &&
                           ownBase != null;

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

            // Oyuncu veya başka bir durum yolu kapattı.
            // Aynı hedef için bir sonraki kararda rota yeniden hesaplanır.
            currentPath.Clear();
            pathIndex = 0;
        }

        private void FindOwnBase()
        {
            BaseZone[] baseZones =
                FindObjectsByType<BaseZone>();

            foreach (BaseZone baseZone in baseZones)
            {
                if (baseZone.Owner == playerIdentity.Side)
                {
                    ownBase = baseZone;
                    return;
                }
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

            if (ownBase == null)
            {
                Debug.LogError(
                    $"BaseZone was not found for " +
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

        private void UpdateTargetMarker(
            Vector2Int targetCell)
        {
            if (targetMarkerPrefab == null)
            {
                return;
            }

            if (targetMarkerInstance == null)
            {
                targetMarkerInstance =
                    Instantiate(targetMarkerPrefab);

                targetMarkerInstance.name =
                    $"{name}_TargetMarker";
            }

            targetMarkerInstance.transform.position =
                new Vector3(
                    targetCell.x,
                    targetMarkerHeight,
                    targetCell.y);

            targetMarkerInstance.SetActive(true);
        }

        private void HideTargetMarker()
        {
            if (targetMarkerInstance != null)
            {
                targetMarkerInstance.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (targetMarkerInstance != null)
            {
                Destroy(targetMarkerInstance);
            }
        }
    }
}