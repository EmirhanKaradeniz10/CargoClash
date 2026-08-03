using UnityEngine;

namespace CargoClash.Gameplay.Cargo
{
    public sealed class CargoSpawnSlot : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField]
        private Vector2Int cell;

        [SerializeField, Min(0f)]
        private float cargoHeight = 0.4f;

        private CargoItem currentCargo;
        private CargoSpawnManager spawnManager;

        public Vector2Int Cell => cell;

        public CargoItem CurrentCargo => currentCargo;

        public bool IsOccupied => currentCargo != null;

        public CargoItem SpawnCargo(
            CargoItem cargoPrefab,
            CargoSpawnManager manager)
        {
            if (IsOccupied || cargoPrefab == null || manager == null)
            {
                return null;
            }

            spawnManager = manager;

            Vector3 spawnPosition = new(
                cell.x,
                cargoHeight,
                cell.y);

            currentCargo = Instantiate(
                cargoPrefab,
                spawnPosition,
                Quaternion.identity,
                transform);

            currentCargo.name =
                $"{cargoPrefab.name}_{cell.x}_{cell.y}";

            currentCargo.Initialize(this, manager);

            return currentCargo;
        }

        public void NotifyCargoRemoved(CargoItem cargo)
        {
            if (currentCargo != cargo)
            {
                return;
            }

            currentCargo = null;
            spawnManager?.NotifySlotEmptied(this);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = new(
                cell.x,
                0.05f,
                cell.y);

            Gizmos.DrawWireCube(
                center,
                new Vector3(1f, 0.1f, 1f));
        }
    }
}