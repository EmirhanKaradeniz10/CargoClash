using UnityEngine;

namespace CargoClash.Movement
{
    [RequireComponent(typeof(GridMovementController))]
    public sealed class ScriptedGridBot : MonoBehaviour
    {
        [Header("Decision Timing")]
        [SerializeField, Min(0.05f)]
        private float minimumDecisionDelay = 0.15f;

        [SerializeField, Min(0.05f)]
        private float maximumDecisionDelay = 0.35f;

        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private GridMovementController movementController;
        private float nextDecisionTime;

        private void Awake()
        {
            movementController =
                GetComponent<GridMovementController>();
        }

        private void Start()
        {
            ScheduleNextDecision();
        }

        private void Update()
        {
            if (Time.time < nextDecisionTime)
            {
                return;
            }

            if (movementController.IsMoving)
            {
                return;
            }

            TryRandomMovement();
            ScheduleNextDecision();
        }

        private void TryRandomMovement()
        {
            int startingIndex = Random.Range(0, Directions.Length);

            for (int offset = 0; offset < Directions.Length; offset++)
            {
                int directionIndex =
                    (startingIndex + offset) % Directions.Length;

                Vector2Int direction = Directions[directionIndex];

                if (movementController.TryMove(direction))
                {
                    return;
                }
            }
        }

        private void ScheduleNextDecision()
        {
            nextDecisionTime = Time.time + Random.Range(
                minimumDecisionDelay,
                maximumDecisionDelay);
        }

        private void OnValidate()
        {
            if (maximumDecisionDelay < minimumDecisionDelay)
            {
                maximumDecisionDelay = minimumDecisionDelay;
            }
        }
    }
}