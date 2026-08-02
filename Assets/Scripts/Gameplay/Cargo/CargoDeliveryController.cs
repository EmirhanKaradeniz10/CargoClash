using CargoClash.Movement;
using UnityEngine;

using CargoClash.Gameplay;


namespace CargoClash.Gameplay.Cargo
{
    [RequireComponent(typeof(CargoCarrier))]
    [RequireComponent(typeof(PlayerIdentity))]
    [RequireComponent(typeof(GridMovementController))]
    public sealed class CargoDeliveryController : MonoBehaviour
    {
        [SerializeField]
        private BaseZone ownBase;

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

            if (ownBase == null)
            {
                FindOwnBase();
            }

            lastCheckedCell = movementController.CurrentCell;
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
                ownBase == null ||
                scoreManager == null)
            {
                return;
            }

            if (!ownBase.Contains(
                    movementController.CurrentCell))
            {
                return;
            }

            CargoItem deliveredCargo =
                cargoCarrier.RemoveCarriedCargo();

            if (deliveredCargo == null)
            {
                return;
            }

            scoreManager.AddScore(
                playerIdentity.Side,
                deliveredCargo.ScoreValue);

            Destroy(deliveredCargo.gameObject);
        }

        private void FindOwnBase()
        {
            BaseZone[] baseZones =
                    FindObjectsByType<BaseZone>();

            foreach (BaseZone baseZone in baseZones)
            {
                if (baseZone.Owner == playerIdentity.Side)
                {
                    ownBase = baseZone;
                    return;
                }
            }
        }

        private void ValidateDependencies()
        {
            if (ownBase == null)
            {
                Debug.LogError(
                    $"Base zone was not found for " +
                    $"{playerIdentity.Side}.",
                    this);

                enabled = false;
                return;
            }

            if (scoreManager == null)
            {
                Debug.LogError(
                    "MatchScoreManager was not found.",
                    this);

                enabled = false;
            }
        }
    }
}