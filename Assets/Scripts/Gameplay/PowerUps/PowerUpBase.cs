using System.Collections;
using UnityEngine;

namespace CargoClash.Gameplay.PowerUps
{
    [RequireComponent(typeof(Collider))]
    public abstract class PowerUpBase : MonoBehaviour
    {
        private PowerUpSpawnManager spawnManager;

        private Coroutine lifetimeCoroutine;

        private int spawnSlotIndex = -1;

        private bool isInitialized;
        private bool isRemoving;

        protected virtual void Awake()
        {
            Collider pickupCollider =
                GetComponent<Collider>();

            if (!pickupCollider.isTrigger)
            {
                pickupCollider.isTrigger = true;
            }
        }

        public void Initialize(
            PowerUpSpawnManager manager,
            int slotIndex,
            float lifetime)
        {
            spawnManager = manager;
            spawnSlotIndex = slotIndex;

            isInitialized = true;

            if (lifetime > 0f)
            {
                lifetimeCoroutine =
                    StartCoroutine(
                        LifetimeRoutine(lifetime));
            }
        }

        protected void ConsumePowerUp()
        {
            RemovePowerUp();
        }

        private IEnumerator LifetimeRoutine(
            float lifetime)
        {
            yield return new WaitForSeconds(
                lifetime);

            lifetimeCoroutine = null;

            RemovePowerUp();
        }

        private void RemovePowerUp()
        {
            if (isRemoving)
            {
                return;
            }

            isRemoving = true;

            if (lifetimeCoroutine != null)
            {
                StopCoroutine(lifetimeCoroutine);
                lifetimeCoroutine = null;
            }

            spawnManager?.NotifyPowerUpRemoved(
                this,
                spawnSlotIndex);

            Destroy(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (!isInitialized ||
                isRemoving)
            {
                return;
            }

            spawnManager?.NotifyPowerUpRemoved(
                this,
                spawnSlotIndex);
        }
    }
}