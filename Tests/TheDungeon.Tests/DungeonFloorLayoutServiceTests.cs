using Xunit;

public sealed class DungeonFloorLayoutServiceTests
{
	[Fact]
	public void TryLinkRooms_NullFloor_ReturnsFalseWithDiagnostic()
	{
		var ok = DungeonFloorLayoutService.TryLinkRooms(
			null!,
			DirectionHelper.Origin,
			HorizontalDirection.North,
			RoomConnectionType.Passage,
			out var diagnostic);

		Assert.False(ok);
		Assert.False(string.IsNullOrWhiteSpace(diagnostic));
	}

	[Fact]
	public void TryLinkRooms_MissingSourceOrNeighbor_ReturnsFalse()
	{
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = new DungeonRoom { Position = DirectionHelper.Origin };

		var ok = DungeonFloorLayoutService.TryLinkRooms(
			floor,
			DirectionHelper.Origin,
			HorizontalDirection.North,
			RoomConnectionType.Passage,
			out var diagnostic);

		Assert.False(ok);
		Assert.False(string.IsNullOrWhiteSpace(diagnostic));
	}

	[Fact]
	public void TryLinkRooms_WhenBothRoomsExist_SetsBidirectionalConnections()
	{
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		var source = new DungeonRoom { Position = DirectionHelper.Origin };
		var neighborCoord = DungeonNavigationHelper.GetNeighborCoord(DirectionHelper.Origin, HorizontalDirection.East);
		var neighbor = new DungeonRoom { Position = neighborCoord };
		floor.Rooms[source.Position] = source;
		floor.Rooms[neighbor.Position] = neighbor;

		var ok = DungeonFloorLayoutService.TryLinkRooms(
			floor,
			source.Position,
			HorizontalDirection.East,
			RoomConnectionType.Door,
			out _);

		Assert.True(ok);
		Assert.Equal(RoomConnectionType.Door, source.Exits.Get(HorizontalDirection.East));
		Assert.Equal(RoomConnectionType.Door, neighbor.Exits.Get(HorizontalDirection.West));
	}
}
