using System;
using System.Collections.Generic;
using System.Linq;

public sealed class ExplorationService
{
    private readonly RoomFeaturePopulationService _roomFeaturePopulation;
    private readonly ICombatService _combat;

    public ExplorationService(RoomFeaturePopulationService roomFeaturePopulation, ICombatService combat)
    {
        _roomFeaturePopulation = roomFeaturePopulation;
        _combat = combat;
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

        dungeon.PlayerCoord = destinationCoord;
        dungeon.DiscoveredRoomsByFloor[floor.Level].Add(destinationCoord);
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

        var inspectData = new InspectRoomData { VerticalConnection = GetExitType(currentRoom) };
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

        if (!DungeonNavigationHelper.IsVerticalConnectionTraversable(floorExitFeature.ExitType))
            return ExplorationServiceResult.Fail(ExplorationErrorCode.MoveBlocked);

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

        var targetExitType = RoomFeatureHelper.GetFeature<FloorExitFeature>(targetRoom)?.ExitType;
        if (targetExitType != floorExitFeature.ExitType)
            return ExplorationServiceResult.Fail(ExplorationErrorCode.NoRoomAtTargetCoord,
                DebugMessage.Format(
                    "Move the player up one floor",
                    "Target room and current room do not share the same vertical connection type",
                    $"playerCoord={dungeon.PlayerCoord}, facing={session.Player.Facing}, roomCount={floor.Rooms.Count}, floorLevel={floor.Level}, " +
                        $"targetVerticalConnection={targetExitType}, currentVerticalConnection={floorExitFeature.ExitType}"));

        dungeon.CurrentFloor = floorAbove;
        dungeon.PlayerCoord = targetRoom.Position;
        return ExplorationServiceResult.OkChangeFloor(floorAbove.Level, targetRoom.Position);
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

        if (!DungeonNavigationHelper.IsVerticalConnectionTraversable(floorExitFeature.ExitType))
            return ExplorationServiceResult.Fail(ExplorationErrorCode.MoveBlocked);

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
        dungeon.Floors.Add(newFloor);
        dungeon.CurrentFloor = newFloor;
        dungeon.PlayerCoord = DirectionHelper.Origin;
        dungeon.DiscoveredRoomsByFloor.Add(newFloor.Level, new HashSet<RoomCoord>([DirectionHelper.Origin]));
        return ExplorationServiceResult.OkChangeFloor(newFloor.Level, DirectionHelper.Origin);
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
                case TrapFeature trapFeature:
                    foreach (var t in trapFeature.Traps)
                        inspectData.FeatureLines.Add(new InspectRoomFeatureLine { Text = $"Trap: {t.Definition.Name}" });
                    break;
                case TreasureFeature treasureFeature:
                    if (treasureFeature.TreasureItems.Count == 0)
                        break;
                    foreach (var t in treasureFeature.TreasureItems)
                        inspectData.FeatureLines.Add(new InspectRoomFeatureLine { Text = $"Treasure: {t.Definition.Name}" });
                    break;
                case NpcFeature npcFeature:
                    foreach (var n in npcFeature.NPCs)
                        inspectData.FeatureLines.Add(new InspectRoomFeatureLine { Text = $"Figure: {n.Definition.Name}" });
                    break;
                case LoreFeature loreFeature:
                    foreach (var l in loreFeature.Lore)
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

    private FloorConnectionType GetExitType(DungeonRoom room)
    {
        if (RoomFeatureHelper.HasFeature<FloorExitFeature>(room))
            return RoomFeatureHelper.GetFeature<FloorExitFeature>(room).ExitType;

        return FloorConnectionType.None;
    }
}
