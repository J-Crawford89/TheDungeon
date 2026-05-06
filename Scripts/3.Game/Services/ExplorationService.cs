using System;
using System.Collections.Generic;
using System.Linq;

public sealed class ExplorationService
{
    private readonly RoomFeaturePopulationService _roomFeaturePopulation;
    private readonly ICombatService _combat;
    private readonly InspectService _inspect;
    private readonly TrapService _trapService;
    private readonly PlayerExperienceService? _experience;
    private readonly int _experiencePerFirstRoomVisit;
    private readonly int _experiencePerFloorEntry;

    public ExplorationService(
        RoomFeaturePopulationService roomFeaturePopulation,
        ICombatService combat,
        InspectService inspect,
        TrapService trapService,
        PlayerExperienceService? experience = null,
        int experiencePerFirstRoomVisit = 0,
        int experiencePerFloorEntry = 0)
    {
        _roomFeaturePopulation = roomFeaturePopulation;
        _combat = combat;
        _inspect = inspect;
        _trapService = trapService;
        _experience = experience;
        _experiencePerFirstRoomVisit = experiencePerFirstRoomVisit;
        _experiencePerFloorEntry = experiencePerFloorEntry;
    }

    public bool TryBeginCombatIfHostile(GameSessionState session, RoomCoord previousCoord, int floorLevel) =>
        _combat.TryBeginCombatIfHostile(session, previousCoord, floorLevel);

    public ExplorationServiceResult MoveForward(GameSessionState session)
    {
        var dungeon = session.Dungeon;
        var floor = dungeon.CurrentFloor;
        if (floor == null)
        {
            return ExplorationServiceResult.Fail(
                ExplorationErrorCode.NoCurrentFloor,
                DebugMessage.Format(
                    "Move the player forward one room",
                    "CurrentFloor was null",
                    $"playerCoord={dungeon.PlayerCoord}, facing={session.Player.Facing}"));
        }

        if (dungeon.CurrentRoom is not { } currentRoom)
        {
            return ExplorationServiceResult.Fail(
                ExplorationErrorCode.NoRoomAtPlayer,
                DebugMessage.Format(
                    "Move the player forward one room",
                    "No DungeonRoom exists at the player's coordinate",
                    $"playerCoord={dungeon.PlayerCoord}, facing={session.Player.Facing}, roomCount={floor.Rooms.Count}"));
        }

        var playerFacing = session.Player.Facing;
        var connectionAhead = currentRoom.Exits.Get(playerFacing);
        if (!DungeonNavigationHelper.IsConnectionTraversable(connectionAhead))
            return ExplorationServiceResult.Fail(ExplorationErrorCode.MoveBlocked);

        var destinationCoord = DungeonNavigationHelper.GetNeighborCoord(dungeon.PlayerCoord, playerFacing);
        if (!DungeonFloorLayoutService.TryGetRoom(floor, destinationCoord, out _))
        {
            return ExplorationServiceResult.Fail(
                ExplorationErrorCode.NoRoomAtTargetCoord,
                DebugMessage.Format(
                    "Move the player forward through a traversable exit",
                    "No neighbor room exists at the destination coordinate (layout inconsistency)",
                    $"fromCoord={dungeon.PlayerCoord}, destinationCoord={destinationCoord}, connectionAhead={connectionAhead}, facing={playerFacing}"));
        }

        var backtrackHorizontal = dungeon.EnteredFromCompass.HasValue && playerFacing == dungeon.EnteredFromCompass.Value;
        _trapService.ProcessTrapsOnRoomExit(session, currentRoom, backtrackHorizontal);

        var discovered = dungeon.DiscoveredRoomsByFloor[floor.Level];
        var firstVisit = !discovered.Contains(destinationCoord);
        dungeon.PlayerCoord = destinationCoord;
        discovered.Add(destinationCoord);
        dungeon.SetIngressAfterHorizontalEnter(playerFacing);
        if (firstVisit)
            _experience?.GrantExperience(session, _experiencePerFirstRoomVisit);
        return ExplorationServiceResult.OkMoveForward(destinationCoord);
    }

    public ExplorationServiceResult AboutFace(PlayerState player)
    {
        player.Facing = DirectionHelper.TurnAround(player.Facing);
        return ExplorationServiceResult.OkRotation(player.Facing);
    }

