using System.Collections.Generic;
using CargoClash.Gameplay;
using CargoClash.Gameplay.Cargo;
using UnityEngine;
using CargoClash.Movement;

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

        [Header("Participants")]
        [SerializeField]
        private GridMovementController playerMovement;

        [SerializeField]
        private GridMovementController opponentMovement;

        [SerializeField]
        private GridPathfinder pathfinder;

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

            errorCount += ValidateStartingCells(
                cargoSlots,
                deliveryZones);

            errorCount += ValidatePathSymmetry(
                deliveryZones);

            errorCount += ValidateCargoPathSymmetry(
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

        private int ValidateStartingCells(
    CargoSpawnSlot[] cargoSlots,
    DeliveryZone[] deliveryZones)
        {
            int errorCount = 0;

            if (!ResolveParticipantReferences())
            {
                return 1;
            }

            Vector2Int playerStart =
                playerMovement.StartingCell;

            Vector2Int opponentStart =
                opponentMovement.StartingCell;

            errorCount += ValidateSingleStartingCell(
                "Player",
                playerStart,
                cargoSlots,
                deliveryZones,
                playerMovement);

            errorCount += ValidateSingleStartingCell(
                "Opponent",
                opponentStart,
                cargoSlots,
                deliveryZones,
                opponentMovement);

            if (playerStart == opponentStart)
            {
                Debug.LogError(
                    $"Player and Opponent use the same " +
                    $"starting cell: {playerStart}.",
                    this);

                errorCount++;
            }

            if (requireMirrorSymmetry)
            {
                Vector2Int expectedOpponentStart =
                    MirrorCell(playerStart);

                if (opponentStart != expectedOpponentStart)
                {
                    Debug.LogError(
                        $"Opponent starting cell {opponentStart} " +
                        $"is not the mirror of Player starting " +
                        $"cell {playerStart}. Expected: " +
                        $"{expectedOpponentStart}.",
                        this);

                    errorCount++;
                }
            }

            return errorCount;
        }

        private int ValidatePathSymmetry(
    DeliveryZone[] deliveryZones)
        {
            if (!ResolveParticipantReferences() ||
                !ResolvePathfinder())
            {
                return 1;
            }

            DeliveryZone playerHome =
                FindDeliveryZone(
                    deliveryZones,
                    PlayerSide.Player,
                    DeliveryZoneType.Home);

            DeliveryZone opponentHome =
                FindDeliveryZone(
                    deliveryZones,
                    PlayerSide.Opponent,
                    DeliveryZoneType.Home);

            DeliveryZone playerForward =
                FindDeliveryZone(
                    deliveryZones,
                    PlayerSide.Player,
                    DeliveryZoneType.Forward);

            DeliveryZone opponentForward =
                FindDeliveryZone(
                    deliveryZones,
                    PlayerSide.Opponent,
                    DeliveryZoneType.Forward);

            if (playerHome == null ||
                opponentHome == null ||
                playerForward == null ||
                opponentForward == null)
            {
                Debug.LogError(
                    "Path symmetry could not be validated " +
                    "because one or more DeliveryZones are missing.",
                    this);

                return 1;
            }

            int errorCount = 0;

            errorCount += ComparePathLengths(
                "Home",
                playerMovement.StartingCell,
                playerHome,
                opponentMovement.StartingCell,
                opponentHome);

            errorCount += ComparePathLengths(
                "Forward",
                playerMovement.StartingCell,
                playerForward,
                opponentMovement.StartingCell,
                opponentForward);

            return errorCount;
        }

        private static DeliveryZone FindDeliveryZone(
    DeliveryZone[] deliveryZones,
    PlayerSide owner,
    DeliveryZoneType zoneType)
        {
            foreach (DeliveryZone zone in deliveryZones)
            {
                if (zone != null &&
                    zone.Owner == owner &&
                    zone.ZoneType == zoneType)
                {
                    return zone;
                }
            }

            return null;
        }

        private int ValidateSingleStartingCell(
    string participantName,
    Vector2Int startingCell,
    CargoSpawnSlot[] cargoSlots,
    DeliveryZone[] deliveryZones,
    Object context)
        {
            int errorCount = 0;

            if (!mapGenerator.IsInsideGrid(startingCell))
            {
                Debug.LogError(
                    $"{participantName} starting cell " +
                    $"{startingCell} is outside the grid.",
                    context);

                return 1;
            }

            if (!mapGenerator.IsWalkable(startingCell))
            {
                Debug.LogError(
                    $"{participantName} starting cell " +
                    $"{startingCell} is blocked.",
                    context);

                errorCount++;
            }

            foreach (CargoSpawnSlot slot in cargoSlots)
            {
                if (slot == null ||
                    slot.Cell != startingCell)
                {
                    continue;
                }

                Debug.LogError(
                    $"{participantName} starting cell " +
                    $"{startingCell} overlaps cargo slot " +
                    $"{slot.name}.",
                    context);

                errorCount++;
            }

            foreach (DeliveryZone zone in deliveryZones)
            {
                if (zone == null ||
                    !zone.IsInDeliveryRange(startingCell))
                {
                    continue;
                }

                Debug.LogError(
                    $"{participantName} starting cell " +
                    $"{startingCell} is inside the delivery " +
                    $"area of {zone.name}.",
                    context);

                errorCount++;
            }

            return errorCount;
        }

        private int ValidateCargoPathSymmetry(
    CargoSpawnSlot[] cargoSlots)
        {
            if (!ResolveParticipantReferences() ||
                !ResolvePathfinder())
            {
                return 1;
            }

            int errorCount = 0;

            HashSet<Vector2Int> checkedCells = new();

            foreach (CargoSpawnSlot slot in cargoSlots)
            {
                if (slot == null ||
                    checkedCells.Contains(slot.Cell))
                {
                    continue;
                }

                Vector2Int mirrorCell =
                    MirrorCell(slot.Cell);

                CargoSpawnSlot mirrorSlot =
                    FindCargoSlotAtCell(
                        cargoSlots,
                        mirrorCell);

                if (mirrorSlot == null)
                {
                    // Bu durum zaten ValidateCargoSlotSymmetry
                    // tarafından ayrıca raporlanıyor.
                    continue;
                }

                int playerPathLength =
                    GetStaticPathLength(
                        playerMovement.StartingCell,
                        slot.Cell);

                int opponentPathLength =
                    GetStaticPathLength(
                        opponentMovement.StartingCell,
                        mirrorSlot.Cell);

                if (playerPathLength < 0 ||
                    opponentPathLength < 0)
                {
                    Debug.LogError(
                        $"Cargo path could not be calculated for " +
                        $"{slot.name} at {slot.Cell} and its mirror " +
                        $"{mirrorSlot.name} at {mirrorSlot.Cell}. " +
                        $"Player path: {playerPathLength}, " +
                        $"Opponent path: {opponentPathLength}.",
                        this);

                    errorCount++;

                    checkedCells.Add(slot.Cell);
                    checkedCells.Add(mirrorSlot.Cell);
                    continue;
                }

                int difference =
                    Mathf.Abs(
                        playerPathLength -
                        opponentPathLength);

                if (difference > 1)
                {
                    Debug.LogError(
                        $"Cargo path imbalance between " +
                        $"{slot.name} at {slot.Cell} and " +
                        $"{mirrorSlot.name} at {mirrorSlot.Cell}. " +
                        $"Player path: {playerPathLength}, " +
                        $"Opponent path: {opponentPathLength}, " +
                        $"difference: {difference}. " +
                        "Maximum allowed difference: 1.",
                        this);

                    errorCount++;
                }

                checkedCells.Add(slot.Cell);
                checkedCells.Add(mirrorSlot.Cell);
            }

            return errorCount;
        }

        private static CargoSpawnSlot FindCargoSlotAtCell(
    CargoSpawnSlot[] cargoSlots,
    Vector2Int targetCell)
        {
            foreach (CargoSpawnSlot slot in cargoSlots)
            {
                if (slot != null &&
                    slot.Cell == targetCell)
                {
                    return slot;
                }
            }

            return null;
        }

        private int GetStaticPathLength(
    Vector2Int start,
    Vector2Int goal)
        {
            if (start == goal)
            {
                return 0;
            }

            if (!mapGenerator.IsWalkable(start) ||
                !mapGenerator.IsWalkable(goal))
            {
                return -1;
            }

            List<Vector2Int> path =
                pathfinder.FindStaticPath(
                    start,
                    goal);

            return path.Count == 0
                ? -1
                : path.Count;
        }

        private int ComparePathLengths(
    string routeName,
    Vector2Int playerStart,
    DeliveryZone playerZone,
    Vector2Int opponentStart,
    DeliveryZone opponentZone)
        {
            int playerLength =
                GetShortestPathLengthToZone(
                    playerStart,
                    playerZone);

            int opponentLength =
                GetShortestPathLengthToZone(
                    opponentStart,
                    opponentZone);

            if (playerLength < 0 ||
                opponentLength < 0)
            {
                Debug.LogError(
                    $"{routeName} route could not be calculated. " +
                    $"Player length: {playerLength}, " +
                    $"Opponent length: {opponentLength}.",
                    this);

                return 1;
            }

            int difference =
                Mathf.Abs(playerLength - opponentLength);

            if (difference <= 1)
            {
                return 0;
            }

            Debug.LogError(
                $"{routeName} path lengths are not balanced. " +
                $"Player: {playerLength}, " +
                $"Opponent: {opponentLength}, " +
                $"Difference: {difference}. " +
                "Maximum allowed difference: 1.",
                this);

            return 1;
        }

        private int GetShortestPathLengthToZone(
    Vector2Int start,
    DeliveryZone zone)
        {
            int shortestLength = int.MaxValue;

            HashSet<Vector2Int> deliveryCells =
                GetDeliveryCells(zone);

            foreach (Vector2Int targetCell in deliveryCells)
            {
                if (!mapGenerator.IsWalkable(targetCell))
                {
                    continue;
                }

                if (start == targetCell)
                {
                    return 0;
                }

                List<Vector2Int> path =
                    pathfinder.FindStaticPath(
                        start,
                        targetCell);

                if (path.Count == 0)
                {
                    continue;
                }

                if (path.Count < shortestLength)
                {
                    shortestLength = path.Count;
                }
            }

            return shortestLength == int.MaxValue
                ? -1
                : shortestLength;
        }

        private bool ResolveParticipantReferences()
        {
            if (playerMovement != null &&
                opponentMovement != null)
            {
                return true;
            }

            GridMovementController[] movementControllers =
                FindObjectsByType<GridMovementController>();

            foreach (GridMovementController controller
                     in movementControllers)
            {
                if (controller == null)
                {
                    continue;
                }

                PlayerIdentity identity =
                    controller.GetComponent<PlayerIdentity>();

                if (identity == null)
                {
                    continue;
                }

                switch (identity.Side)
                {
                    case PlayerSide.Player:
                        playerMovement = controller;
                        break;

                    case PlayerSide.Opponent:
                        opponentMovement = controller;
                        break;
                }
            }

            bool referencesFound =
                playerMovement != null &&
                opponentMovement != null;

            if (!referencesFound)
            {
                Debug.LogError(
                    "Player and Opponent movement controllers " +
                    "could not both be resolved.",
                    this);
            }

            return referencesFound;
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

        private bool ResolvePathfinder()
        {
            if (pathfinder == null)
            {
                pathfinder =
                    FindAnyObjectByType<GridPathfinder>();
            }

            if (pathfinder != null)
            {
                return true;
            }

            Debug.LogError(
                "GridPathfinder was not found.",
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