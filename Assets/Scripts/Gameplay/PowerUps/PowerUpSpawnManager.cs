using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CargoClash.Gameplay.PowerUps
{
    public sealed class PowerUpSpawnManager : MonoBehaviour
    {
        [Header("Slots")]
        [SerializeField]
        private PowerUpSpawnSlot[] spawnSlots;

        [Header("Power-Ups")]
        [SerializeField]
        private PowerUpBase[] powerUpPrefabs;

        [Header("Limits")]
        [SerializeField, Min(1)]
        private int maximumActivePowerUps = 2;

        [Header("Timing")]
        [SerializeField, Min(0f)]
        private float initialSpawnDelay = 1f;

        [SerializeField, Min(0.1f)]
        private float activeLifetime = 10f;

        [SerializeField, Min(0f)]
        private float respawnDelay = 6f;

        private readonly List<PowerUpBase>
            activePowerUps = new();

        private readonly HashSet<int>
            occupiedSlotIndices = new();

        private int lastPowerUpPrefabIndex = -1;

        private void Start()
        {
            if (!ValidateSetup())
            {
                enabled = false;
                return;
            }

            StartCoroutine(
                InitialSpawnRoutine());
        }

        private IEnumerator InitialSpawnRoutine()
        {
            if (initialSpawnDelay > 0f)
            {
                yield return new WaitForSeconds(
                    initialSpawnDelay);
            }

            int initialCount =
                Mathf.Min(
                    maximumActivePowerUps,
                    spawnSlots.Length);

            for (int i = 0;
                 i < initialCount;
                 i++)
            {
                SpawnPowerUp();
            }
        }

        private void SpawnPowerUp()
        {
            CleanupActivePowerUps();

            if (activePowerUps.Count >=
                maximumActivePowerUps)
            {
                return;
            }

            int slotIndex =
                SelectAvailableSlotIndex();

            if (slotIndex < 0)
            {
                return;
            }

            int prefabIndex =
                SelectPowerUpPrefabIndex();

            if (prefabIndex < 0)
            {
                return;
            }

            PowerUpSpawnSlot slot =
                spawnSlots[slotIndex];

            PowerUpBase prefab =
                powerUpPrefabs[prefabIndex];

            PowerUpBase instance =
                Instantiate(
                    prefab,
                    slot.SpawnPosition,
                    Quaternion.identity);

            activePowerUps.Add(instance);

            occupiedSlotIndices.Add(
                slotIndex);

            lastPowerUpPrefabIndex =
                prefabIndex;

            instance.Initialize(
                this,
                slotIndex,
                activeLifetime);

            Debug.Log(
                $"Spawned {prefab.name} " +
                $"at slot {slotIndex}.",
                this);
        }

        public void NotifyPowerUpRemoved(
            PowerUpBase powerUp,
            int slotIndex)
        {
            if (powerUp != null)
            {
                activePowerUps.Remove(
                    powerUp);
            }

            occupiedSlotIndices.Remove(
                slotIndex);

            StartCoroutine(
                RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            if (respawnDelay > 0f)
            {
                yield return new WaitForSeconds(
                    respawnDelay);
            }

            SpawnPowerUp();
        }

        private int SelectAvailableSlotIndex()
        {
            List<int> availableSlots =
                new();

            for (int i = 0;
                 i < spawnSlots.Length;
                 i++)
            {
                if (!occupiedSlotIndices.Contains(i))
                {
                    availableSlots.Add(i);
                }
            }

            if (availableSlots.Count == 0)
            {
                return -1;
            }

            int randomIndex =
                Random.Range(
                    0,
                    availableSlots.Count);

            return availableSlots[randomIndex];
        }

        private int SelectPowerUpPrefabIndex()
        {
            if (powerUpPrefabs == null ||
                powerUpPrefabs.Length == 0)
            {
                return -1;
            }

            if (powerUpPrefabs.Length == 1)
            {
                return 0;
            }

            int selectedIndex;

            do
            {
                selectedIndex =
                    Random.Range(
                        0,
                        powerUpPrefabs.Length);
            }
            while (selectedIndex ==
                   lastPowerUpPrefabIndex);

            return selectedIndex;
        }

        private void CleanupActivePowerUps()
        {
            activePowerUps.RemoveAll(
                powerUp => powerUp == null);
        }

        private bool ValidateSetup()
        {
            if (spawnSlots == null ||
                spawnSlots.Length == 0)
            {
                Debug.LogError(
                    "No power-up spawn slots are assigned.",
                    this);

                return false;
            }

            foreach (PowerUpSpawnSlot slot
                     in spawnSlots)
            {
                if (slot != null)
                {
                    continue;
                }

                Debug.LogError(
                    "A power-up spawn slot is missing.",
                    this);

                return false;
            }

            if (powerUpPrefabs == null ||
                powerUpPrefabs.Length == 0)
            {
                Debug.LogError(
                    "No power-up prefabs are assigned.",
                    this);

                return false;
            }

            foreach (PowerUpBase prefab
                     in powerUpPrefabs)
            {
                if (prefab != null)
                {
                    continue;
                }

                Debug.LogError(
                    "A power-up prefab is missing.",
                    this);

                return false;
            }

            maximumActivePowerUps =
                Mathf.Clamp(
                    maximumActivePowerUps,
                    1,
                    spawnSlots.Length);

            return true;
        }
    }
}