using System.Collections;
using UnityEngine;

namespace CargoClash.Movement
{
    public sealed class GridMovementController : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField, Min(0.1f)]
        private float cellSize = 1f;

        [SerializeField]
        private Vector2Int minimumCell = Vector2Int.zero;

        [SerializeField]
        private Vector2Int maximumCell = new(19, 19);

        [Header("Movement")]
        [SerializeField, Min(0.1f)]
        private float movementSpeed = 5f;

        private float movementSpeedMultiplier = 1f;

        [SerializeField]
        private GridOccupancyManager occupancyManager;

        [Header("Collision")]
        [SerializeField]
        private LayerMask obstacleLayer;

        [SerializeField]
        private Vector3 collisionBoxHalfExtents =
            new(0.35f, 0.4f, 0.35f);

        private bool isMoving;
        private bool isDashing;


        private bool isMovementLocked;

        private Vector2Int movementTargetCell;

        private Vector2Int bufferedDirection;
        private Vector2Int facingDirection = Vector2Int.up;

        public Vector2Int CurrentCell { get; private set; }

        public Vector2Int FacingDirection =>
            facingDirection;

        public bool IsMoving => isMoving;

        public bool IsDashing => isDashing;

        public Vector2Int StartingCell =>
            WorldToGrid(transform.position);

        public Vector2Int MinimumCell =>
            minimumCell;

        public Vector2Int MaximumCell =>
            maximumCell;

        public bool IsMovementLocked =>
            isMovementLocked;

        public Vector2Int EffectiveCell =>
            isMoving
                ? movementTargetCell
                : CurrentCell;

        public float MovementSpeedMultiplier =>
            movementSpeedMultiplier;

        private void Awake()
        {
            CurrentCell =
                WorldToGrid(transform.position);

            movementTargetCell = CurrentCell;

            facingDirection =
                GetCardinalDirection(transform.forward);

            SnapToCurrentCell();

            if (occupancyManager == null)
            {
                occupancyManager =
                    FindAnyObjectByType<GridOccupancyManager>();
            }

            if (occupancyManager == null)
            {
                Debug.LogError(
                    "GridOccupancyManager was not found.",
                    this);

                enabled = false;
                return;
            }

            if (!occupancyManager.TryRegister(
                    this,
                    CurrentCell))
            {
                Debug.LogError(
                    $"Starting cell {CurrentCell} " +
                    "is already occupied.",
                    this);

                enabled = false;
            }
        }

        private void Update()
        {
            if (!isMovementLocked &&
                !isMoving &&
                bufferedDirection != Vector2Int.zero)
            {
                Vector2Int direction =
                    bufferedDirection;

                bufferedDirection =
                    Vector2Int.zero;

                TryMove(direction);
            }
        }

        public void ClearBufferedMovement()
        {
            bufferedDirection = Vector2Int.zero;
        }

        public bool TryMove(Vector2Int direction)
        {

            if (!IsCardinalDirection(direction))
            {
                Debug.LogWarning(
                    $"Invalid movement direction: {direction}",
                    this);

                return false;
            }

            if (isMovementLocked || isDashing)
            {
                return false;
            }

            if (isMoving)
            {
                bufferedDirection = direction;
                return false;
            }

            facingDirection = direction;

            Vector2Int targetCell =
                CurrentCell + direction;

            if (!IsInsideGrid(targetCell))
            {
                return false;
            }

            if (occupancyManager.IsOccupied(
                    targetCell,
                    this))
            {
                return false;
            }

            Vector3 targetPosition =
                GridToWorld(targetCell);

            if (IsCellBlocked(targetPosition))
            {
                return false;
            }

            movementTargetCell = targetCell;

            StartCoroutine(
                MoveToCellRoutine(
                    targetCell,
                    targetPosition));

            return true;
        }

        public bool TryDash(
            Vector2Int direction,
            int maximumDistance,
            float dashSpeedMultiplier,
            float recoveryDuration,
            out GridMovementController hitTarget)
        {

            hitTarget = null;

            if (isMovementLocked ||
                        isMoving ||
                        maximumDistance <= 0 ||
                        !IsCardinalDirection(direction))
            {
                return false;
            }

            bufferedDirection = Vector2Int.zero;
            facingDirection = direction;

            Vector2Int destinationCell =
                CurrentCell;

            for (int step = 1;
                 step <= maximumDistance;
                 step++)
            {
                Vector2Int candidateCell =
                    CurrentCell +
                    direction * step;

                if (!IsInsideGrid(candidateCell))
                {
                    break;
                }

                if (occupancyManager.TryGetOccupant(
                        candidateCell,
                        this,
                        out GridMovementController occupant))
                {
                    hitTarget = occupant;
                    break;
                }

                Vector3 candidatePosition =
                    GridToWorld(candidateCell);

                if (IsCellBlocked(candidatePosition))
                {
                    break;
                }

                destinationCell = candidateCell;
            }

            if (destinationCell == CurrentCell)
            {
                StartCoroutine(
                    DashRecoveryRoutine(
                        recoveryDuration));

                return true;
            }

            movementTargetCell = destinationCell;

            StartCoroutine(
                DashToCellRoutine(
                    destinationCell,
                    dashSpeedMultiplier,
                    recoveryDuration));

            return true;
        }

        private IEnumerator MoveToCellRoutine(
            Vector2Int targetCell,
            Vector3 targetPosition)
        {
            isMoving = true;

            Vector2Int previousCell =
                CurrentCell;

            if (!occupancyManager.TryRegister(
                    this,
                    targetCell))
            {
                movementTargetCell = CurrentCell;
                isMoving = false;
                yield break;
            }

            Vector3 startPosition =
                transform.position;

            float distance =
                Vector3.Distance(
                    startPosition,
                    targetPosition);

            float effectiveSpeed =
                    movementSpeed *
                    movementSpeedMultiplier;

            float duration =
                distance /
                Mathf.Max(0.01f, effectiveSpeed);

            float elapsedTime = 0f;

            FaceDirection(
                targetPosition - startPosition);

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsedTime / duration);

                transform.position =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        progress);

                yield return null;
            }

            transform.position = targetPosition;
            CurrentCell = targetCell;
            movementTargetCell = targetCell;

            occupancyManager.Release(
                this,
                previousCell);

            isMoving = false;
        }

        private IEnumerator DashToCellRoutine(
            Vector2Int targetCell,
            float speedMultiplier,
            float recoveryDuration)
        {
            isMoving = true;
            isDashing = true;

            Vector2Int previousCell =
                CurrentCell;

            if (!occupancyManager.TryRegister(
                    this,
                    targetCell))
            {
                movementTargetCell = CurrentCell;

                yield return new WaitForSeconds(
                    recoveryDuration);

                isMoving = false;
                isDashing = false;
                yield break;
            }

            Vector3 startPosition =
                transform.position;

            Vector3 targetPosition =
                GridToWorld(targetCell);

            float distance =
                Vector3.Distance(
                    startPosition,
                    targetPosition);

            float effectiveSpeed =
                movementSpeed *
                Mathf.Max(1f, speedMultiplier);

            float duration =
                distance / effectiveSpeed;

            float elapsedTime = 0f;

            FaceDirection(
                targetPosition - startPosition);

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsedTime / duration);

                transform.position =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        progress);

                yield return null;
            }

            transform.position = targetPosition;
            CurrentCell = targetCell;
            movementTargetCell = targetCell;

            occupancyManager.Release(
                this,
                previousCell);

            if (recoveryDuration > 0f)
            {
                yield return new WaitForSeconds(
                    recoveryDuration);
            }

            isMoving = false;
            isDashing = false;
        }

        public void SetMovementLocked(bool locked)
        {
            isMovementLocked = locked;

            if (locked)
            {
                bufferedDirection = Vector2Int.zero;
            }
        }

        public void SetMovementSpeedMultiplier(float multiplier)
        {
            movementSpeedMultiplier =
                Mathf.Max(0.1f, multiplier);
        }

        private IEnumerator DashRecoveryRoutine(
            float recoveryDuration)
        {
            isMoving = true;
            isDashing = true;

            if (recoveryDuration > 0f)
            {
                yield return new WaitForSeconds(
                    recoveryDuration);
            }

            isMoving = false;
            isDashing = false;
        }

        private bool IsCellBlocked(
            Vector3 targetPosition)
        {
            return Physics.CheckBox(
                targetPosition,
                collisionBoxHalfExtents,
                Quaternion.identity,
                obstacleLayer,
                QueryTriggerInteraction.Ignore);
        }

        private bool IsInsideGrid(Vector2Int cell)
        {
            return cell.x >= minimumCell.x &&
                   cell.y >= minimumCell.y &&
                   cell.x <= maximumCell.x &&
                   cell.y <= maximumCell.y;
        }

        private static bool IsCardinalDirection(
            Vector2Int direction)
        {
            return direction == Vector2Int.up ||
                   direction == Vector2Int.down ||
                   direction == Vector2Int.left ||
                   direction == Vector2Int.right;
        }

        private void FaceDirection(
            Vector3 worldDirection)
        {
            worldDirection.y = 0f;

            if (worldDirection.sqrMagnitude <= 0.001f)
            {
                return;
            }

            facingDirection =
                GetCardinalDirection(worldDirection);

            transform.rotation =
                Quaternion.LookRotation(worldDirection);
        }

        private static Vector2Int GetCardinalDirection(
            Vector3 worldDirection)
        {
            if (Mathf.Abs(worldDirection.x) >
                Mathf.Abs(worldDirection.z))
            {
                return worldDirection.x >= 0f
                    ? Vector2Int.right
                    : Vector2Int.left;
            }

            return worldDirection.z >= 0f
                ? Vector2Int.up
                : Vector2Int.down;
        }

        private Vector2Int WorldToGrid(
            Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.RoundToInt(
                    worldPosition.x / cellSize),
                Mathf.RoundToInt(
                    worldPosition.z / cellSize));
        }

        private Vector3 GridToWorld(
            Vector2Int cell)
        {
            return new Vector3(
                cell.x * cellSize,
                transform.position.y,
                cell.y * cellSize);
        }

        private void SnapToCurrentCell()
        {
            transform.position =
                GridToWorld(CurrentCell);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireCube(
                transform.position,
                collisionBoxHalfExtents * 2f);
        }
    }
}