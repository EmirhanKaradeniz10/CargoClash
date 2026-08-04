using System.Collections.Generic;
using CargoClash.Gameplay;
using CargoClash.Gameplay.Cargo;
using UnityEngine;

namespace CargoClash.Map
{
    public sealed class ArenaLayoutValidator : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private GridMapGenerator mapGenerator;

        [Header("Rules")]
        [SerializeField, Min(1)]
        private int minimumWalkableApproachCells = 3;

        [SerializeField, Min(0)]
        private int preferredSpawnZoneDistance = 3;

        [SerializeField]
        private bool requireMirrorSymmetry = true;

        [Header("Validation")]
        [SerializeField]
        private bool validateOnStart = true;

        private void Start()
        {
            if (validateOnStart)
            {
                ValidateLayout();
            }
        }

        [ContextMenu("Validate Arena Layout")]
        public void ValidateLayout()
        {
            if (!ResolveMapGenerator())
            {
                return;
            }

            DeliveryZone[] deliveryZones =
                FindObjectsByType<DeliveryZone>();

            CargoSpawnSlot[] cargoSlots =
                FindObjectsByType<CargoSpawnSlot>();

            int errorCount = 0;
            int warningCount = 0;

            errorCount += ValidateDeliveryZones(
                deliveryZones);

            errorCount += ValidateDeliveryZoneOverlaps(
                deliveryZones);

            errorCount += ValidateDeliveryZoneOwnership(
                deliveryZones);

            errorCount += ValidateDeliveryZoneSettings(
                deliveryZones);

            errorCount += ValidateCargoSlots(
                cargoSlots);

            ValidateSpawnZoneDistances(
                cargoSlots,
                deliveryZones,
                ref errorCount,
                ref warningCount);

            if (requireMirrorSymmetry)
            {
                errorCount += ValidateDeliveryZoneSymmetry(
                    deliveryZones);

                errorCount += ValidateCargoSlotSymmetry(
                    cargoSlots);

                errorCount += ValidateObstacleSymmetry();
            }

            if (errorCount == 0 &&
                warningCount == 0)
            {
                Debug.Log(
                    "Arena layout validation passed. " +
                    $"{deliveryZones.Length} delivery zones and " +
                    $"{cargoSlots.Length} cargo slots are valid.",
                    this);

                return;
            }

            if (errorCount == 0)
            {
                Debug.LogWarning(
                    "Arena layout validation completed with " +
                    $"{warningCount} warning(s) and no errors.",
                    this);

                return;
            }

            Debug.LogError(
                "Arena layout validation failed with " +
                $"{errorCount} error(s) and " +
                $"{warningCount} warning(s).",
                this);
        }

        private int ValidateDeliveryZones(
            DeliveryZone[] deliveryZones)
        {
            int errorCount = 0;

            if (deliveryZones.Length != 4)
            {
                Debug.LogError(
                    "Arena must contain exactly 4 DeliveryZones. " +
                    $"Found: {deliveryZones.Length}.",
                    this);

                errorCount++;
            }

            HashSet<Vector2Int> usedCenters = new();

            foreach (DeliveryZone zone in deliveryZones)
            {
                if (zone == null)
                {
                    Debug.LogError(
                        "A null DeliveryZone reference was found.",
                        this);

                    errorCount++;
                    continue;
                }

                if (!mapGenerator.IsWalkable(
                        zone.CenterCell))
                {
                    Debug.LogError(
                        $"{zone.name}: center cell " +
                        $"{zone.CenterCell} is not walkable.",
                        zone);

                    errorCount++;
                }

                if (!usedCenters.Add(zone.CenterCell))
                {
                    Debug.LogError(
                        $"{zone.name}: another DeliveryZone " +
                        $"already uses center cell " +
                        $"{zone.CenterCell}.",
                        zone);

                    errorCount++;
                }

                int approachCount =
                    zone.CountWalkableApproachCells();

                if (approachCount <
                    minimumWalkableApproachCells)
                {
                    Debug.LogError(
                        $"{zone.name}: only {approachCount} " +
                        "walkable approach cells were found. " +
                        $"Minimum required: " +
                        $"{minimumWalkableApproachCells}.",
                        zone);

                    errorCount++;
                }
            }

            return errorCount;
        }

        private int ValidateDeliveryZoneOverlaps(
            DeliveryZone[] deliveryZones)
        {
            int errorCount = 0;

            for (int firstIndex = 0;
                 firstIndex < deliveryZones.Length;
                 firstIndex++)
            {
                DeliveryZone firstZone =
                    deliveryZones[firstIndex];

                if (firstZone == null)
                {
                    continue;
                }

                HashSet<Vector2Int> firstCells =
                    GetDeliveryCells(firstZone);

                for (int secondIndex = firstIndex + 1;
                     secondIndex < deliveryZones.Length;
                     secondIndex++)
                {
                    DeliveryZone secondZone =
                        deliveryZones[secondIndex];

                    if (secondZone == null)
                    {
                        continue;
                    }

                    HashSet<Vector2Int> secondCells =
                        GetDeliveryCells(secondZone);

                    foreach (Vector2Int cell in firstCells)
                    {
                        if (!secondCells.Contains(cell))
                        {
                            continue;
                        }

                        Debug.LogError(
                            $"{firstZone.name} and " +
                            $"{secondZone.name} overlap at " +
                            $"delivery cell {cell}.",
                            this);

                        errorCount++;
                        break;
                    }
                }
            }

            return errorCount;
        }