    public ExplorationServiceResult Turn(PlayerState player, DirectionTurned turn)
    {
        if (!DirectionHelper.TryGetFacingAfterTurn(player.Facing, turn, out var nextFacing))
        {
            return ExplorationServiceResult.Fail(
                ExplorationErrorCode.TurnInvalid,
                DebugMessage.Format(
                    "Rotate the player facing after a relative turn",
                    "DirectionHelper could not resolve a new facing (unexpected DirectionTurned value)",
                    $"turn={turn}, facing={player.Facing}"));
        }

        player.Facing = nextFacing;
        return ExplorationServiceResult.OkRotation(nextFacing);
    }

    public ExplorationServiceResult Inspect(GameSessionState session)
    {
        var dungeon = session.Dungeon;
        var floor = dungeon.CurrentFloor;
        if (floor == null)
        {
            return ExplorationServiceResult.Fail(
                ExplorationErrorCode.InspectNoFloor,
                DebugMessage.Format(
                    "Describe the current room for Inspect",
                    "CurrentFloor was null",
                    $"playerCoord={dungeon.PlayerCoord}"));
        }

        if (dungeon.CurrentRoom is not { } currentRoom)
        {
            return ExplorationServiceResult.Fail(
                ExplorationErrorCode.InspectNoRoom,
                DebugMessage.Format(
                    "Describe the current room for Inspect",
                    "No DungeonRoom exists at the player's coordinate",
                    $"playerCoord={dungeon.PlayerCoord}, roomCount={floor.Rooms.Count}"));
        }

        _inspect.RunInspectDiscovery(session, currentRoom);

        var inspectData = BuildInspectRoomData(currentRoom);
        foreach (var roomExit in currentRoom.Exits.All())
        {
            if (roomExit.Connection == RoomConnectionType.None)
                continue;
            inspectData.Exits.Add((roomExit.Side, roomExit.Connection));
        }

        AppendRoomFeatureInspectLines(currentRoom, inspectData);

        return ExplorationServiceResult.OkInspect(inspectData);
    }

