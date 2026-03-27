using System;
using System.Collections.Generic;
using System.Linq;

public sealed class FloorGenerator
{
	private static readonly HorizontalDirection[] CardinalDirections =
	{
		HorizontalDirection.North, HorizontalDirection.East, HorizontalDirection.South, HorizontalDirection.West
	};

	private readonly RoomFeaturePopulationService _roomFeaturePopulation;

	public FloorGenerator(RoomFeaturePopulationService roomFeaturePopulation) =>
		_roomFeaturePopulation = roomFeaturePopulation;

	public DungeonFloor Generate(FloorGenerationParameters parameters, Action<string>? logDiagnostic = null)
	{
		var random = new Random(parameters.Seed);
		var minimumRooms = Math.Max(1, parameters.MinRooms);
		var maximumRooms = Math.Max(minimumRooms, parameters.MaxRooms);
		var targetRoomCount = random.Next(minimumRooms, maximumRooms + 1);

		var entranceCoord = new RoomCoord(0, 0);
		var floor = new DungeonFloor 
		{
			Level = parameters.CurrentFloorCount + 1,
			Entrance = entranceCoord
		};
		var firstRoom = new DungeonRoom();
		if (parameters.CurrentFloorCount > 0)
		{
			if (parameters.PreviousFloorConnectionType == FloorConnectionType.None)
				logDiagnostic?.Invoke(DebugMessage.Format(
					"Add previous floor connection",
					"No previous floor connection type present in parameters",
					$"currentFloorCount={parameters.CurrentFloorCount}"));
			else
				firstRoom.Features.Add(new FloorExitFeature() { ExitType = parameters.PreviousFloorConnectionType });
		}

		DungeonFloorLayoutService.PlaceRoom(floor, entranceCoord, firstRoom);

		var placedCoords = new HashSet<RoomCoord> { entranceCoord };
		var frontierCoords = new HashSet<RoomCoord>();
		AddNeighborCoordsToFrontier(entranceCoord, placedCoords, frontierCoords);

		while (placedCoords.Count < targetRoomCount && frontierCoords.Count > 0)
		{
			if (!TryPickRandomCoord(frontierCoords, random, out var newRoomCoord))
			{
				logDiagnostic?.Invoke(DebugMessage.Format(
					"Pick a frontier cell to expand the procedural floor",
					"Frontier was unexpectedly empty while the generation loop continued",
					$"placedCount={placedCoords.Count}, targetRoomCount={targetRoomCount}, frontierCount={frontierCoords.Count}"));
				break;
			}

			frontierCoords.Remove(newRoomCoord);

			var possibleParents = new List<(RoomCoord parentCoord, HorizontalDirection directionFromParentToNew)>();
			foreach (var candidateDirectionFromParent in CardinalDirections)
			{
				var directionFromNewTowardParent = DirectionHelper.GetOpposite(candidateDirectionFromParent);
				var parentCoord = DungeonNavigationHelper.GetNeighborCoord(newRoomCoord, directionFromNewTowardParent);
				if (placedCoords.Contains(parentCoord))
					possibleParents.Add((parentCoord, candidateDirectionFromParent));
			}

			if (possibleParents.Count == 0)
				continue;

			var parentIndex = random.Next(possibleParents.Count);
			var (parentRoomCoord, chosenDirectionFromParentToNew) = possibleParents[parentIndex];

			DungeonFloorLayoutService.PlaceRoom(floor, newRoomCoord, new DungeonRoom());
			placedCoords.Add(newRoomCoord);

			if (!DungeonFloorLayoutService.TryLinkRooms(
				    floor,
				    parentRoomCoord,
				    chosenDirectionFromParentToNew,
				    RoomConnectionType.Passage,
				    out var linkDiagnostic))
			{
				logDiagnostic?.Invoke(linkDiagnostic ?? "LinkRooms failed with no diagnostic.");
			}

			foreach (var cardinalDirection in CardinalDirections)
			{
				var adjacentCoord = DungeonNavigationHelper.GetNeighborCoord(newRoomCoord, cardinalDirection);
				if (!placedCoords.Contains(adjacentCoord))
					frontierCoords.Add(adjacentCoord);
			}
		}

		ApplyRandomDoors(floor, random, parameters.DoorChance, logDiagnostic);
		MarkFarthestNonEntranceAsVerticalConnection(floor, random, parameters.VerticalConnectionWeights);

		var roomFeatureParams = parameters.RoomFeatures ?? RoomFeaturePopulationParameters.CreateDefault();
		var featureRandom = new Random(HashCode.Combine(parameters.Seed, unchecked((int)0xBEEFF00Du)));
		_roomFeaturePopulation.Populate(floor, roomFeatureParams, featureRandom, logDiagnostic);

		return floor;
	}