        private int ValidateCargoSlots(
            CargoSpawnSlot[] cargoSlots)
        {
            int errorCount = 0;

            if (cargoSlots.Length != 10)
            {
                Debug.LogError(
                    "Arena must contain exactly 10 " +
                    "CargoSpawnSlots. " +
                    $"Found: {cargoSlots.Length}.",
                    this);

                errorCount++;
            }

            HashSet<Vector2Int> usedCells = new();

            foreach (CargoSpawnSlot slot in cargoSlots)
            {
                if (slot == null)
                {
                    Debug.LogError(
                        "A null CargoSpawnSlot reference " +
                        "was found.",
                        this);

                    errorCount++;
                    continue;
                }

                if (!mapGenerator.IsWalkable(slot.Cell))
                {
                    Debug.LogError(
                        $"{slot.name}: cargo cell " +
                        $"{slot.Cell} is not walkable.",
                        slot);

                    errorCount++;
                }

                if (!usedCells.Add(slot.Cell))
                {
                    Debug.LogError(
                        $"{slot.name}: another cargo slot " +
                        $"already uses cell {slot.Cell}.",
                        slot);

                    errorCount++;
                }
            }

            return errorCount;
        }

        private void ValidateSpawnZoneDistances(
            CargoSpawnSlot[] cargoSlots,
            DeliveryZone[] deliveryZones,
            ref int errorCount,
            ref int warningCount)
        {
            foreach (CargoSpawnSlot slot in cargoSlots)
            {
                if (slot == null)
                {
                    continue;
                }

                foreach (DeliveryZone zone in deliveryZones)
                {
                    if (zone == null)
                    {
                        continue;
                    }

                    int distance =
                        CalculateManhattanDistance(
                            slot.Cell,
                            zone.CenterCell);

                    if (zone.IsInDeliveryRange(slot.Cell))
                    {
                        Debug.LogError(
                            $"{slot.name} at {slot.Cell} is " +
                            $"inside the delivery area of " +
                            $"{zone.name}.",
                            slot);

                        errorCount++;
                        continue;
                    }

                    if (distance <
                        preferredSpawnZoneDistance)
                    {
                        Debug.LogWarning(
                            $"{slot.name} at {slot.Cell} is " +
                            $"only {distance} cells from " +
                            $"{zone.name}. Preferred minimum " +
                            $"distance: " +
                            $"{preferredSpawnZoneDistance}.",
                            slot);

                        warningCount++;
                    }
                }
            }
        }

        private int ValidateDeliveryZoneSymmetry(
            DeliveryZone[] deliveryZones)
        {
            int errorCount = 0;

            foreach (DeliveryZone zone in deliveryZones)
            {
                if (zone == null)
                {
                    continue;
                }

                Vector2Int expectedMirrorCell =
                    MirrorCell(zone.CenterCell);

                PlayerSide expectedMirrorOwner =
                    zone.Owner == PlayerSide.Player
                        ? PlayerSide.Opponent
                        : PlayerSide.Player;

                bool mirrorFound = false;

                foreach (DeliveryZone candidate in deliveryZones)
                {
                    if (candidate == null ||
                        candidate == zone)
                    {
                        continue;
                    }

                    if (candidate.Owner ==
                            expectedMirrorOwner &&
                        candidate.ZoneType ==
                            zone.ZoneType &&
                        candidate.CenterCell ==
                            expectedMirrorCell)
                    {
                        mirrorFound = true;
                        break;
                    }
                }

                if (mirrorFound)
                {
                    continue;
                }

                Debug.LogError(
                    $"{zone.name}: expected mirrored " +
                    $"{zone.ZoneType} DeliveryZone for " +
                    $"{expectedMirrorOwner} at " +
                    $"{expectedMirrorCell}.",
                    zone);

                errorCount++;
            }

            return errorCount;
        }

        private int ValidateObstacleSymmetry()
        {
            int errorCount = 0;

            foreach (Vector2Int blockedCell in
                     mapGenerator.GetBlockedCells())
            {
                Vector2Int expectedMirrorCell =
                    MirrorCell(blockedCell);

                if (mapGenerator.IsBlocked(
                        expectedMirrorCell))
                {
                    continue;
                }

                Debug.LogError(
                    $"Blocked cell {blockedCell} has no " +
                    $"mirrored obstacle at " +
                    $"{expectedMirrorCell}.",
                    mapGenerator);

                errorCount++;
            }

            return errorCount;
        }