    public ExplorationServiceResult MoveUpAFloor(GameSessionState session)
    {
        var dungeon = session.Dungeon;
        var floor = dungeon.CurrentFloor;
        if (floor == null)
        {
            return ExplorationServiceResult.Fail(
                ExplorationErrorCode.NoCurrentFloor,
                DebugMessage.Format(
                    "Move the player up one floor",
                    "CurrentFloor was null",
                    $"playerCoord={dungeon.PlayerCoord}, facing={session.Player.Facing}"));
        }

        if (dungeon.CurrentRoom is not { } currentRoom)
        {
            return ExplorationServiceResult.Fail(
                ExplorationErrorCode.NoRoomAtPlayer,
                DebugMessage.Format(
                    "Move the player up one floor",
                    "No DungeonRoom exists at the player's coordinate",
                    $"playerCoord={dungeon.PlayerCoord}, facing={session.Player.Facing}, roomCount={floor.Rooms.Count}, floorLevel={floor.Level}"));
        }

        var floorExitFeature = RoomFeatureHelper.GetFeature<FloorExitFeature>(currentRoom);
        if (floorExitFeature == null)
        {
            return ExplorationServiceResult.Fail(
                ExplorationErrorCode.NoVerticalConnection,
                DebugMessage.Format(
                    "Move the player up one floor",
                    "No FloorExitFeature exists at the player's coordinate",
                    $"playerCoord={dungeon.PlayerCoord}, facing={session.Player.Facing}, roomCount={floor.Rooms.Count}, floorLevel={floor.Level}"));
        }

        var floorIndex = dungeon.Floors.IndexOf(dungeon.CurrentFloor);
        if (floorIndex <= 0)
            return ExplorationServiceResult.Fail(ExplorationErrorCode.NoPreviousFloor,
                DebugMessage.Format(
                    "Move the player up one floor",
                    "No previous floor exists (current floor missing from stack or already at top)",
                    $"playerCoord={dungeon.PlayerCoord}, facing={session.Player.Facing}, roomCount={floor.Rooms.Count}, floorLevel={floor.Level}, floorIndex={floorIndex}, floorsCount={dungeon.Floors.Count}"));

        var floorAbove = dungeon.Floors[floorIndex - 1];

        var targetRoom = floorAbove.Rooms
            .Values
            .Where(r => RoomFeatureHelper.HasFeature<FloorExitFeature>(r))
            .FirstOrDefault(r => DirectionHelper.GetVerticalExitDirection(r.Position) == VerticalDirection.Down);

        if (targetRoom == null)
            return ExplorationServiceResult.Fail(ExplorationErrorCode.NoRoomAtTargetCoord,
                DebugMessage.Format(
                    "Move the player up one floor",
                    "No room exists on previous floor with downward vertical connection",
                    $"playerCoord={dungeon.PlayerCoord}, facing={session.Player.Facing}, roomCount={floor.Rooms.Count}, floorLevel={floor.Level}"));

        var targetExitFeature = RoomFeatureHelper.GetFeature<FloorExitFeature>(targetRoom);
        if (targetExitFeature == null || targetExitFeature.ExitType != floorExitFeature.ExitType)
            return ExplorationServiceResult.Fail(ExplorationErrorCode.NoRoomAtTargetCoord,
                DebugMessage.Format(
                    "Move the player up one floor",
                    "Target room and current room do not share the same vertical connection type",
                    $"playerCoord={dungeon.PlayerCoord}, facing={session.Player.Facing}, roomCount={floor.Rooms.Count}, floorLevel={floor.Level}, " +
                        $"targetVerticalConnection={targetExitFeature?.ExitType}, currentVerticalConnection={floorExitFeature.ExitType}"));

        var traversalContext = BuildFloorTraversalContext(session);
        if (!DungeonNavigationHelper.IsVerticalConnectionTraversable(floorExitFeature, targetExitFeature, traversalContext))
            return ExplorationServiceResult.Fail(ExplorationErrorCode.MoveBlocked);

        var exemptVerticalReturn = dungeon.VerticalIngress == VerticalIngressKind.EnteredFromFloorAbove;
        _trapService.ProcessTrapsOnRoomExit(session, currentRoom, exemptVerticalReturn);

        var wasHole = floorExitFeature.ExitType == FloorConnectionType.Hole;
        var holeAlreadyAnchored = wasHole && (floorExitFeature.RopeAnchored || targetExitFeature.RopeAnchored);
        var consumedRopeForHole = false;

        if (wasHole && !holeAlreadyAnchored)
        {
            if (!session.Player.InventoryState.TryConsumeOne(InventoryIds.Rope))
                return ExplorationServiceResult.Fail(
                    ExplorationErrorCode.MoveBlocked,
                    DebugMessage.Format(
                        "Move the player up one floor",
                        "Hole requires rope but inventory consumption failed",
                        $"playerCoord={dungeon.PlayerCoord}, floorLevel={floor.Level}"));

            consumedRopeForHole = true;
            floorExitFeature.RopeAnchored = true;
            targetExitFeature.RopeAnchored = true;
        }

        var firstVisitToFloorAbove = !dungeon.DiscoveredRoomsByFloor.TryGetValue(floorAbove.Level, out var discoveredAbove);
        if (discoveredAbove == null)
        {
            discoveredAbove = new HashSet<RoomCoord>();
            dungeon.DiscoveredRoomsByFloor[floorAbove.Level] = discoveredAbove;
        }
        discoveredAbove.Add(targetRoom.Position);

        dungeon.CurrentFloor = floorAbove;
        dungeon.PlayerCoord = targetRoom.Position;
        dungeon.EnteredFromCompass = null;
        dungeon.VerticalIngress = VerticalIngressKind.EnteredFromFloorBelow;
        if (firstVisitToFloorAbove)
            _experience?.GrantExperience(session, _experiencePerFloorEntry);
        return ExplorationServiceResult.OkChangeFloor(
            floorAbove.Level,
            targetRoom.Position,
            consumedRopeForHole,
            holeWasAlreadyAnchored: wasHole && !consumedRopeForHole && holeAlreadyAnchored);
    }

