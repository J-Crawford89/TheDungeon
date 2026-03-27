public static class DungeonFloorLayoutService
{
	public static void PlaceRoom(DungeonFloor floor, RoomCoord position, DungeonRoom room)
	{
		room.Position = position;
		floor.Rooms[position] = room;
	}

	public static bool TryGetRoom(DungeonFloor floor, RoomCoord coord, out DungeonRoom? room)
	{
		room = null;
		if (floor == null)
			return false;
		return floor.Rooms.TryGetValue(coord, out room);
	}

	public static bool TryLinkRooms(
		DungeonFloor floor,
		RoomCoord sourceCoord,
		HorizontalDirection directionFromSourceToNeighbor,
		RoomConnectionType connection,
		out string? diagnosticMessage)
	{
		diagnosticMessage = null;

		if (floor == null)
		{
			diagnosticMessage = DebugMessage.Format(
				"Link two adjacent dungeon rooms with matching exits",
				"DungeonFloor reference was null",
				$"sourceCoord={sourceCoord}, directionFromSourceToNeighbor={directionFromSourceToNeighbor}, connection={connection}");
			return false;
		}

		var neighborCoord = DungeonNavigationHelper.GetNeighborCoord(sourceCoord, directionFromSourceToNeighbor);

		if (!floor.Rooms.TryGetValue(sourceCoord, out var sourceRoom) || sourceRoom == null)
		{
			diagnosticMessage = DebugMessage.Format(
				"Link two adjacent dungeon rooms with matching exits",
				"No room exists at source coordinate",
				$"sourceCoord={sourceCoord}, neighborCoord={neighborCoord}, directionFromSourceToNeighbor={directionFromSourceToNeighbor}, connection={connection}, roomCount={floor.Rooms.Count}");
			return false;
		}

		if (!floor.Rooms.TryGetValue(neighborCoord, out var neighborRoom) || neighborRoom == null)
		{
			diagnosticMessage = DebugMessage.Format(
				"Link two adjacent dungeon rooms with matching exits",
				"No room exists at neighbor coordinate",
				$"sourceCoord={sourceCoord}, neighborCoord={neighborCoord}, directionFromSourceToNeighbor={directionFromSourceToNeighbor}, connection={connection}, roomCount={floor.Rooms.Count}");
			return false;
		}

		var directionFromNeighborToSource = DirectionHelper.GetOpposite(directionFromSourceToNeighbor);
		sourceRoom.Exits.Set(directionFromSourceToNeighbor, connection);
		neighborRoom.Exits.Set(directionFromNeighborToSource, connection);
		return true;
	}
}
