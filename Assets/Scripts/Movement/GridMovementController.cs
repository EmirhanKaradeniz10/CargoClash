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

        [Header("Collision")]
        [SerializeField]
        private LayerMask obstacleLayer;

        [SerializeField]
        private Vector3 collisionBoxHalfExtents =
            new(0.35f, 0.4f, 0.35f);

        private bool isMoving;
        private Vector2Int bufferedDirection;

        public Vector2Int CurrentCell { get; private set; }

        public bool IsMoving => isMoving;

        private void Awake()
        {
            CurrentCell = WorldToGrid(transform.position);
            SnapToCurrentCell();
        }

        private void Update()
        {
            if (!isMoving &&
                bufferedDirection != Vector2Int.zero)
            {
                Vector2Int direction = bufferedDirection;
                bufferedDirection = Vector2Int.zero;

                TryMove(direction);
            }
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

            if (isMoving)
            {
                bufferedDirection = direction;
                return false;
            }

            Vector2Int targetCell = CurrentCell + direction;

            if (!IsInsideGrid(targetCell))
            {
                return false;
            }

            Vector3 targetPosition = GridToWorld(targetCell);

            if (IsCellBlocked(targetPosition))
            {
                return false;
            }

            StartCoroutine(
                MoveToCellRoutine(targetCell, targetPosition));

            return true;
        }

        private IEnumerator MoveToCellRoutine(
            Vector2Int targetCell,
            Vector3 targetPosition)
        {
            isMoving = true;

            Vector3 startPosition = transform.position;

            float distance = Vector3.Distance(
                startPosition,
                targetPosition);

            float duration = distance / movementSpeed;
            float elapsedTime = 0f;

            FaceDirection(targetPosition - startPosition);

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float progress = Mathf.Clamp01(
                    elapsedTime / duration);

                transform.position = Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    progress);

                yield return null;
            }

            transform.position = targetPosition;
            CurrentCell = targetCell;
            isMoving = false;
        }

        private bool IsCellBlocked(Vector3 targetPosition)
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

        private void FaceDirection(Vector3 worldDirection)
        {
            worldDirection.y = 0f;

            if (worldDirection.sqrMagnitude <= 0.001f)
            {
                return;
            }

            transform.rotation =
                Quaternion.LookRotation(worldDirection);
        }

        private Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPosition.x / cellSize),
                Mathf.RoundToInt(worldPosition.z / cellSize));
        }

        private Vector3 GridToWorld(Vector2Int cell)
        {
            return new Vector3(
                cell.x * cellSize,
                transform.position.y,
                cell.y * cellSize);
        }

        private void SnapToCurrentCell()
        {
            transform.position = GridToWorld(CurrentCell);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireCube(
                transform.position,
                collisionBoxHalfExtents * 2f);
        }
    }
}