    public ExplorationServiceResult MoveDownAFloor(GameSessionState session, FloorGenerationParameters? generationOptions = null)
    {
        var dungeon = session.Dungeon;
        var floor = dungeon.CurrentFloor;
        if (floor == null)
        {
            return ExplorationServiceResult.Fail(
                ExplorationErrorCode.NoCurrentFloor,
                DebugMessage.Format(
                    "Move the player down one floor",
                    "CurrentFloor was null",
                    $"playerCoord={dungeon.PlayerCoord}, facing={session.Player.Facing}"));
        }

        if (dungeon.CurrentRoom is not { } currentRoom)
        {
            return ExplorationServiceResult.Fail(
                ExplorationErrorCode.NoRoomAtPlayer,
                DebugMessage.Format(
                    "Move the player down one floor",
                    "No DungeonRoom exists at the player's coordinate",
                    $"playerCoord={dungeon.PlayerCoord}, facing={session.Player.Facing}, roomCount={floor.Rooms.Count}, floorLevel={floor.Level}"));
        }

        var floorExitFeature = RoomFeatureHelper.GetFeature<FloorExitFeature>(currentRoom);
        if (floorExitFeature == null)
        {
            return ExplorationServiceResult.Fail(
                ExplorationErrorCode.NoVerticalConnection,
                DebugMessage.Format(
                    "Move the player down one floor",
                    "No FloorExitFeature exists at the player's coordinate",
                    $"playerCoord={dungeon.PlayerCoord}, facing={session.Player.Facing}, roomCount={floor.Rooms.Count}, floorLevel={floor.Level}"));
        }

        var traversalContext = BuildFloorTraversalContext(session);
        if (!DungeonNavigationHelper.IsVerticalConnectionTraversable(floorExitFeature, traversalContext))
            return ExplorationServiceResult.Fail(ExplorationErrorCode.MoveBlocked);

        var exemptVerticalReturn = dungeon.VerticalIngress == VerticalIngressKind.EnteredFromFloorBelow;
        _trapService.ProcessTrapsOnRoomExit(session, currentRoom, exemptVerticalReturn);

        var opts = generationOptions ?? new FloorGenerationParameters();
        var parameters = new FloorGenerationParameters
        {
            MinRooms = opts.MinRooms,
            MaxRooms = opts.MaxRooms,
            Seed = generationOptions == null ? Random.Shared.Next() : opts.Seed,
            DoorChance = opts.DoorChance,
            CurrentFloorCount = dungeon.Floors.Count,
            PreviousFloorConnectionType = floorExitFeature.ExitType,
            VerticalConnectionWeights = opts.VerticalConnectionWeights,
            RoomFeatures = opts.RoomFeatures
        };

        var newFloor = new FloorGenerator(_roomFeaturePopulation).Generate(parameters);

        var wasHole = floorExitFeature.ExitType == FloorConnectionType.Hole;
        var holeAlreadyAnchored = wasHole && floorExitFeature.RopeAnchored;
        var consumedRopeForHole = false;

        if (wasHole && !floorExitFeature.RopeAnchored)
        {
            if (!session.Player.InventoryState.TryConsumeOne(InventoryIds.Rope))
                return ExplorationServiceResult.Fail(
                    ExplorationErrorCode.MoveBlocked,
                    DebugMessage.Format(
                        "Move the player down one floor",
                        "Hole requires rope but inventory consumption failed",
                        $"playerCoord={dungeon.PlayerCoord}, floorLevel={floor.Level}"));

            consumedRopeForHole = true;
            floorExitFeature.RopeAnchored = true;

            if (!DungeonFloorLayoutService.TryGetRoom(newFloor, newFloor.Entrance, out var entranceRoom))
                return ExplorationServiceResult.Fail(
                    ExplorationErrorCode.NoRoomAtTargetCoord,
                    DebugMessage.Format(
                        "Move the player down one floor",
                        "Generated floor missing entrance room",
                        $"entranceCoord={newFloor.Entrance}, floorLevel={newFloor.Level}"));

            var lowerExit = RoomFeatureHelper.GetFeature<FloorExitFeature>(entranceRoom!);
            if (lowerExit != null)
                lowerExit.RopeAnchored = true;
        }

        var firstVisitToNewFloor = !dungeon.DiscoveredRoomsByFloor.ContainsKey(newFloor.Level);

        dungeon.Floors.Add(newFloor);
        dungeon.CurrentFloor = newFloor;
        dungeon.PlayerCoord = DirectionHelper.Origin;
        if (dungeon.DiscoveredRoomsByFloor.TryGetValue(newFloor.Level, out var discoveredOnNewFloor))
            discoveredOnNewFloor.Add(DirectionHelper.Origin);
        else
            dungeon.DiscoveredRoomsByFloor.Add(newFloor.Level, new HashSet<RoomCoord>([DirectionHelper.Origin]));
        dungeon.EnteredFromCompass = null;
        dungeon.VerticalIngress = VerticalIngressKind.EnteredFromFloorAbove;
        if (firstVisitToNewFloor)
            _experience?.GrantExperience(session, _experiencePerFloorEntry);
        return ExplorationServiceResult.OkChangeFloor(
            newFloor.Level,
            DirectionHelper.Origin,
            consumedRopeForHole,
            holeWasAlreadyAnchored: wasHole && !consumedRopeForHole && holeAlreadyAnchored);
    }
    

