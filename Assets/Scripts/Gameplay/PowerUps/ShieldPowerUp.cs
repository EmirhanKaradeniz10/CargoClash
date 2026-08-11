using System.Collections;
using UnityEngine;

namespace CargoClash.Gameplay.PowerUps
{
    [RequireComponent(typeof(Collider))]
    public sealed class ShieldPowerUp : MonoBehaviour
    {
        private PowerUpSpawnManager spawnManager;

        private Coroutine lifetimeCoroutine;
        private bool isConsumed;
        private bool isInitialized;

        private void Awake()
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
            float lifetime)
        {
            spawnManager = manager;
            isInitialized = true;

            if (lifetime > 0f)
            {
                lifetimeCoroutine =
                    StartCoroutine(
                        LifetimeRoutine(lifetime));
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isConsumed)
            {
                return;
            }

            CharacterShieldController shieldController =
                other.GetComponent<CharacterShieldController>();

            if (shieldController == null)
            {
                return;
            }

            TryConsume(shieldController);
        }

        private void TryConsume(
            CharacterShieldController shieldController)
        {
            if (!shieldController.TryGiveShield())
            {
                return;
            }

            isConsumed = true;

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
            if (lifetimeCoroutine != null)
            {
                StopCoroutine(lifetimeCoroutine);
                lifetimeCoroutine = null;
            }

            spawnManager?.NotifyPowerUpRemoved(this);

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (!isInitialized ||
                isConsumed)
            {
                return;
            }

            spawnManager?.NotifyPowerUpRemoved(this);
        }
    }
}