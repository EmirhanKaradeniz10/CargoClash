using System.Collections.Generic;
using UnityEngine;

namespace CargoClash.Movement
{
    public sealed class GridOccupancyManager : MonoBehaviour
    {
        private readonly Dictionary<Vector2Int, GridMovementController>
            occupiedCells = new();

        public bool TryRegister(
            GridMovementController controller,
            Vector2Int cell)
        {
            if (controller == null)
            {
                return false;
            }

            if (occupiedCells.TryGetValue(
                    cell,
                    out GridMovementController existingController) &&
                existingController != controller)
            {
                return false;
            }

            occupiedCells[cell] = controller;

            return true;
        }

        public bool IsOccupied(
            Vector2Int cell,
            GridMovementController requester)
        {
            return occupiedCells.TryGetValue(
                       cell,
                       out GridMovementController controller) &&
                   controller != requester;
        }

        public void Release(
            GridMovementController controller,
            Vector2Int cell)
        {
            if (!occupiedCells.TryGetValue(
                    cell,
                    out GridMovementController existingController))
            {
                return;
            }

            if (existingController == controller)
            {
                occupiedCells.Remove(cell);
            }
        }

        private void RemoveController(
            GridMovementController controller)
        {
            Vector2Int? cellToRemove = null;

            foreach (KeyValuePair<
                         Vector2Int,
                         GridMovementController> entry in occupiedCells)
            {
                if (entry.Value == controller)
                {
                    cellToRemove = entry.Key;
                    break;
                }
            }

            if (cellToRemove.HasValue)
            {
                occupiedCells.Remove(cellToRemove.Value);
            }
        }
    }
}