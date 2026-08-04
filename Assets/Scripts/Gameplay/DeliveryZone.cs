using CargoClash.Map;
using UnityEngine;

namespace CargoClash.Gameplay
{
    public enum DeliveryZoneType
    {
        Home,
        Forward
    }

    public sealed class DeliveryZone : MonoBehaviour
    {
        private static readonly Vector2Int[] CardinalDirections =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        [Header("Identity")]
        [SerializeField]
        private PlayerSide owner;

        [SerializeField]
        private DeliveryZoneType zoneType;

        [Header("Grid")]
        [SerializeField]
        private Vector2Int centerCell;

        [SerializeField, Min(0)]
        private int deliveryRange = 1;

        [Header("Gameplay")]
        [SerializeField, Min(0f)]
        private float scoreMultiplier = 1f;

        [SerializeField]
        private bool providesCoolingBonus;

        [Header("Validation")]
        [SerializeField]
        private GridMapGenerator mapGenerator;

        public PlayerSide Owner => owner;

        public DeliveryZoneType ZoneType => zoneType;

        public Vector2Int CenterCell => centerCell;

        public int DeliveryRange => deliveryRange;

        public float ScoreMultiplier => scoreMultiplier;

        public bool ProvidesCoolingBonus =>
            providesCoolingBonus;

        public bool IsInDeliveryRange(Vector2Int cell)
        {
            int manhattanDistance =
                Mathf.Abs(cell.x - centerCell.x) +
                Mathf.Abs(cell.y - centerCell.y);

            return manhattanDistance <= deliveryRange;
        }

        public int CountWalkableApproachCells()
        {
            if (mapGenerator == null)
            {
                return 0;
            }

            int walkableCount = 0;

            foreach (Vector2Int direction in CardinalDirections)
            {
                Vector2Int approachCell =
                    centerCell + direction;

                if (mapGenerator.IsWalkable(approachCell))
                {
                    walkableCount++;
                }
            }

            return walkableCount;
        }

        public bool IsCenterWalkable()
        {
            return mapGenerator != null &&
                   mapGenerator.IsWalkable(centerCell);
        }

        [ContextMenu("Validate Delivery Zone")]
        public void ValidateDeliveryZone()
        {
            if (mapGenerator == null)
            {
                Debug.LogError(
                    $"{name}: GridMapGenerator is not assigned.",
                    this);

                return;
            }

            if (!IsCenterWalkable())
            {
                Debug.LogError(
                    $"{name}: Center cell {centerCell} " +
                    "is outside the grid or blocked.",
                    this);

                return;
            }

            int approachCount =
                CountWalkableApproachCells();

            if (approachCount < 3)
            {
                Debug.LogError(
                    $"{name}: Only {approachCount} walkable " +
                    "approach cells were found. Minimum is 3.",
                    this);

                return;
            }

            if (approachCount == 3)
            {
                Debug.LogWarning(
                    $"{name}: Delivery zone is valid, but only " +
                    "3 approach cells are walkable.",
                    this);

                return;
            }

            Debug.Log(
                $"{name}: Delivery zone is valid. " +
                "All 4 approach cells are walkable.",
                this);
        }

        private void OnValidate()
        {
            deliveryRange = Mathf.Max(0, deliveryRange);
            scoreMultiplier = Mathf.Max(0f, scoreMultiplier);

            if (zoneType == DeliveryZoneType.Home)
            {
                scoreMultiplier = 1f;
                providesCoolingBonus = true;
            }
            else
            {
                scoreMultiplier = 1f;
                providesCoolingBonus = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = new(
                centerCell.x,
                0.08f,
                centerCell.y);

            Gizmos.DrawWireCube(
                center,
                new Vector3(0.9f, 0.1f, 0.9f));

            foreach (Vector2Int direction in CardinalDirections)
            {
                if (deliveryRange < 1)
                {
                    break;
                }

                Vector2Int cell =
                    centerCell + direction;

                Vector3 approachPosition = new(
                    cell.x,
                    0.08f,
                    cell.y);

                Gizmos.DrawWireCube(
                    approachPosition,
                    new Vector3(0.75f, 0.08f, 0.75f));
            }
        }
    }
}