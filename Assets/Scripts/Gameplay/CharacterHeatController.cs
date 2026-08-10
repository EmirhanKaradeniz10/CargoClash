using CargoClash.Movement;
using UnityEngine;

namespace CargoClash.Gameplay
{
    [RequireComponent(typeof(GridMovementController))]
    [RequireComponent(typeof(PlayerIdentity))]
    public sealed class CharacterHeatController : MonoBehaviour
    {
        [Header("Capacity")]
        [SerializeField, Min(1f)]
        private float maximumHeat = 100f;

        [Header("Heat Gain")]
        [SerializeField, Min(0f)]
        private float dashHeat = 35f;

        [Header("Cooling")]
        [SerializeField, Min(0f)]
        private float passiveCoolingPerSecond = 5f;

        [SerializeField, Min(0f)]
        private float homeCoolingPerSecond = 12f;

        [SerializeField, Min(0f)]
        private float coolingDelay = 0.75f;

        [Header("Overheat")]
        [SerializeField, Min(0f)]
        private float overheatExitThreshold = 40f;

        [SerializeField, Range(0.1f, 1f)]
        private float overheatedMovementMultiplier = 0.6f;

        private GridMovementController movementController;
        private PlayerIdentity playerIdentity;

        private DeliveryZone homeDeliveryZone;

        private float currentHeat;
        private float lastActivityTime;

        private bool isOverheated;

        public float CurrentHeat => currentHeat;

        public float MaximumHeat => maximumHeat;

        public float NormalizedHeat =>
            maximumHeat <= 0f
                ? 0f
                : currentHeat / maximumHeat;

        public bool IsOverheated => isOverheated;

        private void Awake()
        {
            movementController =
                GetComponent<GridMovementController>();

            playerIdentity =
                GetComponent<PlayerIdentity>();

            lastActivityTime = Time.time;

            FindHomeDeliveryZone();
        }

        private void Update()
        {
            if (isOverheated)
            {
                UpdateOverheatedCooling();
                return;
            }

            UpdateNormalCooling();
        }

        public void RegisterDash()
        {
            if (isOverheated)
            {
                return;
            }

            lastActivityTime = Time.time;

            AddHeat(dashHeat);
        }

        public void ReduceHeat(float amount)
        {
            if (amount <= 0f ||
                currentHeat <= 0f)
            {
                return;
            }

            currentHeat =
                Mathf.Max(
                    0f,
                    currentHeat - amount);

            CheckOverheatRecovery();
        }

        private void CheckOverheatRecovery()
        {
            if (!isOverheated)
            {
                return;
            }

            if (currentHeat <
                overheatExitThreshold)
            {
                ExitOverheat();
            }
        }

        private void AddHeat(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            currentHeat =
                Mathf.Min(
                    maximumHeat,
                    currentHeat + amount);

            if (currentHeat >= maximumHeat)
            {
                EnterOverheat();
            }
        }

        private void UpdateNormalCooling()
        {
            if (currentHeat <= 0f)
            {
                return;
            }

            if (movementController.IsMoving ||
                movementController.IsDashing)
            {
                lastActivityTime = Time.time;
                return;
            }

            if (Time.time - lastActivityTime <
                coolingDelay)
            {
                return;
            }

            ApplyCooling();
        }

        private void UpdateOverheatedCooling()
        {
            if (currentHeat > 0f)
            {
                ApplyCooling();
            }

            CheckOverheatRecovery();
        }

        private void ApplyCooling()
        {
            float coolingRate =
                IsInsideHomeCoolingZone()
                    ? homeCoolingPerSecond
                    : passiveCoolingPerSecond;

            currentHeat =
                Mathf.MoveTowards(
                    currentHeat,
                    0f,
                    coolingRate * Time.deltaTime);
        }

        private void EnterOverheat()
        {
            if (isOverheated)
            {
                return;
            }

            isOverheated = true;

            movementController
                .SetMovementSpeedMultiplier(
                    overheatedMovementMultiplier);

            movementController.ClearBufferedMovement();

            Debug.Log(
                $"{playerIdentity.Side} overheated.",
                this);
        }

        private void ExitOverheat()
        {
            if (!isOverheated)
            {
                return;
            }

            isOverheated = false;

            movementController
                .SetMovementSpeedMultiplier(1f);

            lastActivityTime = Time.time;

            Debug.Log(
                $"{playerIdentity.Side} recovered from overheat.",
                this);
        }

        private bool IsInsideHomeCoolingZone()
        {
            return homeDeliveryZone != null &&
                   homeDeliveryZone.ProvidesCoolingBonus &&
                   homeDeliveryZone.IsInDeliveryRange(
                       movementController.EffectiveCell);
        }

        private void FindHomeDeliveryZone()
        {
            DeliveryZone[] zones =
                FindObjectsByType<DeliveryZone>();

            foreach (DeliveryZone zone in zones)
            {
                if (zone.Owner != playerIdentity.Side ||
                    zone.ZoneType != DeliveryZoneType.Home)
                {
                    continue;
                }

                homeDeliveryZone = zone;
                return;
            }

            Debug.LogError(
                $"Home DeliveryZone was not found for " +
                $"{playerIdentity.Side}.",
                this);
        }

        private void OnDisable()
        {
            if (movementController != null)
            {
                movementController
                    .SetMovementSpeedMultiplier(1f);
            }
        }

        private void OnValidate()
        {
            maximumHeat =
                Mathf.Max(1f, maximumHeat);

            overheatExitThreshold =
                Mathf.Clamp(
                    overheatExitThreshold,
                    0f,
                    maximumHeat);
        }
    }
}