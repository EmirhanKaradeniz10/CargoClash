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
        private CargoItem normalCargoPrefab;

        [SerializeField]
        private CargoItem heavyCargoPrefab;

        [SerializeField]
        private CargoItem rareCargoPrefab;

        [Header("Spawn Probabilities")]
        [SerializeField, Range(0f, 1f)]
        private float normalProbability = 0.6f;

        [SerializeField, Range(0f, 1f)]
        private float heavyProbability = 0.3f;

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

            if (normalCargoPrefab == null ||
                        heavyCargoPrefab == null ||
                        rareCargoPrefab == null)
            {
                Debug.LogError(
                    $"One or more cargo prefabs are not assigned on {name}.",
                    this);

                return;
            }

            Vector3 spawnPosition = new(
                cell.x,
                cargoHeight,
                cell.y);

            CargoItem selectedPrefab = SelectCargoPrefab();

            currentCargo = Instantiate(
                selectedPrefab,
                spawnPosition,
                Quaternion.identity,
                transform);

            currentCargo.name =
                $"{selectedPrefab.name}_{cell.x}_{cell.y}";

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

        private CargoItem SelectCargoPrefab()
        {
            float randomValue = Random.value;

            if (randomValue < normalProbability)
            {
                return normalCargoPrefab;
            }

            if (randomValue <
                normalProbability + heavyProbability)
            {
                return heavyCargoPrefab;
            }

            return rareCargoPrefab;
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

        private void OnValidate()
        {
            normalProbability =
                Mathf.Clamp01(normalProbability);

            heavyProbability =
                Mathf.Clamp(
                    heavyProbability,
                    0f,
                    1f - normalProbability);
        }
    }
}