    private static void AppendRoomFeatureInspectLines(DungeonRoom room, InspectRoomData inspectData)
    {
        foreach (var feature in room.Features)
        {
            switch (feature)
            {
                case FloorExitFeature:
                    break;
                case MonsterFeature monsterFeature:
                    foreach (var m in monsterFeature.Monsters)
                    {
                        if (m.CurrentHp <= 0)
                            inspectData.FeatureLines.Add(new InspectRoomFeatureLine { Text = $"Slain: {m.Definition.Name} (carcass)" });
                        else
                            inspectData.FeatureLines.Add(new InspectRoomFeatureLine { Text = $"Monster: {m.Definition.Name}" });
                    }

                    break;
                case CorpseFeature corpse:
                {
                    var stacks = corpse.Contents.Count(static c =>
                        !string.IsNullOrWhiteSpace(c.ItemDefinitionId) && c.Quantity > 0);
                    var lootPart = stacks == 0 ? "nothing recoverable" : $"{stacks} stack(s)";
                    var line = $"Remains: {lootPart}";
                    inspectData.FeatureLines.Add(new InspectRoomFeatureLine { Text = line });
                    break;
                }
                case SalvageFeature salvage:
                {
                    var stacks = salvage.Contents.Count(static c =>
                        !string.IsNullOrWhiteSpace(c.ItemDefinitionId) && c.Quantity > 0);
                    var lootPart = stacks == 0 ? "nothing left" : $"{stacks} stack(s)";
                    inspectData.FeatureLines.Add(new InspectRoomFeatureLine { Text = $"Salvage: {lootPart}" });
                    break;
                }
                case ChestFeature chestFeature:
                {
                    var stacks = chestFeature.Contents.Count(static c =>
                        !string.IsNullOrWhiteSpace(c.ItemDefinitionId) && c.Quantity > 0);
                    var lootPart = stacks == 0 ? "nothing inside" : $"{stacks} stack(s)";
                    var line = $"Chest: {lootPart}";
                    if (chestFeature.Locked)
                        line += " (locked)";
                    inspectData.FeatureLines.Add(new InspectRoomFeatureLine { Text = line });
                    break;
                }
                case TrapFeature trapFeature:
                    foreach (var t in trapFeature.Traps.Where(x => x.IsRevealed))
                        inspectData.FeatureLines.Add(new InspectRoomFeatureLine { Text = $"Trap: {t.Definition.Name}" });
                    break;
                case TreasureFeature treasureFeature:
                    if (treasureFeature.TreasureItems.Count == 0)
                        break;
                    foreach (var t in treasureFeature.TreasureItems.Where(x => x.IsRevealed))
                        inspectData.FeatureLines.Add(new InspectRoomFeatureLine { Text = $"Treasure: {t.Definition.Name}" });
                    break;
                case NpcFeature npcFeature:
                    foreach (var n in npcFeature.NPCs.Where(x => x.IsRevealed))
                        inspectData.FeatureLines.Add(new InspectRoomFeatureLine { Text = $"Figure: {n.Definition.Name}" });
                    break;
                case LoreFeature loreFeature:
                    foreach (var l in loreFeature.Lore.Where(x => x.IsRevealed))
                    {
                        var line = string.IsNullOrWhiteSpace(l.Definition.Description)
                            ? $"Lore: {l.Definition.Name}"
                            : $"Lore: {l.Definition.Name} — {l.Definition.Description}";
                        inspectData.FeatureLines.Add(new InspectRoomFeatureLine { Text = line });
                    }

                    break;
            }
        }
    }

    private static InspectRoomData BuildInspectRoomData(DungeonRoom room)
    {
        FloorConnectionType vertical = FloorConnectionType.None;
        var holeRopeAnchored = false;

        if (RoomFeatureHelper.GetFeature<FloorExitFeature>(room) is { } exit)
        {
            vertical = exit.ExitType;
            if (vertical == FloorConnectionType.Hole)
                holeRopeAnchored = exit.RopeAnchored;
        }

        return new InspectRoomData
        {
            VerticalConnection = vertical,
            HoleRopeAnchored = holeRopeAnchored,
        };
    }

    private static DungeonNavigationHelper.FloorTraversalContext BuildFloorTraversalContext(GameSessionState session) =>
        new()
        {
            HasRope = session.Player.InventoryState.SumQuantityForDefinitionId(InventoryIds.Rope) > 0,
        };
}
