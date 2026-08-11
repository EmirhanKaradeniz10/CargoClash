using System.Collections;
using UnityEngine;

namespace CargoClash.Gameplay.PowerUps
{
    public sealed class PowerUpSpawnManager : MonoBehaviour
    {
        [Header("Slots")]
        [SerializeField]
        private PowerUpSpawnSlot[] spawnSlots;

        [Header("Prefab")]
        [SerializeField]
        private ShieldPowerUp shieldPowerUpPrefab;

        [Header("Timing")]
        [SerializeField, Min(0f)]
        private float initialSpawnDelay = 1f;

        [SerializeField, Min(0.1f)]
        private float activeLifetime = 10f;

        [SerializeField, Min(0f)]
        private float respawnDelay = 6f;

        private ShieldPowerUp activePowerUp;
        private Coroutine spawnRoutine;

        private int lastSpawnSlotIndex = -1;

        private void Start()
        {
            if (!ValidateSetup())
            {
                enabled = false;
                return;
            }

            spawnRoutine =
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

            SpawnPowerUp();
        }

        private void SpawnPowerUp()
        {
            if (activePowerUp != null)
            {
                return;
            }

            int slotIndex =
                SelectSpawnSlotIndex();

            if (slotIndex < 0)
            {
                return;
            }

            PowerUpSpawnSlot slot =
                spawnSlots[slotIndex];

            activePowerUp =
                Instantiate(
                    shieldPowerUpPrefab,
                    slot.SpawnPosition,
                    Quaternion.identity);

            lastSpawnSlotIndex =
                slotIndex;

            activePowerUp.Initialize(
                this,
                activeLifetime);
        }

        public void NotifyPowerUpRemoved(
            ShieldPowerUp powerUp)
        {
            if (powerUp == null ||
                activePowerUp != powerUp)
            {
                return;
            }

            activePowerUp = null;

            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
            }

            spawnRoutine =
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

            spawnRoutine = null;
            SpawnPowerUp();
        }

        private int SelectSpawnSlotIndex()
        {
            if (spawnSlots == null ||
                spawnSlots.Length == 0)
            {
                return -1;
            }

            if (spawnSlots.Length == 1)
            {
                return 0;
            }

            int selectedIndex;

            do
            {
                selectedIndex =
                    Random.Range(
                        0,
                        spawnSlots.Length);
            }
            while (selectedIndex ==
                   lastSpawnSlotIndex);

            return selectedIndex;
        }

        private bool ValidateSetup()
        {
            if (shieldPowerUpPrefab == null)
            {
                Debug.LogError(
                    "Shield power-up prefab is not assigned.",
                    this);

                return false;
            }

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

            return true;
        }
    }
}