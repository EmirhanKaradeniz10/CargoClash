using System.Collections.Generic;
using UnityEngine;

namespace CargoClash.Movement
{
    [RequireComponent(typeof(GridMovementController))]
    public sealed class ScriptedGridBot : MonoBehaviour
    {
        [Header("Decision Timing")]
        [SerializeField, Min(0.05f)]
        private float decisionDelay = 0.15f;

        [Header("Debug")]
        [SerializeField]
        private GameObject targetMarkerPrefab;

        [SerializeField, Min(0f)]
        private float targetMarkerHeight = 0.05f;

        private GameObject targetMarkerInstance;

        [Header("Target Selection")]
        [SerializeField, Min(1)]
        private int targetSelectionAttempts = 30;

        private GridMovementController movementController;
        private GridPathfinder pathfinder;

        private List<Vector2Int> currentPath = new();
        private int pathIndex;
        private float nextDecisionTime;

        private void Awake()
        {
            movementController =
                GetComponent<GridMovementController>();

            pathfinder =
                FindAnyObjectByType<GridPathfinder>();
        }

        private void Start()
        {
            if (pathfinder == null)
            {
                Debug.LogError(
                    "GridPathfinder was not found.",
                    this);

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

            if (pathIndex >= currentPath.Count)
            {
                HideTargetMarker();
                SelectNewTarget();
            }

            FollowCurrentPath();
            ScheduleNextDecision();
        }

        private void SelectNewTarget()
        {
            currentPath.Clear();
            pathIndex = 0;

            for (int attempt = 0;
                 attempt < targetSelectionAttempts;
                 attempt++)
            {
                Vector2Int targetCell = new(
                    Random.Range(0, 20),
                    Random.Range(0, 20));

                if (!pathfinder.IsWalkable(
                        targetCell,
                        movementController))
                {
                    continue;
                }

                List<Vector2Int> path =
                    pathfinder.FindPath(
                        movementController.CurrentCell,
                        targetCell,
                        movementController);

                if (path.Count == 0)
                {
                    continue;
                }

                currentPath = path;
                UpdateTargetMarker(targetCell);
                return;
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

            // Yol, diğer oyuncunun hareketi nedeniyle
            // geçersiz hâle gelmiş olabilir.
            currentPath.Clear();
            pathIndex = 0;
        }

        private void UpdateTargetMarker(Vector2Int targetCell)
        {
            if (targetMarkerPrefab == null)
            {
                return;
            }

            if (targetMarkerInstance == null)
            {
                targetMarkerInstance = Instantiate(targetMarkerPrefab);
                targetMarkerInstance.name = $"{name}_TargetMarker";
            }

            targetMarkerInstance.transform.position = new Vector3(
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

        private void ScheduleNextDecision()
        {
            nextDecisionTime =
                Time.time + decisionDelay;
        }
    }
}