        private int ValidateCargoSlotSymmetry(
            CargoSpawnSlot[] cargoSlots)
        {
            int errorCount = 0;

            foreach (CargoSpawnSlot slot in cargoSlots)
            {
                if (slot == null)
                {
                    continue;
                }

                Vector2Int expectedMirrorCell =
                    MirrorCell(slot.Cell);

                bool mirrorFound = false;

                foreach (CargoSpawnSlot candidate in cargoSlots)
                {
                    if (candidate == null)
                    {
                        continue;
                    }

                    if (candidate.Cell ==
                        expectedMirrorCell)
                    {
                        mirrorFound = true;
                        break;
                    }
                }

                if (mirrorFound)
                {
                    continue;
                }

                Debug.LogError(
                    $"{slot.name}: mirrored cargo slot " +
                    $"was not found at " +
                    $"{expectedMirrorCell}.",
                    slot);

                errorCount++;
            }

            return errorCount;
        }

        private int ValidateDeliveryZoneOwnership(
                                            DeliveryZone[] deliveryZones)
        {
            int errorCount = 0;

            errorCount += ValidateZonesForSide(
                deliveryZones,
                PlayerSide.Player);

            errorCount += ValidateZonesForSide(
                deliveryZones,
                PlayerSide.Opponent);

            return errorCount;
        }

        private int ValidateDeliveryZoneSettings(
    DeliveryZone[] deliveryZones)
        {
            int errorCount = 0;

            foreach (DeliveryZone zone in deliveryZones)
            {
                if (zone == null)
                {
                    continue;
                }

                if (zone.DeliveryRange != 1)
                {
                    Debug.LogError(
                        $"{zone.name}: DeliveryRange must be 1. " +
                        $"Current value: {zone.DeliveryRange}.",
                        zone);

                    errorCount++;
                }

                if (!Mathf.Approximately(
                        zone.ScoreMultiplier,
                        1f))
                {
                    Debug.LogError(
                        $"{zone.name}: ScoreMultiplier must " +
                        $"currently be 1.0. Current value: " +
                        $"{zone.ScoreMultiplier}.",
                        zone);

                    errorCount++;
                }

                bool expectedCooling =
                    zone.ZoneType ==
                    DeliveryZoneType.Home;

                if (zone.ProvidesCoolingBonus !=
                    expectedCooling)
                {
                    Debug.LogError(
                        $"{zone.name}: cooling setting does not " +
                        $"match its zone type. " +
                        $"Expected: {expectedCooling}, " +
                        $"Current: " +
                        $"{zone.ProvidesCoolingBonus}.",
                        zone);

                    errorCount++;
                }
            }

            return errorCount;
        }

        private int ValidateZonesForSide(
    DeliveryZone[] deliveryZones,
    PlayerSide side)
        {
            int homeCount = 0;
            int forwardCount = 0;

            foreach (DeliveryZone zone in deliveryZones)
            {
                if (zone == null ||
                    zone.Owner != side)
                {
                    continue;
                }

                switch (zone.ZoneType)
                {
                    case DeliveryZoneType.Home:
                        homeCount++;
                        break;

                    case DeliveryZoneType.Forward:
                        forwardCount++;
                        break;
                }
            }

            int errorCount = 0;

            if (homeCount != 1)
            {
                Debug.LogError(
                    $"{side} must have exactly one Home " +
                    $"DeliveryZone. Found: {homeCount}.",
                    this);

                errorCount++;
            }

            if (forwardCount != 1)
            {
                Debug.LogError(
                    $"{side} must have exactly one Forward " +
                    $"DeliveryZone. Found: {forwardCount}.",
                    this);

                errorCount++;
            }

            return errorCount;
        }

        private HashSet<Vector2Int> GetDeliveryCells(
            DeliveryZone zone)
        {
            HashSet<Vector2Int> cells = new();

            int range = zone.DeliveryRange;

            for (int xOffset = -range;
                 xOffset <= range;
                 xOffset++)
            {
                int remainingRange =
                    range - Mathf.Abs(xOffset);

                for (int yOffset = -remainingRange;
                     yOffset <= remainingRange;
                     yOffset++)
                {
                    Vector2Int cell =
                        zone.CenterCell +
                        new Vector2Int(
                            xOffset,
                            yOffset);

                    cells.Add(cell);
                }
            }

            return cells;
        }

        private Vector2Int MirrorCell(Vector2Int cell)
        {
            return new Vector2Int(
                mapGenerator.Width - 1 - cell.x,
                cell.y);
        }

        private static int CalculateManhattanDistance(
            Vector2Int first,
            Vector2Int second)
        {
            return Mathf.Abs(first.x - second.x) +
                   Mathf.Abs(first.y - second.y);
        }

        private bool ResolveMapGenerator()
        {
            if (mapGenerator == null)
            {
                mapGenerator =
                    FindAnyObjectByType<GridMapGenerator>();
            }

            if (mapGenerator != null)
            {
                return true;
            }

            Debug.LogError(
                "GridMapGenerator was not found.",
                this);

            return false;
        }

        private void OnValidate()
        {
            minimumWalkableApproachCells =
                Mathf.Max(
                    1,
                    minimumWalkableApproachCells);

            preferredSpawnZoneDistance =
                Mathf.Max(
                    0,
                    preferredSpawnZoneDistance);
        }
    }
}