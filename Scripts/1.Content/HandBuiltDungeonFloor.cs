using System;
using System.Diagnostics;

public static class HandBuiltDungeonFloor
{
	public static DungeonFloor CreateSample(
		RoomFeaturePopulationService roomFeaturePopulation,
		RoomFeaturePopulationParameters? roomFeatures = null)
	{
		var entranceCoord = new RoomCoord(0, 0);
		var firstNorthCoord = new RoomCoord(0, -1);
		var eastWingCoord = new RoomCoord(1, -1);
		var northDeadEndCoord = new RoomCoord(0, -2);

		var floor = new DungeonFloor
		{
			Level = 1,
			Entrance = entranceCoord
		};
		DungeonFloorLayoutService.PlaceRoom(floor, entranceCoord, new DungeonRoom());
		DungeonFloorLayoutService.PlaceRoom(floor, firstNorthCoord, new DungeonRoom());
		DungeonFloorLayoutService.PlaceRoom(floor, eastWingCoord, new DungeonRoom());
		DungeonFloorLayoutService.PlaceRoom(floor, northDeadEndCoord, new DungeonRoom());

		LinkOrTrace(floor, entranceCoord, HorizontalDirection.North, RoomConnectionType.Passage);
		LinkOrTrace(floor, firstNorthCoord, HorizontalDirection.East, RoomConnectionType.Door);
		LinkOrTrace(floor, firstNorthCoord, HorizontalDirection.North, RoomConnectionType.Passage);

		floor.Rooms[northDeadEndCoord].Features.Add(new FloorExitFeature() { ExitType = FloorConnectionType.Stairs });

		var populationParams = roomFeatures ?? RoomFeaturePopulationParameters.CreateDefault();
		var featureRandom = new Random(HashCode.Combine(floor.Level, 0xC0FFEE));
		roomFeaturePopulation.Populate(floor, populationParams, featureRandom, static msg => Debug.WriteLine(msg));

		return floor;
	}

	private static void LinkOrTrace(
		DungeonFloor floor,
		RoomCoord sourceCoord,
		HorizontalDirection directionFromSourceToNeighbor,
		RoomConnectionType connection)
	{
		if (DungeonFloorLayoutService.TryLinkRooms(floor, sourceCoord, directionFromSourceToNeighbor, connection, out var diagnostic))
			return;
		Debug.WriteLine(diagnostic);
	}
}
