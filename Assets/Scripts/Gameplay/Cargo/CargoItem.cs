using System.Collections;
using UnityEngine;

namespace CargoClash.Gameplay.Cargo
{
    public sealed class CargoItem : MonoBehaviour
    {
        [Header("Cargo")]
        [SerializeField]
        private CargoType cargoType = CargoType.Normal;

        [SerializeField, Min(0)]
        private int scoreValue = 10;

        [Header("Dropped Cargo")]
        [SerializeField, Min(0f)]
        private float droppedLifetime = 7f;

        [SerializeField, Min(0f)]
        private float dropperPickupLockDuration = 0.4f;

        private CargoSpawnSlot originSlot;
        private CargoSpawnManager spawnManager;
        private CargoCarrier carrier;

        private CargoCarrier blockedCarrier;
        private float blockedCarrierUntil;

        private Collider cargoCollider;
        private Rigidbody cargoRigidbody;

        private Coroutine droppedLifetimeCoroutine;
        private bool isRegistered;

        public CargoType CargoType => cargoType;

        public int ScoreValue => scoreValue;

        public CargoSpawnSlot OriginSlot => originSlot;

        public CargoCarrier Carrier => carrier;

        public bool IsCarried => carrier != null;

        public bool IsDropped =>
            carrier == null &&
            originSlot == null;

        private void Awake()
        {
            cargoCollider = GetComponent<Collider>();
            cargoRigidbody = GetComponent<Rigidbody>();
        }

        public void Initialize(
            CargoSpawnSlot slot,
            CargoSpawnManager manager)
        {
            originSlot = slot;
            spawnManager = manager;
            carrier = null;

            blockedCarrier = null;
            blockedCarrierUntil = 0f;

            isRegistered = true;
        }

        public bool CanBePickedUpBy(
            CargoCarrier requestingCarrier)
        {
            if (requestingCarrier == null ||
                IsCarried)
            {
                return false;
            }

            if (requestingCarrier == blockedCarrier &&
                Time.time < blockedCarrierUntil)
            {
                return false;
            }

            return true;
        }

        public void RemoveFromSlot()
        {
            if (originSlot == null)
            {
                return;
            }

            CargoSpawnSlot previousSlot = originSlot;
            originSlot = null;

            previousSlot.NotifyCargoRemoved(this);
        }

        public void SetCarried(
            CargoCarrier newCarrier,
            Transform carryPoint)
        {
            CancelDroppedLifetime();

            carrier = newCarrier;
            blockedCarrier = null;
            blockedCarrierUntil = 0f;

            if (cargoCollider != null)
            {
                cargoCollider.enabled = false;
            }

            if (cargoRigidbody != null)
            {
                cargoRigidbody.isKinematic = true;
                cargoRigidbody.useGravity = false;
            }

            Transform targetParent =
                carryPoint != null
                    ? carryPoint
                    : newCarrier.transform;

            transform.SetParent(targetParent);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public void SetDropped(
            Vector3 worldPosition,
            CargoCarrier dropper)
        {
            CancelDroppedLifetime();

            carrier = null;
            originSlot = null;

            blockedCarrier = dropper;
            blockedCarrierUntil =
                Time.time + dropperPickupLockDuration;

            transform.SetParent(null);
            transform.position = worldPosition;
            transform.rotation = Quaternion.identity;

            if (cargoCollider != null)
            {
                cargoCollider.enabled = true;
            }

            if (cargoRigidbody != null)
            {
                cargoRigidbody.isKinematic = true;
                cargoRigidbody.useGravity = false;
            }

            StartDroppedLifetime();
        }

        public void DetachFromCarrier()
        {
            CancelDroppedLifetime();

            carrier = null;
            transform.SetParent(null);
        }

        public void NotifyDelivered()
        {
            if (!isRegistered)
            {
                return;
            }

            CancelDroppedLifetime();

            isRegistered = false;
            spawnManager?.UnregisterCargo(this);
        }

        private void StartDroppedLifetime()
        {
            if (droppedLifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            droppedLifetimeCoroutine =
                StartCoroutine(
                    DroppedLifetimeRoutine());
        }

        private IEnumerator DroppedLifetimeRoutine()
        {
            yield return new WaitForSeconds(
                droppedLifetime);

            droppedLifetimeCoroutine = null;
            Destroy(gameObject);
        }

        private void CancelDroppedLifetime()
        {
            if (droppedLifetimeCoroutine == null)
            {
                return;
            }

            StopCoroutine(droppedLifetimeCoroutine);
            droppedLifetimeCoroutine = null;
        }

        private void OnDestroy()
        {
            CancelDroppedLifetime();

            if (!isRegistered)
            {
                return;
            }

            isRegistered = false;
            spawnManager?.UnregisterCargo(this);
        }
    }
}