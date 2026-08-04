using System.Collections.Generic;
using UnityEngine;

namespace CargoClash.Map
{
    public sealed class GridMapGenerator : MonoBehaviour
    {
        private const int GridWidth = 20;
        private const int GridHeight = 20;

        [Header("Grid")]
        [SerializeField, Min(0.1f)]
        private float cellSize = 1f;

        [Header("Visuals")]
        [SerializeField]
        private Transform obstacleParent;

        [SerializeField]
        private GameObject obstaclePrefab;

        private readonly List<GameObject> generatedObstacles = new();

        private static readonly Vector2Int[] BlockedCells =
        {
            // Üst dış sınır
            new(0, 19), new(1, 19), new(2, 19), new(3, 19), new(4, 19),
            new(15, 19), new(16, 19), new(17, 19), new(18, 19), new(19, 19),

            new(0, 18), new(1, 18), new(2, 18),
            new(17, 18), new(18, 18), new(19, 18),

            new(0, 17), new(1, 17),
            new(18, 17), new(19, 17),

            // Alt dış sınır
            new(0, 0), new(1, 0), new(2, 0), new(3, 0), new(4, 0),
            new(15, 0), new(16, 0), new(17, 0), new(18, 0), new(19, 0),

            new(0, 1), new(1, 1), new(2, 1),
            new(17, 1), new(18, 1), new(19, 1),

            new(0, 2), new(1, 2),
            new(18, 2), new(19, 2),

            // Üst orta raflar
            new(8, 16), new(9, 16), new(10, 16), new(11, 16),
            new(7, 15), new(8, 15), new(11, 15), new(12, 15),
            new(7, 14), new(8, 14), new(11, 14), new(12, 14),

            // Merkez kavşak rafları
            new(8, 11), new(11, 11),
            new(8, 9),  new(11, 9),

            // Alt orta raflar
            new(7, 7), new(8, 7), new(11, 7), new(12, 7),
            new(7, 6), new(8, 6), new(11, 6), new(12, 6),
            new(8, 4), new(9, 4), new(10, 4), new(11, 4)
        };

        private static readonly HashSet<Vector2Int> BlockedCellSet =
            new(BlockedCells);

        public int Width => GridWidth;

        public int Height => GridHeight;

        public float CellSize => cellSize;

        private void Start()
        {
            Generate();
        }

        [ContextMenu("Generate Map")]
        public void Generate()
        {
            ClearGenerated();

            if (obstaclePrefab == null)
            {
                Debug.LogError(
                    "Obstacle prefab is not assigned.",
                    this);

                return;
            }

            if (obstacleParent == null)
            {
                obstacleParent = transform;
            }

            foreach (Vector2Int cell in BlockedCells)
            {
                Vector3 position = GridToWorld(cell);

                GameObject obstacle = Instantiate(
                    obstaclePrefab,
                    position,
                    Quaternion.identity,
                    obstacleParent);

                obstacle.name =
                    $"Obstacle_{cell.x}_{cell.y}";

                generatedObstacles.Add(obstacle);
            }
        }

        [ContextMenu("Clear Generated")]
        public void ClearGenerated()
        {
            foreach (GameObject obstacle in generatedObstacles)
            {
                if (obstacle == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(obstacle);
                }
                else
                {
                    DestroyImmediate(obstacle);
                }
            }

            generatedObstacles.Clear();
        }

        public Vector3 GridToWorld(Vector2Int cell)
        {
            return new Vector3(
                cell.x * cellSize,
                0f,
                cell.y * cellSize);
        }

        public bool IsInsideGrid(Vector2Int cell)
        {
            return cell.x >= 0 &&
                   cell.x < GridWidth &&
                   cell.y >= 0 &&
                   cell.y < GridHeight;
        }

        public bool IsBlocked(Vector2Int cell)
        {
            return BlockedCellSet.Contains(cell);
        }

        public bool IsWalkable(Vector2Int cell)
        {
            return IsInsideGrid(cell) &&
                   !IsBlocked(cell);
        }
    }
}