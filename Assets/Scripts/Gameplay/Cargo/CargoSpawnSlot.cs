using System.Collections;
using UnityEngine;

namespace CargoClash.Gameplay.Cargo
{
    public sealed class CargoSpawnSlot : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField]
        private Vector2Int cell;

        [Header("Spawn")]
        [SerializeField]
        private CargoItem cargoPrefab;

        [SerializeField]
        private bool spawnOnStart = true;

        [SerializeField, Min(0f)]
        private float respawnDelay = 3f;

        [SerializeField, Min(0f)]
        private float cargoHeight = 0.35f;

        private CargoItem currentCargo;
        private Coroutine respawnCoroutine;

        public Vector2Int Cell => cell;

        public CargoItem CurrentCargo => currentCargo;

        public bool IsOccupied => currentCargo != null;

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnCargo();
            }
        }

        [ContextMenu("Spawn Cargo")]
        public void SpawnCargo()
        {
            if (currentCargo != null)
            {
                return;
            }

            if (cargoPrefab == null)
            {
                Debug.LogError(
                    $"Cargo prefab is not assigned on {name}.",
                    this);

                return;
            }

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

            currentCargo.Initialize(this);
        }

        public void NotifyCargoRemoved(CargoItem cargo)
        {
            if (currentCargo != cargo)
            {
                return;
            }

            currentCargo = null;

            if (respawnCoroutine != null)
            {
                StopCoroutine(respawnCoroutine);
            }

            respawnCoroutine =
                StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);

            respawnCoroutine = null;
            SpawnCargo();
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