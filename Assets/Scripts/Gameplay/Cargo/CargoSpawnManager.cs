using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CargoClash.Gameplay.Cargo
{
    public sealed class CargoSpawnManager : MonoBehaviour
    {
        [Header("Slots")]
        [SerializeField]
        private CargoSpawnSlot[] spawnSlots;

        [Header("Cargo Prefabs")]
        [SerializeField]
        private CargoItem normalCargoPrefab;

        [SerializeField]
        private CargoItem heavyCargoPrefab;

        [SerializeField]
        private CargoItem rareCargoPrefab;

        [Header("Cargo Probabilities")]
        [SerializeField, Range(0f, 1f)]
        private float normalProbability = 0.6f;

        [SerializeField, Range(0f, 1f)]
        private float heavyProbability = 0.3f;

        [Header("Spawn Limits")]
        [SerializeField, Min(1)]
        private int targetActiveSlotCount = 3;

        [SerializeField, Min(1)]
        private int maximumUndeliveredCargo = 6;

        [Header("Spawn Timing")]
        [SerializeField, Min(0f)]
        private float minimumSpawnDelay = 3f;

        [SerializeField, Min(0f)]
        private float maximumSpawnDelay = 5f;

        private readonly HashSet<CargoItem> undeliveredCargo = new();

        private CargoSpawnSlot lastSelectedSlot;
        private Coroutine refillCoroutine;

        public int ActiveSlotCount => CountActiveSlots();

        public int UndeliveredCargoCount => undeliveredCargo.Count;

        private void Start()
        {
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            FillInitialSlots();
        }

        public void NotifySlotEmptied(CargoSpawnSlot emptiedSlot)
        {
            ScheduleRefill();
        }

        public void UnregisterCargo(CargoItem cargo)
        {
            if (cargo != null)
            {
                undeliveredCargo.Remove(cargo);
            }

            ScheduleRefill();
        }

        private void FillInitialSlots()
        {
            while (CanSpawnMore())
            {
                if (!TrySpawnOneCargo())
                {
                    break;
                }
            }
        }

        private void ScheduleRefill()
        {
            if (refillCoroutine != null)
            {
                return;
            }

            refillCoroutine =
                StartCoroutine(RefillRoutine());
        }

        private IEnumerator RefillRoutine()
        {
            float delay = Random.Range(
                minimumSpawnDelay,
                maximumSpawnDelay);

            yield return new WaitForSeconds(delay);

            refillCoroutine = null;

            if (CanSpawnMore())
            {
                TrySpawnOneCargo();
            }

            if (CanSpawnMore())
            {
                ScheduleRefill();
            }
        }

        private bool CanSpawnMore()
        {
            return CountActiveSlots() < targetActiveSlotCount &&
                   undeliveredCargo.Count < maximumUndeliveredCargo;
        }

        private bool TrySpawnOneCargo()
        {
            List<CargoSpawnSlot> candidates =
                GetAvailableSlots();

            if (candidates.Count == 0)
            {
                return false;
            }

            CargoSpawnSlot selectedSlot =
                candidates[Random.Range(0, candidates.Count)];

            CargoItem selectedPrefab =
                SelectCargoPrefab();

            CargoItem spawnedCargo =
                selectedSlot.SpawnCargo(
                    selectedPrefab,
                    this);

            if (spawnedCargo == null)
            {
                return false;
            }

            undeliveredCargo.Add(spawnedCargo);
            lastSelectedSlot = selectedSlot;

            return true;
        }

        private List<CargoSpawnSlot> GetAvailableSlots()
        {
            List<CargoSpawnSlot> availableSlots = new();

            foreach (CargoSpawnSlot slot in spawnSlots)
            {
                if (slot == null || slot.IsOccupied)
                {
                    continue;
                }

                availableSlots.Add(slot);
            }

            // Son seçilen slot yalnızca bir sonraki
            // seçimden çıkarılır. Başka seçenek yoksa
            // aynı slotun kullanılmasına izin verilir.
            if (availableSlots.Count > 1 &&
                lastSelectedSlot != null)
            {
                availableSlots.Remove(lastSelectedSlot);
            }

            return availableSlots;
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

        private int CountActiveSlots()
        {
            int count = 0;

            foreach (CargoSpawnSlot slot in spawnSlots)
            {
                if (slot != null && slot.IsOccupied)
                {
                    count++;
                }
            }

            return count;
        }

        private bool ValidateConfiguration()
        {
            if (spawnSlots == null || spawnSlots.Length == 0)
            {
                Debug.LogError(
                    "No cargo spawn slots are assigned.",
                    this);

                return false;
            }

            if (normalCargoPrefab == null ||
                heavyCargoPrefab == null ||
                rareCargoPrefab == null)
            {
                Debug.LogError(
                    "One or more cargo prefabs are missing.",
                    this);

                return false;
            }

            return true;
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

            if (maximumSpawnDelay < minimumSpawnDelay)
            {
                maximumSpawnDelay = minimumSpawnDelay;
            }

            if (maximumUndeliveredCargo <
                targetActiveSlotCount)
            {
                maximumUndeliveredCargo =
                    targetActiveSlotCount;
            }
        }
    }
}