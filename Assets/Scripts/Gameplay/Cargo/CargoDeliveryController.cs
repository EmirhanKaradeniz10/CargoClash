using CargoClash.Movement;
using UnityEngine;

namespace CargoClash.Gameplay.Cargo
{
    [RequireComponent(typeof(CargoCarrier))]
    [RequireComponent(typeof(PlayerIdentity))]
    [RequireComponent(typeof(GridMovementController))]
    public sealed class CargoDeliveryController : MonoBehaviour
    {
        [Header("Delivery Zones")]
        [SerializeField]
        private DeliveryZone homeDeliveryZone;

        [SerializeField]
        private DeliveryZone forwardDeliveryZone;

        [Header("Score")]
        [SerializeField]
        private MatchScoreManager scoreManager;

        private CargoCarrier cargoCarrier;
        private PlayerIdentity playerIdentity;
        private GridMovementController movementController;

        private Vector2Int lastCheckedCell;

        private void Awake()
        {
            cargoCarrier = GetComponent<CargoCarrier>();
            playerIdentity = GetComponent<PlayerIdentity>();
            movementController =
                GetComponent<GridMovementController>();

            if (scoreManager == null)
            {
                scoreManager =
                    FindAnyObjectByType<MatchScoreManager>();
            }

            FindOwnDeliveryZones();

            lastCheckedCell =
                movementController.CurrentCell;
        }

        private void Start()
        {
            ValidateDependencies();
            TryDeliverCargo();
        }

        private void Update()
        {
            Vector2Int currentCell =
                movementController.CurrentCell;

            if (currentCell == lastCheckedCell)
            {
                return;
            }

            lastCheckedCell = currentCell;
            TryDeliverCargo();
        }

        private void TryDeliverCargo()
        {
            if (!cargoCarrier.IsCarrying ||
                scoreManager == null)
            {
                return;
            }

            Vector2Int currentCell =
                movementController.CurrentCell;

            DeliveryZone reachedZone =
                GetReachedDeliveryZone(currentCell);

            if (reachedZone == null)
            {
                return;
            }

            CargoItem deliveredCargo =
                cargoCarrier.RemoveCarriedCargo();

            if (deliveredCargo == null)
            {
                return;
            }

            int awardedScore =
                CalculateDeliveryScore(
                    deliveredCargo.ScoreValue,
                    reachedZone.ScoreMultiplier);

            scoreManager.AddScore(
                playerIdentity.Side,
                awardedScore);

            Debug.Log(
                $"{playerIdentity.Side} delivered " +
                $"{deliveredCargo.CargoType} cargo to " +
                $"{reachedZone.ZoneType}. " +
                $"Base score: {deliveredCargo.ScoreValue}, " +
                $"Multiplier: {reachedZone.ScoreMultiplier}, " +
                $"Awarded score: {awardedScore}.",
                this);

            deliveredCargo.NotifyDelivered();
            Destroy(deliveredCargo.gameObject);
        }

        private DeliveryZone GetReachedDeliveryZone(
            Vector2Int currentCell)
        {
            if (homeDeliveryZone != null &&
                homeDeliveryZone.IsInDeliveryRange(
                    currentCell))
            {
                return homeDeliveryZone;
            }

            if (forwardDeliveryZone != null &&
                forwardDeliveryZone.IsInDeliveryRange(
                    currentCell))
            {
                return forwardDeliveryZone;
            }

            return null;
        }

        private static int CalculateDeliveryScore(
            int baseScore,
            float multiplier)
        {
            return Mathf.RoundToInt(
                baseScore * multiplier);
        }

        private void FindOwnDeliveryZones()
        {
            DeliveryZone[] deliveryZones =
                    FindObjectsByType<DeliveryZone>();

            foreach (DeliveryZone zone in deliveryZones)
            {
                if (zone.Owner != playerIdentity.Side)
                {
                    continue;
                }

                switch (zone.ZoneType)
                {
                    case DeliveryZoneType.Home:
                        homeDeliveryZone = zone;
                        break;

                    case DeliveryZoneType.Forward:
                        forwardDeliveryZone = zone;
                        break;
                }
            }
        }

        private void ValidateDependencies()
        {
            bool hasError = false;

            if (homeDeliveryZone == null)
            {
                Debug.LogError(
                    $"Home DeliveryZone was not found for " +
                    $"{playerIdentity.Side}.",
                    this);

                hasError = true;
            }

            if (forwardDeliveryZone == null)
            {
                Debug.LogError(
                    $"Forward DeliveryZone was not found for " +
                    $"{playerIdentity.Side}.",
                    this);

                hasError = true;
            }

            if (scoreManager == null)
            {
                Debug.LogError(
                    "MatchScoreManager was not found.",
                    this);

                hasError = true;
            }

            if (hasError)
            {
                enabled = false;
            }
        }
    }
}