	private void AddNeighborCoordsToFrontier(
		RoomCoord originCoord,
		HashSet<RoomCoord> placedCoords,
		HashSet<RoomCoord> frontierCoords)
	{
		foreach (var direction in CardinalDirections)
		{
			var neighborCoord = DungeonNavigationHelper.GetNeighborCoord(originCoord, direction);
			if (!placedCoords.Contains(neighborCoord))
				frontierCoords.Add(neighborCoord);
		}
	}

	private bool TryPickRandomCoord(HashSet<RoomCoord> candidates, Random random, out RoomCoord chosenCoord)
	{
		chosenCoord = default;
		if (candidates == null || candidates.Count == 0)
			return false;

		var skipCount = random.Next(candidates.Count);
		foreach (var candidateCoord in candidates)
		{
			if (skipCount == 0)
			{
				chosenCoord = candidateCoord;
				return true;
			}

			skipCount--;
		}

		return false;
	}

	private void ApplyRandomDoors(
		DungeonFloor floor,
		Random random,
		double doorChance,
		Action<string>? logDiagnostic)
	{
		foreach (var (roomCoord, dungeonRoom) in floor.Rooms.ToList())
		{
			foreach (var roomExit in dungeonRoom.Exits.All().ToList())
			{
				if (roomExit.Connection != RoomConnectionType.Passage)
					continue;
				if (random.NextDouble() >= doorChance)
					continue;

				var neighborCoord = DungeonNavigationHelper.GetNeighborCoord(roomCoord, roomExit.Side);
				if (!floor.Rooms.ContainsKey(neighborCoord))
					continue;

				if (!DungeonFloorLayoutService.TryLinkRooms(
					    floor,
					    roomCoord,
					    roomExit.Side,
					    RoomConnectionType.Door,
					    out var doorDiagnostic))
					logDiagnostic?.Invoke(doorDiagnostic ?? "TryLinkRooms (door) failed with no diagnostic.");
			}
		}
	}

	private void MarkFarthestNonEntranceAsVerticalConnection(
		DungeonFloor floor,
		Random random,
		IReadOnlyDictionary<FloorConnectionType, double>? verticalConnectionWeights)
	{
		RoomCoord? farthestCoord = null;
		var farthestDistance = -1;
		foreach (var roomCoord in floor.Rooms.Keys)
		{
			var distance = ManhattanDistance(floor.Entrance, roomCoord);
			if (distance > farthestDistance)
			{
				farthestDistance = distance;
				farthestCoord = roomCoord;
			}
		}

		if (farthestCoord == null)
			return;
		if (farthestCoord.Value == floor.Entrance)
			return;
		if (!floor.Rooms.TryGetValue(farthestCoord.Value, out var verticalRoom) || verticalRoom == null)
			return;

		verticalRoom.Features.Add(new FloorExitFeature() { ExitType = PickRandomVerticalConnectionType(random, verticalConnectionWeights) });
	}

	private FloorConnectionType PickRandomVerticalConnectionType(
		Random random,
		IReadOnlyDictionary<FloorConnectionType, double>? weightsByType)
	{
		var weighted = new List<(FloorConnectionType type, double weight)>();
		foreach (FloorConnectionType type in Enum.GetValues<FloorConnectionType>())
		{
			if (type == FloorConnectionType.None)
				continue;
			var w = weightsByType != null && weightsByType.TryGetValue(type, out var custom) ? custom : 1.0;
			weighted.Add((type, w));
		}

		var positive = weighted.Where(x => x.weight > 0).ToList();
		if (positive.Count == 0)
			positive = weighted.Select(x => (x.type, 1.0)).ToList();

		return WeightedRandomSelection.Pick(random, positive);
	}

	private int ManhattanDistance(RoomCoord first, RoomCoord second) =>
		Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);
}
