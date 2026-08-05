using System.Collections.Generic;
using CargoClash.Map;
using UnityEngine;

namespace CargoClash.Movement
{
    public sealed class GridPathfinder : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField]
        private Vector2Int minimumCell = Vector2Int.zero;

        [SerializeField]
        private Vector2Int maximumCell = new(19, 19);

        [Header("Dependencies")]
        [SerializeField]
        private GridMapGenerator mapGenerator;

        [SerializeField]
        private GridOccupancyManager occupancyManager;

        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private void Awake()
        {
            ResolveMapGenerator();

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

        public List<Vector2Int> FindPath(
            Vector2Int start,
            Vector2Int goal,
            GridMovementController requester)
        {
            List<Vector2Int> emptyPath = new();

            if (mapGenerator == null ||
                occupancyManager == null ||
                start == goal ||
                !IsWalkable(goal, requester))
            {
                return emptyPath;
            }

            List<Vector2Int> openSet = new()
            {
                start
            };

            HashSet<Vector2Int> closedSet = new();

            Dictionary<Vector2Int, Vector2Int> cameFrom = new();
            Dictionary<Vector2Int, int> gScore = new()
            {
                [start] = 0
            };

            Dictionary<Vector2Int, int> fScore = new()
            {
                [start] = ManhattanDistance(start, goal)
            };

            while (openSet.Count > 0)
            {
                Vector2Int current =
                    GetLowestScoreCell(openSet, fScore);

                if (current == goal)
                {
                    return ReconstructPath(
                        cameFrom,
                        current,
                        start);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int neighbour = current + direction;

                    if (closedSet.Contains(neighbour) ||
                        !IsWalkable(neighbour, requester))
                    {
                        continue;
                    }

                    int tentativeGScore = gScore[current] + 1;

                    if (gScore.TryGetValue(
                            neighbour,
                            out int existingGScore) &&
                        tentativeGScore >= existingGScore)
                    {
                        continue;
                    }

                    cameFrom[neighbour] = current;
                    gScore[neighbour] = tentativeGScore;
                    fScore[neighbour] =
                        tentativeGScore +
                        ManhattanDistance(neighbour, goal);

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                }
            }

            return emptyPath;
        }

        public List<Vector2Int> FindStaticPath(
    Vector2Int start,
    Vector2Int goal)
        {
            List<Vector2Int> emptyPath = new();

            ResolveMapGenerator();

            if (mapGenerator == null ||
                !mapGenerator.IsWalkable(start) ||
                !mapGenerator.IsWalkable(goal))
            {
                return emptyPath;
            }

            if (start == goal)
            {
                return emptyPath;
            }

            List<Vector2Int> openSet = new()
    {
        start
    };

            HashSet<Vector2Int> closedSet = new();

            Dictionary<Vector2Int, Vector2Int> cameFrom = new();

            Dictionary<Vector2Int, int> gScore = new()
            {
                [start] = 0
            };

            Dictionary<Vector2Int, int> fScore = new()
            {
                [start] = ManhattanDistance(start, goal)
            };

            while (openSet.Count > 0)
            {
                Vector2Int current =
                    GetLowestScoreCell(openSet, fScore);

                if (current == goal)
                {
                    return ReconstructPath(
                        cameFrom,
                        current,
                        start);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int neighbour =
                        current + direction;

                    if (closedSet.Contains(neighbour) ||
                        !mapGenerator.IsWalkable(neighbour))
                    {
                        continue;
                    }

                    int tentativeGScore =
                        gScore[current] + 1;

                    if (gScore.TryGetValue(
                            neighbour,
                            out int existingGScore) &&
                        tentativeGScore >= existingGScore)
                    {
                        continue;
                    }

                    cameFrom[neighbour] = current;
                    gScore[neighbour] = tentativeGScore;
                    fScore[neighbour] =
                        tentativeGScore +
                        ManhattanDistance(neighbour, goal);

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                }
            }

            return emptyPath;
        }

        private void ResolveMapGenerator()
        {
            if (mapGenerator == null)
            {
                mapGenerator =
                    FindAnyObjectByType<GridMapGenerator>();
            }
        }

        public bool IsWalkable(
            Vector2Int cell,
            GridMovementController requester)
        {
            if (!IsInsideGrid(cell))
            {
                return false;
            }

            if (mapGenerator.IsBlocked(cell))
            {
                return false;
            }

            return !occupancyManager.IsOccupied(
                cell,
                requester);
        }

        private bool IsInsideGrid(Vector2Int cell)
        {
            return cell.x >= minimumCell.x &&
                   cell.y >= minimumCell.y &&
                   cell.x <= maximumCell.x &&
                   cell.y <= maximumCell.y;
        }

        private static Vector2Int GetLowestScoreCell(
            List<Vector2Int> openSet,
            Dictionary<Vector2Int, int> fScore)
        {
            Vector2Int bestCell = openSet[0];
            int bestScore = GetScore(fScore, bestCell);

            for (int index = 1;
                 index < openSet.Count;
                 index++)
            {
                Vector2Int candidate = openSet[index];
                int candidateScore =
                    GetScore(fScore, candidate);

                if (candidateScore < bestScore)
                {
                    bestCell = candidate;
                    bestScore = candidateScore;
                }
            }

            return bestCell;
        }

        private static int GetScore(
            Dictionary<Vector2Int, int> scores,
            Vector2Int cell)
        {
            return scores.TryGetValue(cell, out int score)
                ? score
                : int.MaxValue;
        }

        private static int ManhattanDistance(
            Vector2Int first,
            Vector2Int second)
        {
            return Mathf.Abs(first.x - second.x) +
                   Mathf.Abs(first.y - second.y);
        }

        private static List<Vector2Int> ReconstructPath(
            Dictionary<Vector2Int, Vector2Int> cameFrom,
            Vector2Int current,
            Vector2Int start)
        {
            List<Vector2Int> path = new()
            {
                current
            };

            while (cameFrom.TryGetValue(
                       current,
                       out Vector2Int previous))
            {
                current = previous;

                if (current != start)
                {
                    path.Add(current);
                }
            }

            path.Reverse();
            return path;
        }
    }
}