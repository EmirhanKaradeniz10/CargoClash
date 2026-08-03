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

        private CargoSpawnSlot originSlot;
        private CargoSpawnManager spawnManager;
        private CargoCarrier carrier;

        private Collider cargoCollider;
        private Rigidbody cargoRigidbody;

        private bool isRegistered;

        public CargoType CargoType => cargoType;

        public int ScoreValue => scoreValue;

        public CargoSpawnSlot OriginSlot => originSlot;

        public CargoCarrier Carrier => carrier;

        public bool IsCarried => carrier != null;

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
            isRegistered = true;
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
            carrier = newCarrier;

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

        public void SetDropped(Vector3 worldPosition)
        {
            carrier = null;

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
        }

        public void DetachFromCarrier()
        {
            carrier = null;
            transform.SetParent(null);
        }

        public void NotifyDelivered()
        {
            if (!isRegistered)
            {
                return;
            }

            isRegistered = false;
            spawnManager?.UnregisterCargo(this);
        }

        private void OnDestroy()
        {
            if (!isRegistered)
            {
                return;
            }

            isRegistered = false;
            spawnManager?.UnregisterCargo(this);
        }
    }
}